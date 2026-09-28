using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Infrastructure.Persistence.Repositories;
using AnaliticAsd.Application.DashboardQueries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Tests.Infrastructure;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Client_repository_only_returns_current_agency_data()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var firstAgency = Agency.Create("Agency A", DateTimeOffset.UtcNow);
        var secondAgency = Agency.Create("Agency B", DateTimeOffset.UtcNow);
        context.AddRange(firstAgency, secondAgency,
            Client.Create(firstAgency.Id, "Visible", DateTimeOffset.UtcNow),
            Client.Create(secondAgency.Id, "Hidden", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var repository = new ClientRepository(context, new FixedTenant(firstAgency.Id));
        var clients = await repository.ListAsync();

        Assert.Single(clients);
        Assert.Equal("Visible", clients[0].Name);
    }

    [Fact]
    public async Task Client_and_ad_account_round_trip_through_relational_mapping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var agency = Agency.Create("Agency", DateTimeOffset.UtcNow);
        var client = Client.Create(agency.Id, "Client A", DateTimeOffset.UtcNow);
        var account = CreateAccount(agency.Id, client.Id, "987654321");
        context.AddRange(agency, client, account);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.AdAccounts.SingleAsync();
        var source = await context.DataSources.SingleAsync();
        Assert.Equal(client.Id, stored.ClientId);
        Assert.Equal("987654321", stored.MetaAccountId.Value);
        Assert.Equal("USD", stored.Currency.Value);
        Assert.Equal(stored.Id, source.Id);
        Assert.Equal(agency.Id, source.AgencyId);
        Assert.Equal(DataProvider.MetaAds, source.Provider);
    }

    [Fact]
    public async Task Data_source_cannot_reference_a_provider_connection_from_another_agency()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var firstAgency = Agency.Create("Agency A", DateTimeOffset.UtcNow);
        var secondAgency = Agency.Create("Agency B", DateTimeOffset.UtcNow);
        var client = Client.Create(firstAgency.Id, "Client", DateTimeOffset.UtcNow);
        var providerConnection = ProviderConnection.Create(secondAgency.Id, DataProvider.GoogleAds,
            "Google Ads", "protected", null, null, DateTimeOffset.UtcNow);
        var source = DataSource.Create(Guid.NewGuid(), firstAgency.Id, client.Id, providerConnection.Id,
            DataProvider.GoogleAds, DataSourceType.AdvertisingAccount, "customer-1", "Account", "USD",
            "America/La_Paz", DateTimeOffset.UtcNow);

        context.AddRange(firstAgency, secondAgency, client, providerConnection, source);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Removing_a_legacy_ad_account_also_removes_its_generic_source()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var agency = Agency.Create("Agency", DateTimeOffset.UtcNow);
        var client = Client.Create(agency.Id, "Client", DateTimeOffset.UtcNow);
        var account = CreateAccount(agency.Id, client.Id, "456789123");
        context.AddRange(agency, client, account);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new AdAccountRepository(context, new FixedTenant(agency.Id));
        var stored = await repository.GetByIdAsync(account.Id, true);
        repository.Remove(Assert.IsType<AdAccount>(stored));
        await context.SaveChangesAsync();

        Assert.Empty(await context.AdAccounts.ToArrayAsync());
        Assert.Empty(await context.DataSources.ToArrayAsync());
    }

    [Fact]
    public async Task Folder_parent_cannot_belong_to_another_client()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var agency = Agency.Create("Agency", DateTimeOffset.UtcNow);
        var firstClient = Client.Create(agency.Id, "First", DateTimeOffset.UtcNow);
        var secondClient = Client.Create(agency.Id, "Second", DateTimeOffset.UtcNow);
        var foreignParent = Folder.Create(agency.Id, secondClient.Id, null, "Foreign", 0, DateTimeOffset.UtcNow);
        context.AddRange(agency, firstClient, secondClient, foreignParent);
        await context.SaveChangesAsync();

        context.Folders.Add(Folder.Create(agency.Id, firstClient.Id, foreignParent.Id, "Invalid", 0,
            DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Analyst_only_reads_clients_with_a_persisted_editor_assignment()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTimeOffset.UtcNow;
        var agency = Agency.Create("Agency", now);
        var visible = Client.Create(agency.Id, "Visible", now);
        var hidden = Client.Create(agency.Id, "Hidden", now);
        var owner = User.Create("owner@example.test", now);
        owner.SetPasswordHash("hash");
        var analyst = User.Create("analyst@example.test", now);
        analyst.SetPasswordHash("hash");
        context.AddRange(agency, visible, hidden, owner, analyst,
            Membership.Create(agency.Id, owner.Id, AgencyRole.Owner, now),
            Membership.Create(agency.Id, analyst.Id, AgencyRole.Analyst, now),
            ClientEditorAssignment.Create(agency.Id, visible.Id, analyst.Id, owner.Id, now));
        await context.SaveChangesAsync();

        var repository = new ClientRepository(context,
            new FixedTenant(agency.Id, analyst.Id, nameof(AgencyRole.Analyst)));
        var clients = await repository.ListAsync();

        Assert.Equal(visible.Id, Assert.Single(clients).Id);
    }

    [Fact]
    public async Task Meta_account_id_is_unique()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var agency = Agency.Create("Agency", DateTimeOffset.UtcNow);
        var firstClient = Client.Create(agency.Id, "Client A", DateTimeOffset.UtcNow);
        var secondClient = Client.Create(agency.Id, "Client B", DateTimeOffset.UtcNow);
        context.Agencies.Add(agency);
        context.Clients.AddRange(firstClient, secondClient);
        context.AdAccounts.AddRange(
            CreateAccount(agency.Id, firstClient.Id, "123456789"),
            CreateAccount(agency.Id, secondClient.Id, "123456789"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Dashboard_query_repository_hides_sources_from_another_agency_and_client()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AnalitiAdsDbContext>().UseSqlite(connection).Options;
        await using var context = new AnalitiAdsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var now = DateTimeOffset.UtcNow;
        var firstAgency = Agency.Create("Agency A", now);
        var secondAgency = Agency.Create("Agency B", now);
        var firstClient = Client.Create(firstAgency.Id, "First", now);
        var secondClient = Client.Create(secondAgency.Id, "Second", now);
        var firstAccount = CreateAccount(firstAgency.Id, firstClient.Id, "111111111");
        var secondAccount = CreateAccount(secondAgency.Id, secondClient.Id, "222222222");
        context.AddRange(firstAgency, secondAgency, firstClient, secondClient, firstAccount, secondAccount);
        await context.SaveChangesAsync();

        IDashboardQueryRepository repository = new DashboardQueryRepository(context,
            new FixedTenant(firstAgency.Id));

        Assert.Equal(firstAccount.Id, Assert.Single(await repository.ListSourcesAsync(firstClient.Id)).Id);
        Assert.Empty(await repository.ListSourcesAsync(secondClient.Id));
        Assert.Null(await repository.GetSourceAsync(secondClient.Id, secondAccount.Id));
    }

    private static AdAccount CreateAccount(Guid agencyId, Guid clientId, string metaAccountId) =>
        AdAccount.Create(
            agencyId,
            clientId,
            MetaAdAccountId.Parse(metaAccountId),
            "Account",
            CurrencyCode.Parse("USD"),
            MetaTimeZoneId.Parse("America/La_Paz"),
            DateTimeOffset.UtcNow);

    private sealed record FixedTenant(Guid AgencyId, Guid UserId, string Role) : ICurrentTenant
    {
        public FixedTenant(Guid agencyId) : this(agencyId, Guid.NewGuid(), nameof(AgencyRole.Owner)) { }
    }
}
