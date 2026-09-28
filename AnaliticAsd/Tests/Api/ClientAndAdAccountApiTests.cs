using System.Net;
using System.Net.Http.Json;

namespace AnaliticAsd.Tests.Api;

public sealed class ClientAndAdAccountApiTests
{
    [Fact]
    public async Task Protected_endpoint_rejects_anonymous_requests()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using var httpClient = factory.CreateClient();
        var response = await httpClient.GetAsync("/api/v1/clients");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Full_client_and_ad_account_crud_flow_works()
    {
        await using var factory = new AnaliticAsdApiFactory();
        using var httpClient = factory.CreateClient();

        var createClient = await httpClient.PostAsJsonAsync("/api/v1/clients", new { name = "Client" });
        Assert.Equal(HttpStatusCode.Created, createClient.StatusCode);
        var client = await createClient.Content.ReadFromJsonAsync<ClientDto>();
        Assert.NotNull(client);

        var createAccount = await httpClient.PostAsJsonAsync(
            $"/api/v1/clients/{client.Id}/ad-accounts",
            new
            {
                metaAccountId = "act_123456789",
                name = "Account",
                currency = "usd",
                timeZone = "America/La_Paz"
            });
        Assert.Equal(HttpStatusCode.Created, createAccount.StatusCode);
        var account = await createAccount.Content.ReadFromJsonAsync<AdAccountDto>();
        Assert.NotNull(account);
        Assert.Equal("123456789", account.MetaAccountId);
        Assert.Equal("USD", account.Currency);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await httpClient.DeleteAsync($"/api/v1/clients/{client.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await httpClient.DeleteAsync($"/api/v1/ad-accounts/{account.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await httpClient.DeleteAsync($"/api/v1/clients/{client.Id}")).StatusCode);
    }

    [Fact]
    public async Task Missing_client_returns_not_found()
    {
        await using var factory = new AnaliticAsdApiFactory();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync($"/api/v1/clients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record ClientDto(Guid Id, string Name, bool IsActive);
    private sealed record AdAccountDto(
        Guid Id,
        Guid ClientId,
        string MetaAccountId,
        string Name,
        string Currency,
        string TimeZone,
        string ConnectionStatus,
        bool IsActive);
}
