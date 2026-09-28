using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Meta;
using Microsoft.AspNetCore.DataProtection;

namespace AnaliticAsd.Infrastructure.Meta;

public sealed class MetaOAuthStateProtector(IDataProtectionProvider provider, TimeProvider timeProvider) : IMetaOAuthStateProtector
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("AnalitiAds.Meta.OAuthState.v1").ToTimeLimitedDataProtector();
    public string Protect(MetaOAuthState state) => protector.Protect(JsonSerializer.Serialize(state), timeProvider.GetUtcNow().AddMinutes(10));
    public MetaOAuthState Unprotect(string state)
    {
        try { return JsonSerializer.Deserialize<MetaOAuthState>(protector.Unprotect(state)) ?? throw new ArgumentException("Invalid OAuth state."); }
        catch (Exception exception) when (exception is not ArgumentException) { throw new ArgumentException("OAuth state is invalid or expired.", nameof(state), exception); }
    }
}

public sealed class MetaTokenProtector(IDataProtectionProvider provider) : IMetaTokenProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("AnalitiAds.Meta.AccessToken.v1");
    public string Protect(string token) => protector.Protect(token);
    public string Unprotect(string protectedToken) => protector.Unprotect(protectedToken);
}
