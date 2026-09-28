using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AnaliticAsd.Application.Dashboards;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Application.Reports;
using AnaliticAsd.Contracts.Clients;
using AnaliticAsd.Contracts.Folders;
using AnaliticAsd.Contracts.Identity;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Meta;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnaliticAsd.Tests.Api;

// Real JWT middleware, controllers, application services and relational EF queries. No Meta credentials.
public sealed class ClientAccessApiTests
{
    private const string Password = "ClientPassword123";
    private const string Email = "client@example.test";
    private const string AcceptRoute = "/api/v1/auth/client-invitations/accept";

    [Fact]
    public async Task Invitation_grants_only_assigned_client_and_all_four_metric_levels_are_scoped()
    {
        await using var app = await TestApp.Create();
        var own = await app.Seed(app.Owner.AgencyId, "Assigned", "101");
        var sameAgency = await app.Seed(app.Owner.AgencyId, "Private", "102");
        var otherOwner = await app.Register("other@example.test");
        var otherAgency = await app.Seed(otherOwner.AgencyId, "Other agency", "103");
        var invite = await app.Invite(own.ClientId);
        var auth = await app.Accept(invite);
        Assert.Equal("ClientViewer", auth.Role);
        Assert.Equal(app.Owner.AgencyId, auth.AgencyId);
        using var external = app.As(auth);
        var clients = await external.GetFromJsonAsync<ClientResponse[]>($"/api/v1/clients?agencyId={otherOwner.AgencyId}");
        Assert.Equal(own.ClientId, Assert.Single(clients!).Id);
        foreach (var route in own.ReadRoutes())
        {
            var response = await external.GetAsync(route);
            Assert.True(response.IsSuccessStatusCode, $"{route}: {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            Assert.True(response.Headers.CacheControl?.NoStore);
        }
        foreach (var hidden in new[] { sameAgency, otherAgency })
            foreach (var route in hidden.ReadRoutes())
                Assert.Equal(HttpStatusCode.NotFound, (await external.GetAsync(route)).StatusCode);
        foreach (var group in new[] { "campaigns", "ad-sets", "ads" })
            Assert.Equal(HttpStatusCode.NotFound, (await external.GetAsync($"/api/v1/{group}/{Guid.NewGuid()}/metrics{Seeded.Range}")).StatusCode);
        var metrics = await external.GetFromJsonAsync<System.Text.Json.JsonElement[]>($"/api/v1/ad-accounts/{own.AccountId}/metrics{Seeded.Range}");
        Assert.Equal(12.50m, Assert.Single(metrics!).GetProperty("observed").GetProperty("spend").GetDecimal());
        Assert.Equal("FieldPresencePreserved", Assert.Single(metrics!).GetProperty("observedDataQuality").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await external.GetAsync("/api/v1/meta/connection")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.GetAsync("/api/v1/meta/oauth/start")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.GetAsync("/api/v1/meta/ad-accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await external.GetAsync($"/api/v1/clients/{own.ClientId}/folders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.PostAsJsonAsync($"/api/v1/ad-accounts/{own.AccountId}/sync", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.PostAsJsonAsync($"/api/v1/ad-accounts/{own.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.PostAsJsonAsync("/api/v1/clients", new { name = "Forbidden" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await external.DeleteAsync($"/api/v1/ad-accounts/{own.AccountId}")).StatusCode);
        using var owner = app.As(app.Owner);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/clients/{sameAgency.ClientId}")).StatusCode);
        var all = await owner.GetFromJsonAsync<ClientInvitationModel[]>($"/api/v1/clients/{own.ClientId}/invitations");
        Assert.Equal("Accepted", Assert.Single(all!).Status);
        var users = await owner.GetFromJsonAsync<ClientAccessModel[]>($"/api/v1/clients/{own.ClientId}/users");
        Assert.Equal(auth.UserId, Assert.Single(users!).UserId);
    }

    [Fact]
    public async Task Revoking_one_grant_immediately_blocks_old_JWT_and_pending_reinvites_but_preserves_other_grant()
    {
        await using var app = await TestApp.Create();
        var first = await app.Seed(app.Owner.AgencyId, "First", "201");
        var second = await app.Seed(app.Owner.AgencyId, "Second", "202");
        var invitation = await app.Invite(first.ClientId);
        var pending = await app.Invite(first.ClientId);
        var auth = await app.Accept(invitation);
        await app.Accept(await app.Invite(second.ClientId)); // Existing user must prove password; it is never reset.
        using var owner = app.As(app.Owner);
        using var external = app.As(auth);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/clients/{first.ClientId}/users/{auth.UserId}")).StatusCode);
        foreach (var route in first.ReadRoutes()) Assert.Equal(HttpStatusCode.NotFound, (await external.GetAsync(route)).StatusCode);
        Assert.Equal(second.ClientId, Assert.Single((await external.GetFromJsonAsync<ClientResponse[]>("/api/v1/clients"))!).Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(pending)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await external.GetAsync($"/api/v1/ad-accounts/{second.AccountId}/metrics{Seeded.Range}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/clients/{second.ClientId}/users/{auth.UserId}")).StatusCode);
        Assert.Empty((await external.GetFromJsonAsync<ClientResponse[]>("/api/v1/clients"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/v1/clients/{second.ClientId}/users/{auth.UserId}")).StatusCode);
    }

    [Theory]
    [InlineData(AgencyRole.Admin, true)]
    [InlineData(AgencyRole.Analyst, false)]
    [InlineData(AgencyRole.Viewer, false)]
    [InlineData(AgencyRole.ClientViewer, false)]
    public async Task Only_owner_admin_can_manage_access_and_internal_viewer_keeps_agency_read_access(AgencyRole role, bool allowed)
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "301");
        using var actor = app.As(await app.UserWithRole(role));
        var route = $"/api/v1/clients/{data.ClientId}";
        foreach (var path in new[] { "users", "invitations" })
            Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await actor.GetAsync($"{route}/{path}")).StatusCode);
        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.Forbidden,
            (await actor.PostAsJsonAsync($"{route}/invitations", new { email = Email })).StatusCode);
        Assert.Equal(role is AgencyRole.ClientViewer or AgencyRole.Analyst ? HttpStatusCode.NotFound : HttpStatusCode.OK,
            (await actor.GetAsync(route)).StatusCode);
        Assert.Equal(allowed ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, (await actor.DeleteAsync($"{route}/users/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(allowed ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, (await actor.DeleteAsync($"{route}/invitations/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Invitations_are_hashed_single_use_revocable_expiring_and_do_not_accept_agency_or_role_injection()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "401");
        using var owner = app.As(app.Owner);
        var route = $"/api/v1/clients/{data.ClientId}/invitations";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(route, new { email = Email, agencyId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(route, new { email = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync(route, new { email = app.Owner.Email })).StatusCode);
        var invitation = await app.Invite(data.ClientId);
        using (var scope = app.Services.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().ClientInvitations.SingleAsync();
            Assert.Equal(64, stored.TokenHash.Length);
            Assert.NotEqual(invitation.InvitationToken, stored.TokenHash);
        }
        Assert.DoesNotContain(invitation.InvitationToken, await owner.GetStringAsync(route));
        Assert.Equal(HttpStatusCode.BadRequest, (await app.Http.PostAsJsonAsync(AcceptRoute, new
        { invitationToken = invitation.InvitationToken, email = Email, password = Password, role = "Owner" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(invitation, email: "wrong@example.test")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(invitation, password: "short")).StatusCode);
        await app.Accept(invitation);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(invitation)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync($"{route}/{invitation.Invitation.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync(route, new { email = Email })).StatusCode);
        var revoked = await app.Invite(data.ClientId, "revoked@example.test");
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{route}/{revoked.Invitation.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(revoked, email: revoked.Invitation.Email)).StatusCode);
        var expired = await app.Invite(data.ClientId, "expired@example.test");
        app.Clock.Now = expired.Invitation.ExpiresAtUtc;
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(expired, email: expired.Invitation.Email)).StatusCode);
    }

    [Fact]
    public async Task Existing_users_need_their_password_and_cross_agency_invitation_management_is_hidden()
    {
        await using var app = await TestApp.Create();
        var first = await app.Seed(app.Owner.AgencyId, "Client", "501");
        var second = await app.Seed(app.Owner.AgencyId, "Other", "502");
        await app.Accept(await app.Invite(first.ClientId));
        var invitation = await app.Invite(second.ClientId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.AcceptRaw(invitation, password: "WrongPassword123")).StatusCode);
        await app.Accept(invitation);
        var login = await app.Http.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("ClientViewer", (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Role);
        using var stranger = app.As(await app.Register("stranger@example.test"));
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/clients/{first.ClientId}/users")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync($"/api/v1/clients/{first.ClientId}/invitations", new { email = "x@example.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/v1/clients/{second.ClientId}/invitations/{invitation.Invitation.Id}")).StatusCode);
    }

    [Fact]
    public async Task Inactive_client_hides_data_and_revoked_membership_invalidates_previously_signed_JWT()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "601");
        var auth = await app.Accept(await app.Invite(data.ClientId));
        var pending = await app.Invite(data.ClientId, "pending@example.test");
        using var external = app.As(auth);
        using var owner = app.As(app.Owner);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/v1/clients/{data.ClientId}", new { name = "Client", isActive = false })).StatusCode);
        foreach (var route in data.ReadRoutes()) Assert.Equal(HttpStatusCode.NotFound, (await external.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await app.AcceptRaw(pending, email: pending.Invitation.Email)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/clients/{data.ClientId}/invitations", new { email = "new@example.test" })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
            db.Memberships.Remove(await db.Memberships.SingleAsync(x => x.UserId == auth.UserId));
            await db.SaveChangesAsync();
        }
        var response = await external.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Concurrent_consumption_rolls_back_grant_and_foreign_key_rejects_cross_agency_assignment()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "701");
        var auth = await app.UserWithRole(AgencyRole.ClientViewer);
        var invitation = await app.Invite(data.ClientId, auth.Email);
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();
        var first = scope1.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
        var second = scope2.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
        var one = await first.ClientInvitations.SingleAsync(x => x.Id == invitation.Invitation.Id);
        var two = await second.ClientInvitations.SingleAsync(x => x.Id == invitation.Invitation.Id);
        one.Revoke(app.Clock.Now);
        await first.SaveChangesAsync();
        two.Accept(app.Clock.Now);
        second.ClientAccesses.Add(ClientAccess.Create(app.Owner.AgencyId, data.ClientId, auth.UserId, app.Clock.Now));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        Assert.Empty(await first.ClientAccesses.AsNoTracking().ToArrayAsync());
        second.ChangeTracker.Clear();
        var otherOwner = await app.Register("foreign@example.test");
        second.ClientAccesses.Add(ClientAccess.Create(otherOwner.AgencyId, data.ClientId, otherOwner.UserId, app.Clock.Now));
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Invitation_acceptance_rate_limit_returns_problem_details_without_tokens()
    {
        await using var app = await TestApp.Create();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await app.Http.PostAsJsonAsync(AcceptRoute,
                new { invitationToken = new string('a', 43), email = Email, password = Password })).StatusCode);
        var response = await app.Http.PostAsJsonAsync(AcceptRoute, new { });
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Reports_are_immutable_pdf_backed_and_follow_current_client_access()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Report client", "750");
        using var owner = app.As(app.Owner);
        var create = await owner.PostAsJsonAsync("/api/v1/reports", new
        {
            title = "Reporte de campaña",
            adAccountId = data.AccountId,
            since = Seeded.Day,
            until = Seeded.Day,
            comparison = (string?)null,
            campaignIds = new[] { data.CampaignId },
            selectedMetrics = new[] { "spend", "ctr" }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var report = (await create.Content.ReadFromJsonAsync<ReportDataModel>())!;
        Assert.Equal(data.ClientId, report.ClientId);
        Assert.Equal(64, report.SnapshotHash.Length);
        Assert.Empty(report.Analysis.Insights);
        Assert.NotNull(create.Headers.Location);

        var before = await owner.GetStringAsync($"/api/v1/reports/{report.ReportId}");
        var pdf = await owner.GetAsync($"/api/v1/reports/{report.ReportId}/pdf");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("attachment", pdf.Content.Headers.ContentDisposition?.DispositionType);
        var pdfBytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-1.7", System.Text.Encoding.Latin1.GetString(pdfBytes[..8]));
        if (Environment.GetEnvironmentVariable("ANALITIADS_PDF_SAMPLE_PATH") is { Length: > 0 } samplePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(samplePath)!);
            await File.WriteAllBytesAsync(samplePath, pdfBytes);
        }

        await app.EnableMetricsSync(data.AccountId);
        app.Meta.Values = TestApp.Insights("750", Seeded.Day, 99m);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync",
            new { since = Seeded.Day, until = Seeded.Day })).StatusCode);
        Assert.Equal(before, await owner.GetStringAsync($"/api/v1/reports/{report.ReportId}"));

        var viewer = await app.UserWithRole(AgencyRole.Viewer);
        using (var viewerClient = app.As(viewer))
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await viewerClient.PostAsJsonAsync("/api/v1/reports", new
            { title = "Forbidden", adAccountId = data.AccountId, since = Seeded.Day, until = Seeded.Day })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await viewerClient.GetAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);
        }

        var external = await app.Accept(await app.Invite(data.ClientId));
        using (var externalClient = app.As(external))
        {
            Assert.Equal(HttpStatusCode.OK, (await externalClient.GetAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);
            Assert.Single((await externalClient.GetFromJsonAsync<ReportListItemModel[]>($"/api/v1/clients/{data.ClientId}/reports"))!);
            Assert.Equal(HttpStatusCode.Forbidden, (await externalClient.DeleteAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/clients/{data.ClientId}/users/{external.UserId}")).StatusCode);
        using (var revoked = app.As(external))
            Assert.Equal(HttpStatusCode.NotFound, (await revoked.GetAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);

        using var foreign = app.As(await app.Register("report.foreign@example.test"));
        Assert.Equal(HttpStatusCode.NotFound, (await foreign.GetAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await foreign.GetAsync($"/api/v1/clients/{data.ClientId}/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/reports/{report.ReportId}")).StatusCode);
    }

    [Fact]
    public async Task Shared_report_links_are_hashed_expiring_revocable_read_only_and_audited()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Shared report", "760");
        using var owner = app.As(app.Owner);
        var reportResponse = await owner.PostAsJsonAsync("/api/v1/reports", new
        {
            title = "Public snapshot", adAccountId = data.AccountId, since = Seeded.Day, until = Seeded.Day,
            campaignIds = new[] { data.CampaignId }, selectedMetrics = new[] { "spend", "ctr" }
        });
        var report = (await reportResponse.Content.ReadFromJsonAsync<ReportDataModel>())!;
        var route = $"/api/v1/reports/{report.ReportId}/share-links";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(route, new { expirationDays = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(route, new { expirationDays = 7, role = "Owner" })).StatusCode);

        var create = await owner.PostAsJsonAsync(route, new { expirationDays = 7 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var shared = (await create.Content.ReadFromJsonAsync<CreatedReportShareLinkModel>())!;
        Assert.Equal(43, shared.AccessToken.Length);
        Assert.EndsWith($"#{shared.AccessToken}", shared.SharePath);
        var listed = await owner.GetStringAsync(route);
        Assert.DoesNotContain(shared.AccessToken, listed);

        using (var scope = app.Services.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().ReportShareLinks.SingleAsync();
            Assert.Equal(64, stored.TokenHash.Length);
            Assert.NotEqual(shared.AccessToken, stored.TokenHash);
        }

        var access = await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access", new { accessToken = shared.AccessToken });
        Assert.Equal(HttpStatusCode.OK, access.StatusCode);
        Assert.True(access.Headers.CacheControl?.NoStore);
        var publicData = (await access.Content.ReadFromJsonAsync<SharedReportDataModel>())!;
        Assert.Equal(report.Title, publicData.Title);
        Assert.Equal(report.Analysis.AdAccountId, publicData.Analysis.AdAccountId);
        var pdf = await app.Http.PostAsJsonAsync("/api/v1/shared-reports/pdf", new { accessToken = shared.AccessToken });
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF-1.7", System.Text.Encoding.Latin1.GetString((await pdf.Content.ReadAsByteArrayAsync())[..8]));

        using (var scope = app.Services.CreateScope())
            Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().ReportShareAuditEvents.CountAsync());

        using (var viewer = app.As(await app.UserWithRole(AgencyRole.Viewer)))
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync(route, new { expirationDays = 7 })).StatusCode);
        using (var foreign = app.As(await app.Register("share.foreign@example.test")))
            Assert.Equal(HttpStatusCode.NotFound, (await foreign.PostAsJsonAsync(route, new { expirationDays = 7 })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"{route}/{shared.ShareLink.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access",
            new { accessToken = shared.AccessToken })).StatusCode);

        var expiring = (await (await owner.PostAsJsonAsync(route, new { expirationDays = 1 }))
            .Content.ReadFromJsonAsync<CreatedReportShareLinkModel>())!;
        app.Clock.Now = expiring.ShareLink.ExpiresAtUtc;
        Assert.Equal(HttpStatusCode.NotFound, (await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access",
            new { accessToken = expiring.AccessToken })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access",
            new { accessToken = new string('a', 43) })).StatusCode);
    }

    [Fact]
    public async Task Shared_report_public_access_is_rate_limited()
    {
        await using var app = await TestApp.Create();
        for (var i = 0; i < 60; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access",
                new { accessToken = new string((char)('a' + i % 20), 43) })).StatusCode);
        var response = await app.Http.PostAsJsonAsync("/api/v1/shared-reports/access", new { accessToken = new string('z', 43) });
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
    }

    [Fact]
    public async Task Metrics_sync_replaces_snapshots_and_exposes_safe_range_summary_and_selector()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "801");
        await app.EnableMetricsSync(data.AccountId);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
            db.InsightSnapshots.Add(InsightSnapshot.Create(data.AccountId, null, null, null, InsightLevel.Account,
                new(InsightLevel.Account, Seeded.Day.AddDays(-1), null, null, null, 10m, 50, 25, 2, 1, 1, 10), "USD", app.Clock.Now));
            await db.SaveChangesAsync();
        }
        app.Meta.Values = TestApp.Insights("801", Seeded.Day, 12.50m);
        using var owner = app.As(app.Owner);

        var first = await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        app.Meta.Values = TestApp.Insights("801", Seeded.Day, 25m);
        var second = await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using (var scope = app.Services.CreateScope())
        {
            var snapshots = await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().InsightSnapshots.ToArrayAsync();
            Assert.Equal(5, snapshots.Length);
            Assert.All(snapshots, x => Assert.Equal(InsightSnapshotDataQuality.FieldPresencePreserved, x.ObservedDataQuality));
            Assert.Equal(25m, Assert.Single(snapshots, x => x.Level == InsightLevel.Account && x.SnapshotDate == Seeded.Day).Spend);
        }

        var summary = await owner.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/ad-accounts/{data.AccountId}/metrics/summary{Seeded.Range}");
        Assert.Equal("CompleteForSnapshots", summary.GetProperty("observed").GetProperty("spend").GetProperty("availability").GetString());
        Assert.Equal(25m, summary.GetProperty("observed").GetProperty("spend").GetProperty("value").GetDecimal());
        Assert.Equal("NotAvailableForRange", summary.GetProperty("observed").GetProperty("reach").GetProperty("availability").GetString());
        Assert.Equal("NotAvailableForRange", summary.GetProperty("derived").GetProperty("frequency").GetProperty("availability").GetString());

        var selector = await owner.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/ad-accounts/{data.AccountId}/campaigns/selector{Seeded.Range}");
        Assert.Equal("WithActivity", selector.GetProperty("activityFilter").GetString());
        Assert.Equal(data.CampaignId, Assert.Single(selector.GetProperty("campaigns").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/api/v1/ad-accounts/{data.AccountId}/campaigns/selector{Seeded.Range}&activity=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/summary?since={Seeded.Day.AddDays(-90):yyyy-MM-dd}&until={Seeded.Day:yyyy-MM-dd}")).StatusCode);

        var comparison = await owner.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/ad-accounts/{data.AccountId}/metrics/comparison{Seeded.Range}&comparison=PreviousPeriod");
        var spendComparison = comparison.GetProperty("observed").GetProperty("spend");
        Assert.Equal(15m, spendComparison.GetProperty("absoluteChange").GetDecimal());
        Assert.Equal(150m, spendComparison.GetProperty("percentageChange").GetDecimal());
        Assert.Equal("Available", spendComparison.GetProperty("availability").GetString());
        Assert.Equal("CurrentUnavailable", comparison.GetProperty("observed").GetProperty("reach").GetProperty("availability").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/comparison{Seeded.Range}&comparison=Custom")).StatusCode);
    }

    [Fact]
    public async Task Metrics_sync_rejects_insights_for_an_unknown_hierarchy_entity()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "901");
        await app.EnableMetricsSync(data.AccountId);
        app.Meta.Values =
        [
            new RemoteInsight(InsightLevel.Campaign, Seeded.Day, "unknown", null, null, 1m, 1, 1, 1, null, null, null)
        ];
        using var owner = app.As(app.Owner);

        var response = await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_daily_response_preserves_missing_observed_fields_as_explicit_nulls()
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", "904");
        await app.EnableMetricsSync(data.AccountId);
        app.Meta.Values =
        [
            new RemoteInsight(InsightLevel.Account, Seeded.Day, null, null, null, null, null, null, null, null, null, null)
        ];
        using var owner = app.As(app.Owner);

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day })).StatusCode);
        var response = await owner.GetFromJsonAsync<System.Text.Json.JsonElement[]>($"/api/v1/ad-accounts/{data.AccountId}/metrics{Seeded.Range}");
        var metric = Assert.Single(response!);
        var observed = metric.GetProperty("observed");

        Assert.Equal(System.Text.Json.JsonValueKind.Null, observed.GetProperty("spend").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, observed.GetProperty("impressions").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, observed.GetProperty("purchaseValue").ValueKind);
        Assert.Equal("FieldPresencePreserved", metric.GetProperty("observedDataQuality").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Metrics_sync_rejects_duplicate_or_out_of_requested_range_insights(bool duplicate)
    {
        await using var app = await TestApp.Create();
        var data = await app.Seed(app.Owner.AgencyId, "Client", duplicate ? "902" : "903");
        await app.EnableMetricsSync(data.AccountId);
        var value = new RemoteInsight(InsightLevel.Account, duplicate ? Seeded.Day : Seeded.Day.AddDays(-1), null, null, null, 1m, 1, 1, 1, null, null, null);
        app.Meta.Values = duplicate ? [value, value] : [value];
        using var owner = app.As(app.Owner);

        var response = await owner.PostAsJsonAsync($"/api/v1/ad-accounts/{data.AccountId}/metrics/sync", new { since = Seeded.Day, until = Seeded.Day });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:3001", true)]
    [InlineData("http://localhost:3000", false)]
    [InlineData("http://127.0.0.1:3001", false)]
    public async Task Development_CORS_allows_only_the_authorized_frontend_origin(string origin, bool allowed)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureLogging(x => x.ClearProviders());
            b.ConfigureServices(services => services.AddDataProtection().UseEphemeralDataProtectionProvider());
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/clients");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        var response = await client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        if (allowed) Assert.Equal(origin, Assert.Single(origins!));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("http://localhost:3001/app/configuracion/integraciones/meta", factory.Services.GetRequiredService<IConfiguration>()["Meta:FrontendCallbackUrl"]);
    }

    [Fact]
    public async Task Analysis_is_structured_selective_and_tenant_safe()
    {
        await using var app = await TestApp.Create();
        var own = await app.Seed(app.Owner.AgencyId, "Analysis client", "950");
        var otherOwner = await app.Register("analysis.foreign@example.test");
        var foreign = await app.Seed(otherOwner.AgencyId, "Foreign analysis client", "951");
        var invitation = await app.Invite(own.ClientId);
        var clientViewer = await app.Accept(invitation);
        var request = new
        {
            adAccountId = own.AccountId,
            since = Seeded.Day,
            until = Seeded.Day,
            comparison = "PreviousPeriod",
            campaignIds = new[] { own.CampaignId },
            selectedMetrics = new[] { "spend", "ctr" }
        };

        using var owner = app.As(app.Owner);
        var response = await owner.PostAsJsonAsync("/api/v1/analyses", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(own.AccountId, json.GetProperty("adAccountId").GetGuid());
        Assert.Equal("PreviousPeriod", json.GetProperty("comparisonType").GetString());
        Assert.Equal(2, json.GetProperty("selectedMetrics").GetArrayLength());
        Assert.Equal(2, json.GetProperty("account").GetProperty("metrics").EnumerateObject().Count());
        Assert.Equal("Sufficient", json.GetProperty("account").GetProperty("sufficiency").GetProperty("status").GetString());
        Assert.Single(json.GetProperty("campaigns").EnumerateArray());
        Assert.Single(json.GetProperty("adSets").EnumerateArray());
        Assert.Single(json.GetProperty("ads").EnumerateArray());
        Assert.Equal(3, json.GetProperty("unavailableSections").GetArrayLength());

        using var viewer = app.As(clientViewer);
        Assert.Equal(HttpStatusCode.OK, (await viewer.PostAsJsonAsync("/api/v1/analyses", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.PostAsJsonAsync("/api/v1/analyses", request with { adAccountId = foreign.AccountId })).StatusCode);
        using var foreignActor = app.As(otherOwner);
        Assert.Equal(HttpStatusCode.NotFound, (await foreignActor.PostAsJsonAsync("/api/v1/analyses", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/v1/analyses", request with { campaignIds = new[] { Guid.NewGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/v1/analyses", request with { selectedMetrics = new[] { "invented" } })).StatusCode);

        var emptyDay = Seeded.Day.AddDays(-20);
        var noDataRequest = new
        {
            adAccountId = own.AccountId,
            since = emptyDay,
            until = emptyDay,
            comparison = (string?)null,
            comparisonSince = (DateOnly?)null,
            comparisonUntil = (DateOnly?)null,
            campaignIds = Array.Empty<Guid>(),
            selectedMetrics = new[] { "spend" }
        };
        var noData = await owner.PostAsJsonAsync("/api/v1/analyses", noDataRequest);
        Assert.Equal(HttpStatusCode.OK, noData.StatusCode);
        var noDataJson = await noData.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Insufficient", noDataJson.GetProperty("account").GetProperty("sufficiency").GetProperty("status").GetString());
        Assert.Empty(noDataJson.GetProperty("campaigns").EnumerateArray());
    }

    [Fact]
    public async Task Editor_assignment_scopes_existing_JWT_and_folder_lifecycle_is_isolated()
    {
        await using var app = await TestApp.Create();
        var assignedClient = await app.Seed(app.Owner.AgencyId, "Assigned editor client", "9201");
        var hiddenClient = await app.Seed(app.Owner.AgencyId, "Hidden editor client", "9202");
        var analyst = await app.UserWithRole(AgencyRole.Analyst);
        var viewer = await app.UserWithRole(AgencyRole.Viewer);
        using var owner = app.As(app.Owner);
        using var editor = app.As(analyst);

        Assert.Empty((await editor.GetFromJsonAsync<ClientResponse[]>("/api/v1/clients"))!);
        Assert.Equal(HttpStatusCode.NotFound,
            (await editor.GetAsync($"/api/v1/clients/{assignedClient.ClientId}/folders")).StatusCode);

        var eligible = await owner.GetFromJsonAsync<AgencyEditorModel[]>("/api/v1/agency/editors");
        Assert.Contains(eligible!, candidate => candidate.UserId == analyst.UserId);
        Assert.DoesNotContain(eligible!, candidate => candidate.UserId == viewer.UserId);
        var assignmentResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{assignedClient.ClientId}/editors", new { userId = analyst.UserId });
        Assert.Equal(HttpStatusCode.Created, assignmentResponse.StatusCode);
        var assignment = (await assignmentResponse.Content.ReadFromJsonAsync<ClientEditorModel>())!;

        var visibleClients = (await editor.GetFromJsonAsync<ClientResponse[]>("/api/v1/clients"))!;
        Assert.Equal(assignedClient.ClientId, Assert.Single(visibleClients).Id);
        Assert.Equal(HttpStatusCode.NotFound,
            (await editor.GetAsync($"/api/v1/clients/{hiddenClient.ClientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await editor.GetAsync($"/api/v1/ad-accounts/{assignedClient.AccountId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await editor.GetAsync($"/api/v1/ad-accounts/{hiddenClient.AccountId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await editor.PostAsJsonAsync("/api/v1/clients", new { name = "Forbidden client" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await editor.PutAsJsonAsync($"/api/v1/clients/{assignedClient.ClientId}",
                new { name = "Forbidden update", isActive = true })).StatusCode);

        var rootResponse = await editor.PostAsJsonAsync($"/api/v1/clients/{assignedClient.ClientId}/folders",
            new { name = " Campañas ", parentFolderId = (Guid?)null });
        Assert.Equal(HttpStatusCode.Created, rootResponse.StatusCode);
        var root = (await rootResponse.Content.ReadFromJsonAsync<FolderResponse>())!;
        Assert.Equal("Campañas", root.Name);
        var childResponse = await editor.PostAsJsonAsync($"/api/v1/clients/{assignedClient.ClientId}/folders",
            new { name = "Ventas", parentFolderId = root.Id });
        Assert.Equal(HttpStatusCode.Created, childResponse.StatusCode);
        var child = (await childResponse.Content.ReadFromJsonAsync<FolderResponse>())!;
        Assert.Equal(root.Id, child.ParentFolderId);

        var cycle = await editor.PostAsJsonAsync($"/api/v1/folders/{root.Id}/move",
            new { parentFolderId = child.Id, sortOrder = 0, expectedVersion = root.Version });
        Assert.Equal(HttpStatusCode.Conflict, cycle.StatusCode);

        var foreignFolderResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{hiddenClient.ClientId}/folders", new { name = "Foreign" });
        var foreignFolder = (await foreignFolderResponse.Content.ReadFromJsonAsync<FolderResponse>())!;
        var crossClientMove = await owner.PostAsJsonAsync($"/api/v1/folders/{root.Id}/move",
            new { parentFolderId = foreignFolder.Id, sortOrder = 0, expectedVersion = root.Version });
        Assert.Equal(HttpStatusCode.Conflict, crossClientMove.StatusCode);

        var search = await editor.GetFromJsonAsync<FolderResponse[]>(
            $"/api/v1/clients/{assignedClient.ClientId}/folders?search=ventas");
        Assert.Equal(child.Id, Assert.Single(search!).Id);
        Assert.Equal(HttpStatusCode.NoContent,
            (await editor.DeleteAsync($"/api/v1/folders/{root.Id}?expectedVersion={root.Version}")).StatusCode);
        Assert.Empty((await editor.GetFromJsonAsync<FolderResponse[]>(
            $"/api/v1/clients/{assignedClient.ClientId}/folders"))!);
        var archived = (await editor.GetFromJsonAsync<FolderResponse[]>(
            $"/api/v1/clients/{assignedClient.ClientId}/folders?includeArchived=true"))!;
        Assert.Equal(2, archived.Length);
        Assert.All(archived, folder => Assert.True(folder.IsArchived));
        var archivedRoot = archived.Single(folder => folder.Id == root.Id);

        var staleRename = await editor.PatchAsJsonAsync($"/api/v1/folders/{root.Id}",
            new { name = "Stale", expectedVersion = root.Version });
        Assert.Equal(HttpStatusCode.Conflict, staleRename.StatusCode);
        var restore = await editor.PostAsJsonAsync($"/api/v1/folders/{root.Id}/restore",
            new { expectedVersion = archivedRoot.Version });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.All((await editor.GetFromJsonAsync<FolderResponse[]>(
            $"/api/v1/clients/{assignedClient.ClientId}/folders"))!, folder => Assert.False(folder.IsArchived));

        using var internalViewer = app.As(viewer);
        Assert.Equal(HttpStatusCode.OK,
            (await internalViewer.GetAsync($"/api/v1/clients/{assignedClient.ClientId}/folders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await internalViewer.PostAsJsonAsync($"/api/v1/clients/{assignedClient.ClientId}/folders",
                new { name = "Forbidden" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.DeleteAsync($"/api/v1/clients/{assignedClient.ClientId}/editors/{analyst.UserId}?expectedVersion={assignment.Version}")).StatusCode);
        Assert.Empty((await editor.GetFromJsonAsync<ClientResponse[]>("/api/v1/clients"))!);
        Assert.Equal(HttpStatusCode.NotFound,
            (await editor.GetAsync($"/api/v1/clients/{assignedClient.ClientId}/folders")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await editor.GetAsync($"/api/v1/ad-accounts/{assignedClient.AccountId}")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_drafts_publications_duplicates_templates_and_permissions_are_persistent_and_isolated()
    {
        await using var app = await TestApp.Create();
        var sourceClient = await app.Seed(app.Owner.AgencyId, "Dashboard source", "9251");
        var destinationClient = await app.Seed(app.Owner.AgencyId, "Dashboard destination", "9252");
        using var owner = app.As(app.Owner);

        var folderResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{sourceClient.ClientId}/folders", new { name = "Informes" });
        Assert.Equal(HttpStatusCode.Created, folderResponse.StatusCode);
        var folder = (await folderResponse.Content.ReadFromJsonAsync<FolderResponse>())!;

        var createResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{sourceClient.ClientId}/dashboards",
            new { title = " Rendimiento ", description = " Informe editable " });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var dashboard = (await createResponse.Content.ReadFromJsonAsync<DashboardModel>())!;
        Assert.Equal("Rendimiento", dashboard.Title);
        Assert.Equal(1, dashboard.DraftRevision);

        var moveResponse = await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/move",
            new { folderId = folder.Id, expectedVersion = dashboard.Version });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        dashboard = (await moveResponse.Content.ReadFromJsonAsync<DashboardModel>())!;
        Assert.Equal(folder.Id, dashboard.FolderId);

        var foreignFolderResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{destinationClient.ClientId}/folders", new { name = "Foreign" });
        var foreignFolder = (await foreignFolderResponse.Content.ReadFromJsonAsync<FolderResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/move",
                new { folderId = foreignFolder.Id, expectedVersion = dashboard.Version })).StatusCode);

        var blank = await owner.GetFromJsonAsync<DashboardDraftModel>($"/api/v1/dashboards/{dashboard.Id}/draft");
        Assert.Equal(1, blank!.Revision);
        var firstDefinition = DashboardDefinition(sourceClient.AccountId, metric: "spend");
        var saveResponse = await owner.PutAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/draft",
            new { expectedRevision = blank.Revision, definition = firstDefinition });
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var saved = (await saveResponse.Content.ReadFromJsonAsync<DashboardDraftModel>())!;
        Assert.Equal(2, saved.Revision);
        Assert.Equal(sourceClient.AccountId, FirstDashboardSource(saved.Definition).GetProperty("dataSourceId").GetGuid());

        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PutAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/draft",
                new { expectedRevision = 1, definition = firstDefinition })).StatusCode);
        var publishResponse = await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/publish",
            new { expectedRevision = saved.Revision });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = (await publishResponse.Content.ReadFromJsonAsync<PublishedDashboardModel>())!;
        Assert.Equal(1, published.PublicationNumber);
        Assert.Equal(64, published.DefinitionHash.Length);

        var secondDefinition = DashboardDefinition(sourceClient.AccountId, metric: "clicks");
        var nextDraft = (await (await owner.PutAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/draft",
            new { expectedRevision = saved.Revision, definition = secondDefinition }))
            .Content.ReadFromJsonAsync<DashboardDraftModel>())!;
        Assert.Equal(3, nextDraft.Revision);
        var stillPublished = await owner.GetFromJsonAsync<PublishedDashboardModel>(
            $"/api/v1/dashboards/{dashboard.Id}/published");
        Assert.Equal(published.DefinitionHash, stillPublished!.DefinitionHash);
        Assert.Equal("spend", FirstDashboardComponent(stillPublished.Definition).GetProperty("data")
            .GetProperty("configuration").GetProperty("metric").GetString());

        var sameClientCopyResponse = await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/duplicate",
            new { destinationClientId = sourceClient.ClientId, title = "Copia local" });
        Assert.Equal(HttpStatusCode.Created, sameClientCopyResponse.StatusCode);
        var sameClientCopy = (await sameClientCopyResponse.Content.ReadFromJsonAsync<DashboardModel>())!;
        var sameClientDraft = await owner.GetFromJsonAsync<DashboardDraftModel>(
            $"/api/v1/dashboards/{sameClientCopy.Id}/draft");
        Assert.Equal(sourceClient.AccountId,
            FirstDashboardSource(sameClientDraft!.Definition).GetProperty("dataSourceId").GetGuid());

        var crossClientCopyResponse = await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/duplicate",
            new { destinationClientId = destinationClient.ClientId, title = "Copia segura" });
        Assert.Equal(HttpStatusCode.Created, crossClientCopyResponse.StatusCode);
        var crossClientCopy = (await crossClientCopyResponse.Content.ReadFromJsonAsync<DashboardModel>())!;
        var crossClientDraft = await owner.GetFromJsonAsync<DashboardDraftModel>(
            $"/api/v1/dashboards/{crossClientCopy.Id}/draft");
        var unboundSource = FirstDashboardSource(crossClientDraft!.Definition);
        Assert.False(unboundSource.TryGetProperty("dataSourceId", out _));
        Assert.Equal("rebind-source-1", unboundSource.GetProperty("sourceSlot").GetString());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await owner.PostAsJsonAsync($"/api/v1/dashboards/{crossClientCopy.Id}/publish",
                new { expectedRevision = crossClientDraft.Revision })).StatusCode);

        var templateResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{sourceClient.ClientId}/dashboard-templates",
            new { name = "Plantilla ROAS", definition = DashboardDefinition(sourceSlot: "primary") });
        Assert.Equal(HttpStatusCode.Created, templateResponse.StatusCode);
        var template = (await templateResponse.Content.ReadFromJsonAsync<DashboardTemplateModel>())!;
        var fromTemplateResponse = await owner.PostAsJsonAsync(
            $"/api/v1/clients/{sourceClient.ClientId}/dashboards", new
            {
                title = "Desde plantilla",
                templateId = template.Id,
                sourceBindings = new Dictionary<string, Guid> { ["primary"] = sourceClient.AccountId }
            });
        Assert.Equal(HttpStatusCode.Created, fromTemplateResponse.StatusCode);
        var fromTemplate = (await fromTemplateResponse.Content.ReadFromJsonAsync<DashboardModel>())!;
        var templateDraft = await owner.GetFromJsonAsync<DashboardDraftModel>(
            $"/api/v1/dashboards/{fromTemplate.Id}/draft");
        Assert.Equal(sourceClient.AccountId,
            FirstDashboardSource(templateDraft!.Definition).GetProperty("dataSourceId").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await owner.PostAsJsonAsync($"/api/v1/clients/{sourceClient.ClientId}/dashboards", new
            {
                title = "Foreign binding",
                templateId = template.Id,
                sourceBindings = new Dictionary<string, Guid> { ["primary"] = destinationClient.AccountId }
            })).StatusCode);

        using (var viewer = app.As(await app.UserWithRole(AgencyRole.Viewer)))
        {
            Assert.Equal(HttpStatusCode.OK,
                (await viewer.GetAsync($"/api/v1/dashboards/{dashboard.Id}/published")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await viewer.GetAsync($"/api/v1/dashboards/{dashboard.Id}/draft")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await viewer.PostAsJsonAsync($"/api/v1/clients/{sourceClient.ClientId}/dashboards",
                    new { title = "Forbidden" })).StatusCode);
        }

        var external = await app.Accept(await app.Invite(sourceClient.ClientId));
        using (var clientViewer = app.As(external))
            Assert.Equal(HttpStatusCode.Forbidden,
                (await clientViewer.GetAsync($"/api/v1/dashboards/{dashboard.Id}/published")).StatusCode);

        dashboard = (await owner.GetFromJsonAsync<DashboardModel>($"/api/v1/dashboards/{dashboard.Id}"))!;
        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.DeleteAsync($"/api/v1/dashboards/{dashboard.Id}?expectedVersion={dashboard.Version}"))
            .StatusCode);
        var archived = (await owner.GetFromJsonAsync<DashboardListItemModel[]>(
            $"/api/v1/clients/{sourceClient.ClientId}/dashboards?includeArchived=true"))!
            .Single(item => item.Id == dashboard.Id);
        Assert.True(archived.IsArchived);
        var restoreResponse = await owner.PostAsJsonAsync($"/api/v1/dashboards/{dashboard.Id}/restore",
            new { expectedVersion = archived.Version });
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.False((await restoreResponse.Content.ReadFromJsonAsync<DashboardModel>())!.IsArchived);
    }

    [Fact]
    public async Task Only_administrators_can_manage_analyst_assignments()
    {
        await using var app = await TestApp.Create();
        var client = await app.Seed(app.Owner.AgencyId, "Client", "9301");
        var analyst = await app.UserWithRole(AgencyRole.Analyst);
        var viewer = await app.UserWithRole(AgencyRole.Viewer);
        using var owner = app.As(app.Owner);
        using var editor = app.As(analyst);

        Assert.Equal(HttpStatusCode.Forbidden, (await editor.GetAsync("/api/v1/agency/editors")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await editor.PostAsJsonAsync($"/api/v1/clients/{client.ClientId}/editors",
                new { userId = analyst.UserId })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await owner.PostAsJsonAsync($"/api/v1/clients/{client.ClientId}/editors",
                new { userId = viewer.UserId })).StatusCode);

        var foreignOwner = await app.Register("foreign-editor@example.test");
        Assert.Equal(HttpStatusCode.NotFound,
            (await owner.PostAsJsonAsync($"/api/v1/clients/{client.ClientId}/editors",
                new { userId = foreignOwner.UserId })).StatusCode);
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static JsonElement DashboardDefinition(Guid? dataSourceId = null, string? sourceSlot = null,
        string metric = "spend")
    {
        var sources = new List<object>();
        if (dataSourceId is not null) sources.Add(new { dataSourceId });
        if (sourceSlot is not null) sources.Add(new { sourceSlot });
        return JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1,
            pages = new[]
            {
                new
                {
                    id = Guid.NewGuid(), name = "Resumen", order = 0, width = 1440, height = 900,
                    components = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(), type = "kpiCard", x = 20, y = 20, width = 320, height = 180,
                            zIndex = 0, isLocked = false,
                            data = new { sources, configuration = new { metric } }, style = new { }
                        }
                    }
                }
            },
            theme = new { }
        });
    }

    private static JsonElement FirstDashboardComponent(JsonElement definition) =>
        definition.GetProperty("pages")[0].GetProperty("components")[0];

    private static JsonElement FirstDashboardSource(JsonElement definition) =>
        FirstDashboardComponent(definition).GetProperty("data").GetProperty("sources")[0];

    private sealed record Seeded(Guid ClientId, Guid AccountId, Guid CampaignId, Guid AdSetId, Guid AdId)
    {
        public static DateOnly Day => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        public static string Range => $"?since={Day:yyyy-MM-dd}&until={Day:yyyy-MM-dd}";
        public IEnumerable<string> ReadRoutes() => new[]
        {
            $"/api/v1/clients/{ClientId}", $"/api/v1/clients/{ClientId}/ad-accounts", $"/api/v1/ad-accounts/{AccountId}",
            $"/api/v1/ad-accounts/{AccountId}/sync", $"/api/v1/ad-accounts/{AccountId}/campaigns?includeMissing=true",
            $"/api/v1/campaigns/{CampaignId}", $"/api/v1/campaigns/{CampaignId}/ad-sets", $"/api/v1/ad-sets/{AdSetId}",
            $"/api/v1/ad-sets/{AdSetId}/ads", $"/api/v1/ads/{AdId}", $"/api/v1/ad-accounts/{AccountId}/metrics{Range}",
            $"/api/v1/campaigns/{CampaignId}/metrics{Range}", $"/api/v1/ad-sets/{AdSetId}/metrics{Range}", $"/api/v1/ads/{AdId}/metrics{Range}",
            $"/api/v1/ad-accounts/{AccountId}/metrics/summary{Range}", $"/api/v1/campaigns/{CampaignId}/metrics/summary{Range}",
            $"/api/v1/ad-sets/{AdSetId}/metrics/summary{Range}", $"/api/v1/ads/{AdId}/metrics/summary{Range}",
            $"/api/v1/ad-accounts/{AccountId}/campaigns/selector{Range}"
            ,$"/api/v1/ad-accounts/{AccountId}/metrics/comparison{Range}&comparison=PreviousPeriod"
            ,$"/api/v1/ad-accounts/{AccountId}/campaigns/benchmarks{Range}"
        };
    }

    private sealed class TestApp : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public MutableClock Clock { get; } = new();
        public HttpClient Http { get; private set; } = null!;
        public AuthResponse Owner { get; private set; } = null!;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(x => x.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<DbContextOptions<AnalitiAdsDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AnalitiAdsDbContext>>();
                services.RemoveAll<AnalitiAdsDbContext>();
                services.AddDbContext<AnalitiAdsDbContext>(x => x.UseSqlite(connection));
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
                services.RemoveAll<IMetaInsightsClient>();
                services.AddSingleton<TestMetaInsightsClient>();
                services.AddScoped<IMetaInsightsClient>(provider => provider.GetRequiredService<TestMetaInsightsClient>());
            });
        }
        public static async Task<TestApp> Create()
        {
            var app = new TestApp();
            app.Http = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>().Database.EnsureCreatedAsync();
            app.Owner = await app.Register("owner@example.test");
            return app;
        }
        public HttpClient As(AuthResponse auth)
        {
            var client = CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            return client;
        }
        public TestMetaInsightsClient Meta => Services.GetRequiredService<TestMetaInsightsClient>();
        public async Task<AuthResponse> Register(string email)
        {
            var response = await Http.PostAsJsonAsync("/api/v1/auth/register", new { agencyName = "Agency", email, password = Password });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        }
        public async Task<CreatedClientInvitationModel> Invite(Guid clientId, string email = Email)
        {
            using var owner = As(Owner);
            var response = await owner.PostAsJsonAsync($"/api/v1/clients/{clientId}/invitations", new { email });
            Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            Assert.True(response.Headers.CacheControl?.NoStore);
            return (await response.Content.ReadFromJsonAsync<CreatedClientInvitationModel>())!;
        }
        public Task<HttpResponseMessage> AcceptRaw(CreatedClientInvitationModel invitation, string email = Email, string password = Password) =>
            Http.PostAsJsonAsync(AcceptRoute, new { invitationToken = invitation.InvitationToken, email, password });
        public async Task<AuthResponse> Accept(CreatedClientInvitationModel invitation)
        {
            var response = await AcceptRaw(invitation);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        }
        public async Task<AuthResponse> UserWithRole(AgencyRole role)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
            var user = User.Create($"{role}@example.test", Clock.Now);
            user.SetPasswordHash(scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, Password));
            var member = Membership.Create(Owner.AgencyId, user.Id, role, Clock.Now);
            db.Users.Add(user); db.Memberships.Add(member);
            await db.SaveChangesAsync();
            var agency = await db.Agencies.SingleAsync(x => x.Id == Owner.AgencyId);
            var token = scope.ServiceProvider.GetRequiredService<ITokenIssuer>().Issue(user, agency, member);
            return new(token.Token, token.ExpiresAtUtc, user.Id, user.Email, agency.Id, agency.Name, role.ToString());
        }
        public async Task EnableMetricsSync(Guid accountId)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
            var account = await db.AdAccounts.SingleAsync(x => x.Id == accountId);
            account.MarkConnected(Clock.Now);
            var protector = scope.ServiceProvider.GetRequiredService<IMetaTokenProtector>();
            db.MetaConnections.Add(MetaConnection.Create(Owner.AgencyId, protector.Protect("test-meta-token"), Clock.Now.AddDays(1), Clock.Now));
            await db.SaveChangesAsync();
        }
        public async Task<Seeded> Seed(Guid agencyId, string name, string metaId)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalitiAdsDbContext>();
            var client = Client.Create(agencyId, name, Clock.Now);
            var account = AdAccount.Create(agencyId, client.Id, MetaAdAccountId.Parse(metaId), name, CurrencyCode.Parse("USD"), MetaTimeZoneId.Parse("America/La_Paz"), Clock.Now);
            var campaign = Campaign.Create(account.Id, new(metaId + "1", name, "SALES", "ACTIVE", "ACTIVE", null, null, null, null), Clock.Now);
            var set = AdSet.Create(campaign.Id, new(metaId + "2", campaign.MetaCampaignId, name, null, null, "ACTIVE", "ACTIVE", null, null, null, null), Clock.Now);
            var ad = Ad.Create(set.Id, new(metaId + "3", set.MetaAdSetId, name, "ACTIVE", "ACTIVE", null, null), Clock.Now);
            db.AddRange(client, account, campaign, set, ad);
            foreach (var level in Enum.GetValues<InsightLevel>())
            {
                var c = level >= InsightLevel.Campaign ? campaign.Id : (Guid?)null;
                var s = level >= InsightLevel.AdSet ? set.Id : (Guid?)null;
                var a = level == InsightLevel.Ad ? ad.Id : (Guid?)null;
                db.InsightSnapshots.Add(InsightSnapshot.Create(account.Id, c, s, a, level,
                    new(level, Seeded.Day, campaign.MetaCampaignId, set.MetaAdSetId, ad.MetaAdId, 12.50m, 100, 50, 5, 2, 1, 20), "USD", Clock.Now));
            }
            await db.SaveChangesAsync();
            return new(client.Id, account.Id, campaign.Id, set.Id, ad.Id);
        }
        public static IReadOnlyList<RemoteInsight> Insights(string metaPrefix, DateOnly day, decimal spend) =>
        [
            new(InsightLevel.Account, day, null, null, null, spend, 100, 50, 5, 2m, 1m, spend * 2m),
            new(InsightLevel.Campaign, day, metaPrefix + "1", null, null, spend, 100, 50, 5, 2m, 1m, spend * 2m),
            new(InsightLevel.AdSet, day, metaPrefix + "1", metaPrefix + "2", null, spend, 100, 50, 5, 2m, 1m, spend * 2m),
            new(InsightLevel.Ad, day, metaPrefix + "1", metaPrefix + "2", metaPrefix + "3", spend, 100, 50, 5, 2m, 1m, spend * 2m)
        ];
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { Http?.Dispose(); connection.Dispose(); }
        }
    }

    private sealed class TestMetaInsightsClient : IMetaInsightsClient
    {
        public IReadOnlyList<RemoteInsight> Values { get; set; } = [];
        public Task<MetaInsights> GetDailyAsync(string metaAccountId, string accessToken, DateOnly since, DateOnly until, CancellationToken cancellationToken = default) => Task.FromResult(new MetaInsights(Values));
    }
}
