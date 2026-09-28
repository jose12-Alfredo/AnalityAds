using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.ProviderIntegrations;

public sealed class GoogleIntegrationService(
    IConfiguration configuration,
    ICurrentTenant tenant,
    IProviderOAuthStateProtector stateProtector,
    IProviderCredentialProtector credentialProtector,
    IGoogleProviderClient client,
    IProviderConnectionRepository connectionRepository,
    IProviderSourceRepository sourceRepository,
    IClientRepository clientRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IGoogleIntegrationService
{
    public string CreateAuthorizationUrl(DataProvider provider)
    {
        EnsureGoogle(provider);
        var state = stateProtector.Protect(new(tenant.AgencyId, tenant.UserId, provider));
        var scope = provider == DataProvider.GoogleAds
            ? "https://www.googleapis.com/auth/adwords"
            : "https://www.googleapis.com/auth/analytics.readonly";
        var values = new Dictionary<string, string>
        {
            ["client_id"] = Required("Google:ClientId"),
            ["redirect_uri"] = RedirectUri(provider),
            ["response_type"] = "code",
            ["scope"] = scope,
            ["access_type"] = "offline",
            ["include_granted_scopes"] = "true",
            ["prompt"] = "consent",
            ["state"] = state
        };
        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&",
            values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public async Task<string> CompleteAsync(DataProvider provider, string code, string state,
        CancellationToken cancellationToken = default)
    {
        EnsureGoogle(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var oauth = stateProtector.Unprotect(state);
        if (oauth.Provider != provider) throw new ArgumentException("OAuth provider does not match the callback.");
        var credential = await client.ExchangeCodeAsync(code, provider, cancellationToken);
        var protectedPayload = credentialProtector.Protect(credential);
        var connection = await connectionRepository.GetLatestAsync(oauth.AgencyId, provider, true, cancellationToken);
        if (connection is null)
            connectionRepository.Add(ProviderConnection.Create(oauth.AgencyId, provider, DisplayName(provider),
                protectedPayload, null, credential.ExpiresAtUtc, clock.GetUtcNow()));
        else
            connection.UpdateAuthorization(DisplayName(provider), protectedPayload, null, credential.ExpiresAtUtc,
                clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Redirect(provider, "success");
    }

    public string CreateFailureRedirect(DataProvider provider, string error)
    {
        EnsureGoogle(provider);
        return Redirect(provider, error == "access_denied" ? "denied" : "error");
    }

    public async Task<ProviderConnectionModel> StatusAsync(DataProvider provider,
        CancellationToken cancellationToken = default)
    {
        EnsureGoogle(provider);
        var connection = await connectionRepository.GetLatestAsync(tenant.AgencyId, provider, false, cancellationToken);
        return connection is null
            ? new(provider, ProviderConnectionStatus.Pending, null, null, null)
            : new(provider, connection.Status, connection.ExpiresAtUtc, connection.LastSucceededAtUtc,
                connection.LastErrorCode);
    }

    public async Task<IReadOnlyList<ProviderSourceModel>> DiscoverAsync(DataProvider provider,
        CancellationToken cancellationToken = default)
    {
        EnsureGoogle(provider);
        var (connection, credential) = await ActiveCredential(provider, cancellationToken);
        var remote = provider == DataProvider.GoogleAds
            ? await client.ListGoogleAdsCustomersAsync(credential.AccessToken, cancellationToken)
            : await client.ListAnalyticsPropertiesAsync(credential.AccessToken, cancellationToken);
        connection.RecordSuccess(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var values = new List<ProviderSourceModel>(remote.Count);
        foreach (var item in remote)
        {
            var existing = await sourceRepository.FindAsync(tenant.AgencyId, provider, item.ExternalId, false,
                cancellationToken);
            values.Add(Map(item, existing));
        }
        return values;
    }

    public async Task<ProviderSourceModel> AssociateAsync(DataProvider provider, Guid clientId, string externalId,
        CancellationToken cancellationToken = default)
    {
        EnsureGoogle(provider);
        if (await clientRepository.GetByIdAsync(clientId, false, cancellationToken) is null)
            throw new EntityNotFoundException("The client was not found.");
        var existing = await sourceRepository.FindAsync(tenant.AgencyId, provider, externalId, false,
            cancellationToken);
        if (existing is not null) throw new ConflictException("The selected source is already assigned to a client.");
        var available = await DiscoverRemote(provider, cancellationToken);
        var remote = available.SingleOrDefault(item => item.ExternalId.Equals(externalId, StringComparison.Ordinal))
            ?? throw new EntityNotFoundException("The selected source is not available to the connected Google user.");
        if (provider == DataProvider.GoogleAds && remote.IsManager)
            throw new ConflictException("Manager accounts cannot be assigned as advertising data sources.");
        var connection = await connectionRepository.GetLatestAsync(tenant.AgencyId, provider, false, cancellationToken)
            ?? throw new ConflictException("Connect the provider before assigning a source.");
        var source = DataSource.Create(Guid.NewGuid(), tenant.AgencyId, clientId, connection.Id, provider,
            remote.SourceType, remote.ExternalId, remote.Name, remote.Currency, remote.TimeZone, clock.GetUtcNow());
        sourceRepository.Add(source);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(remote, source);
    }

    private async Task<IReadOnlyList<RemoteProviderSource>> DiscoverRemote(DataProvider provider,
        CancellationToken cancellationToken)
    {
        var (_, credential) = await ActiveCredential(provider, cancellationToken);
        return provider == DataProvider.GoogleAds
            ? await client.ListGoogleAdsCustomersAsync(credential.AccessToken, cancellationToken)
            : await client.ListAnalyticsPropertiesAsync(credential.AccessToken, cancellationToken);
    }

    private async Task<(ProviderConnection Connection, ProviderCredential Credential)> ActiveCredential(
        DataProvider provider, CancellationToken cancellationToken)
    {
        var connection = await connectionRepository.GetLatestAsync(tenant.AgencyId, provider, true, cancellationToken)
            ?? throw new ConflictException($"{DisplayName(provider)} is not connected.");
        if (connection.Status == ProviderConnectionStatus.Revoked)
            throw new ConflictException($"{DisplayName(provider)} authorization was revoked.");
        var credential = credentialProtector.Unprotect(connection.ProtectedCredentialPayload);
        if (credential.ExpiresAtUtc <= clock.GetUtcNow().AddMinutes(1))
        {
            credential = await client.RefreshAsync(credential, cancellationToken);
            connection.UpdateAuthorization(connection.DisplayName, credentialProtector.Protect(credential),
                connection.ExternalSubjectId, credential.ExpiresAtUtc, clock.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return (connection, credential);
    }

    private string Redirect(DataProvider provider, string result)
    {
        var url = Required(provider == DataProvider.GoogleAds
            ? "GoogleAds:FrontendCallbackUrl" : "GoogleAnalytics4:FrontendCallbackUrl");
        return $"{url}{(url.Contains('?') ? '&' : '?')}result={Uri.EscapeDataString(result)}";
    }

    private string RedirectUri(DataProvider provider) => Required(provider == DataProvider.GoogleAds
        ? "GoogleAds:RedirectUri" : "GoogleAnalytics4:RedirectUri");
    private string Required(string key) => configuration[key] is { Length: > 0 } value ? value
        : throw new ServiceConfigurationException($"Configuration '{key}' is required.");
    private static string DisplayName(DataProvider provider) => provider == DataProvider.GoogleAds
        ? "Google Ads" : "Google Analytics 4";
    private static void EnsureGoogle(DataProvider provider)
    {
        if (provider is not (DataProvider.GoogleAds or DataProvider.GoogleAnalytics4))
            throw new ArgumentException("Only Google Ads and Google Analytics 4 use this authorization flow.");
    }
    private static ProviderSourceModel Map(RemoteProviderSource remote, DataSource? source) => new(
        remote.ExternalId, remote.Name, remote.Currency, remote.TimeZone, remote.SourceType,
        source is not null, source?.ClientId, remote.IsManager, remote.IsTest, source?.Id);
}
