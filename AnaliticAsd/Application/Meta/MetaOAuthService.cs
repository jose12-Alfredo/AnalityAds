using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Meta;

namespace AnaliticAsd.Application.Meta;

public sealed class MetaOAuthService(
    IConfiguration configuration,
    ICurrentTenant currentTenant,
    IMetaOAuthStateProtector stateProtector,
    IMetaTokenProtector tokenProtector,
    IMetaGraphClient graphClient,
    IMetaConnectionRepository connectionRepository,
    IProviderConnectionRepository providerConnectionRepository,
    IClientRepository clientRepository,
    IAdAccountRepository adAccountRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IMetaOAuthService
{
    public string CreateAuthorizationUrl()
    {
        var appId = Required("Meta:AppId");
        var redirectUri = Required("Meta:RedirectUri");
        var version = configuration["Meta:GraphVersion"] ?? "v25.0";
        var state = stateProtector.Protect(new(currentTenant.AgencyId, currentTenant.UserId));
        return $"https://www.facebook.com/{version}/dialog/oauth?client_id={Uri.EscapeDataString(appId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}&response_type=code&scope=ads_read";
    }

    public async Task<string> CompleteAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var context = stateProtector.Unprotect(state);
        var token = await graphClient.ExchangeCodeAsync(code, cancellationToken);
        var protectedToken = tokenProtector.Protect(token.AccessToken);
        var connection = await connectionRepository.GetAsync(context.AgencyId, true, cancellationToken);
        if (connection is null) connectionRepository.Add(MetaConnection.Create(context.AgencyId, protectedToken, token.ExpiresAtUtc, timeProvider.GetUtcNow()));
        else connection.Update(protectedToken, token.ExpiresAtUtc, timeProvider.GetUtcNow());

        var providerConnection = await providerConnectionRepository.GetLatestAsync(
            context.AgencyId, DataProvider.MetaAds, true, cancellationToken);
        if (providerConnection is null)
        {
            providerConnectionRepository.Add(ProviderConnection.Create(context.AgencyId, DataProvider.MetaAds,
                "Meta Ads", protectedToken, null, token.ExpiresAtUtc, timeProvider.GetUtcNow()));
        }
        else
        {
            providerConnection.UpdateAuthorization("Meta Ads", protectedToken, null, token.ExpiresAtUtc,
                timeProvider.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Redirect("success");
    }

    public string CreateFailureRedirect(string error) => Redirect(error == "access_denied" ? "denied" : "error");

    public async Task<MetaConnectionStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetAsync(currentTenant.AgencyId, false, cancellationToken);
        return new(connection is not null && (connection.ExpiresAtUtc is null || connection.ExpiresAtUtc > timeProvider.GetUtcNow()), connection?.ExpiresAtUtc);
    }

    public async Task<IReadOnlyList<MetaAccountModel>> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        var connection = await RequiredConnection(cancellationToken);
        var remote = await graphClient.ListAdAccountsAsync(tokenProtector.Unprotect(connection.ProtectedAccessToken), cancellationToken);
        var results = new List<MetaAccountModel>(remote.Count);
        foreach (var account in remote)
        {
            var existing = await adAccountRepository.GetByMetaAccountIdAsync(MetaAdAccountId.Parse(account.MetaAccountId), cancellationToken);
            results.Add(Map(account, existing));
        }
        return results;
    }

    public async Task<MetaAccountModel> AssociateAsync(Guid clientId, string metaAccountId, CancellationToken cancellationToken = default)
    {
        if (await clientRepository.GetByIdAsync(clientId, false, cancellationToken) is null) throw new EntityNotFoundException($"Client '{clientId}' was not found.");
        var parsedId = MetaAdAccountId.Parse(metaAccountId);
        if (await adAccountRepository.GetByMetaAccountIdAsync(parsedId, cancellationToken) is not null) throw new ConflictException($"Meta ad account '{parsedId}' is already associated.");
        var remote = (await ListRemote(cancellationToken)).SingleOrDefault(x => MetaAdAccountId.Parse(x.MetaAccountId) == parsedId)
            ?? throw new EntityNotFoundException($"Meta ad account '{parsedId}' is not available to the connected Meta user.");
        var providerConnection = await providerConnectionRepository.GetLatestAsync(
            currentTenant.AgencyId, DataProvider.MetaAds, false, cancellationToken)
            ?? throw new ConflictException("The agency's generic Meta connection is missing. Connect Meta again.");
        var entity = AdAccount.Create(currentTenant.AgencyId, clientId, parsedId, remote.Name,
            CurrencyCode.Parse(remote.Currency), MetaTimeZoneId.Parse(remote.TimeZone), timeProvider.GetUtcNow(),
            providerConnection.Id);
        entity.MarkConnected(timeProvider.GetUtcNow());
        adAccountRepository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(remote, entity);
    }

    private async Task<IReadOnlyList<MetaRemoteAccount>> ListRemote(CancellationToken cancellationToken)
    {
        var connection = await RequiredConnection(cancellationToken);
        return await graphClient.ListAdAccountsAsync(tokenProtector.Unprotect(connection.ProtectedAccessToken), cancellationToken);
    }

    private async Task<MetaConnection> RequiredConnection(CancellationToken cancellationToken)
    {
        var connection = await connectionRepository.GetAsync(currentTenant.AgencyId, false, cancellationToken)
            ?? throw new ConflictException("The agency is not connected to Meta.");
        if (connection.ExpiresAtUtc is not null && connection.ExpiresAtUtc <= timeProvider.GetUtcNow()) throw new ConflictException("The Meta connection has expired. Connect again.");
        return connection;
    }

    private MetaAccountModel Map(MetaRemoteAccount account, AdAccount? existing) => new(MetaAdAccountId.Parse(account.MetaAccountId).Value, account.Name, account.Currency, account.TimeZone, account.AccountStatus, existing is not null, existing?.ClientId);
    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value : throw new ServiceConfigurationException($"Configuration '{key}' is required.");
    private string Redirect(string result)
    {
        var baseUrl = Required("Meta:FrontendCallbackUrl");
        return $"{baseUrl}{(baseUrl.Contains('?') ? '&' : '?')}meta={Uri.EscapeDataString(result)}";
    }
}
