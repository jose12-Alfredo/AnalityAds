using System.Text.Json;
using AnaliticAsd.Application.ProviderIntegrations;
using Microsoft.AspNetCore.DataProtection;

namespace AnaliticAsd.Infrastructure.ProviderIntegrations;

public sealed class ProviderOAuthStateProtector(IDataProtectionProvider provider, TimeProvider clock)
    : IProviderOAuthStateProtector
{
    private readonly ITimeLimitedDataProtector protector = provider
        .CreateProtector("AnalitiAds.Provider.OAuthState.v1").ToTimeLimitedDataProtector();

    public string Protect(ProviderOAuthState state) =>
        protector.Protect(JsonSerializer.Serialize(state), clock.GetUtcNow().AddMinutes(10));

    public ProviderOAuthState Unprotect(string state)
    {
        try
        {
            return JsonSerializer.Deserialize<ProviderOAuthState>(protector.Unprotect(state))
                ?? throw new ArgumentException("Invalid OAuth state.");
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            throw new ArgumentException("OAuth state is invalid or expired.", nameof(state), exception);
        }
    }
}

public sealed class ProviderCredentialProtector(IDataProtectionProvider provider) : IProviderCredentialProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("AnalitiAds.Provider.Credential.v1");
    public string Protect(ProviderCredential credential) => protector.Protect(JsonSerializer.Serialize(credential));
    public ProviderCredential Unprotect(string payload) =>
        JsonSerializer.Deserialize<ProviderCredential>(protector.Unprotect(payload))
        ?? throw new InvalidOperationException("The provider credential payload is invalid.");
}
