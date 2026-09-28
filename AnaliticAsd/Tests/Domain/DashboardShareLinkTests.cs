using AnaliticAsd.Domain.Dashboards;
namespace AnaliticAsd.Tests.Domain;
public sealed class DashboardShareLinkTests
{
 [Fact] public void Revocation_invalidates_link_immediately(){var now=DateTimeOffset.Parse("2026-09-22T12:00:00Z");var link=DashboardShareLink.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new string('A',64),null,null,now.AddDays(1),true,false,true,now);Assert.True(link.IsAvailable(now));link.Revoke(now);Assert.False(link.IsAvailable(now));Assert.Equal("Revoked",link.Status(now));}
 [Fact] public void Link_can_require_password_and_recipient_without_exposing_secret(){var now=DateTimeOffset.UtcNow;var link=DashboardShareLink.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new string('B',64),null,"CLIENT@EXAMPLE.COM",null,false,false,false,now);link.SetPasswordHash("server-side-password-hash");Assert.Equal("client@example.com",link.RecipientEmail);Assert.NotNull(link.PasswordHash);Assert.Null(link.ExpiresAtUtc);}
 [Fact] public void Branding_rejects_insecure_logo_urls(){Assert.Throws<ArgumentException>(()=>BrandProfile.Create(Guid.NewGuid(),null,"http://example.com/logo.png","#112233","#445566","#FFFFFF","Arial",DateTimeOffset.UtcNow));}
}
