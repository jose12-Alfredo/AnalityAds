using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.ProviderIntegrations;

public sealed class TikTokIntegrationService(IConfiguration configuration, ICurrentTenant tenant,
    IProviderOAuthStateProtector stateProtector, IProviderCredentialProtector credentialProtector,
    ITikTokProviderClient client, IProviderConnectionRepository connections, IProviderSourceRepository sources,
    IClientRepository clients, IUnitOfWork unitOfWork, TimeProvider clock) : ITikTokIntegrationService
{
    public string CreateAuthorizationUrl()
    {
        var state = stateProtector.Protect(new(tenant.AgencyId, tenant.UserId, DataProvider.TikTokAds));
        var root = configuration["TikTokAds:AuthorizationUrl"] ?? "https://business-api.tiktok.com/portal/auth";
        return $"{root}?app_id={Uri.EscapeDataString(Required("TikTokAds:AppId"))}&state={Uri.EscapeDataString(state)}&redirect_uri={Uri.EscapeDataString(Required("TikTokAds:RedirectUri"))}";
    }
    public async Task<string> CompleteAsync(string code, string state, CancellationToken ct = default)
    {
        var oauth = stateProtector.Unprotect(state);
        if (oauth.Provider != DataProvider.TikTokAds) throw new ArgumentException("OAuth provider does not match the callback.");
        var credential = await client.ExchangeCodeAsync(code, ct);
        var protectedValue = credentialProtector.Protect(credential);
        var connection = await connections.GetLatestAsync(oauth.AgencyId, DataProvider.TikTokAds, true, ct);
        if (connection is null) connections.Add(ProviderConnection.Create(oauth.AgencyId, DataProvider.TikTokAds,
            "TikTok Ads", protectedValue, null, credential.ExpiresAtUtc, clock.GetUtcNow()));
        else connection.UpdateAuthorization("TikTok Ads", protectedValue, null, credential.ExpiresAtUtc, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct); return Redirect("success");
    }
    public string CreateFailureRedirect(string error) => Redirect(error == "access_denied" ? "denied" : "error");
    public async Task<ProviderConnectionModel> StatusAsync(CancellationToken ct = default)
    {
        var value = await connections.GetLatestAsync(tenant.AgencyId, DataProvider.TikTokAds, false, ct);
        return value is null ? new(DataProvider.TikTokAds, ProviderConnectionStatus.Pending, null, null, null)
            : new(DataProvider.TikTokAds, value.Status, value.ExpiresAtUtc, value.LastSucceededAtUtc, value.LastErrorCode);
    }
    public async Task<IReadOnlyList<ProviderSourceModel>> DiscoverAsync(CancellationToken ct = default)
    {
        var (connection, remote) = await Remote(ct); connection.RecordSuccess(clock.GetUtcNow()); await unitOfWork.SaveChangesAsync(ct);
        var result = new List<ProviderSourceModel>();
        foreach (var item in remote) { var existing = await sources.FindAsync(tenant.AgencyId, DataProvider.TikTokAds, item.ExternalId, false, ct); result.Add(Map(item, existing)); }
        return result;
    }
    public async Task<ProviderSourceModel> AssociateAsync(Guid clientId, string externalId, CancellationToken ct = default)
    {
        if (await clients.GetByIdAsync(clientId, false, ct) is null) throw new EntityNotFoundException("The client was not found.");
        if (await sources.FindAsync(tenant.AgencyId, DataProvider.TikTokAds, externalId, false, ct) is not null) throw new ConflictException("The selected source is already assigned.");
        var (connection, remote) = await Remote(ct);
        var item = remote.SingleOrDefault(x => x.ExternalId == externalId) ?? throw new EntityNotFoundException("The advertiser is not available to this TikTok authorization.");
        var source = DataSource.Create(Guid.NewGuid(), tenant.AgencyId, clientId, connection.Id, DataProvider.TikTokAds,
            DataSourceType.AdvertisingAccount, item.ExternalId, item.Name, item.Currency, item.TimeZone, clock.GetUtcNow());
        sources.Add(source); await unitOfWork.SaveChangesAsync(ct); return Map(item, source);
    }
    private async Task<(ProviderConnection, IReadOnlyList<RemoteProviderSource>)> Remote(CancellationToken ct)
    {
        var connection = await connections.GetLatestAsync(tenant.AgencyId, DataProvider.TikTokAds, true, ct) ?? throw new ConflictException("TikTok Ads is not connected.");
        if (connection.Status == ProviderConnectionStatus.Revoked) throw new ConflictException("TikTok Ads authorization was revoked.");
        var credential = credentialProtector.Unprotect(connection.ProtectedCredentialPayload);
        if (credential.ExpiresAtUtc <= clock.GetUtcNow()) throw new ConflictException("TikTok Ads authorization expired. Reconnect it.");
        return (connection, await client.ListAdvertisersAsync(credential.AccessToken, ct));
    }
    private string Redirect(string result) => $"{Required("TikTokAds:FrontendCallbackUrl")}?result={Uri.EscapeDataString(result)}";
    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value : throw new ServiceConfigurationException($"Configuration '{key}' is required.");
    private static ProviderSourceModel Map(RemoteProviderSource item, DataSource? source) => new(item.ExternalId, item.Name, item.Currency, item.TimeZone, item.SourceType, source is not null, source?.ClientId, false, item.IsTest, source?.Id);
}
