using AnaliticAsd.Application.Meta;
using AnaliticAsd.Infrastructure.Meta;
using Microsoft.AspNetCore.DataProtection;

namespace AnaliticAsd.Tests.Meta;

public sealed class MetaSecurityTests
{
    [Fact]
    public void OAuth_state_and_access_token_are_protected_and_recoverable()
    {
        var keyDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "analitiads-tests", Guid.NewGuid().ToString("N")));
        var provider = DataProtectionProvider.Create(keyDirectory, configuration => configuration.SetApplicationName("AnalitiAds.Tests"));
        var stateProtector = new MetaOAuthStateProtector(provider, TimeProvider.System);
        var tokenProtector = new MetaTokenProtector(provider);
        var state = new MetaOAuthState(Guid.NewGuid(), Guid.NewGuid());

        var protectedState = stateProtector.Protect(state);
        var protectedToken = tokenProtector.Protect("sensitive-token");

        Assert.DoesNotContain(state.AgencyId.ToString(), protectedState, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-token", protectedToken, StringComparison.Ordinal);
        Assert.Equal(state, stateProtector.Unprotect(protectedState));
        Assert.Equal("sensitive-token", tokenProtector.Unprotect(protectedToken));
    }
}
