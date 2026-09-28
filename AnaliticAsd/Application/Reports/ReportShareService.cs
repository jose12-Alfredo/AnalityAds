using System.Security.Cryptography;
using System.Text;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Reports;
using Microsoft.AspNetCore.WebUtilities;

namespace AnaliticAsd.Application.Reports;

public sealed class ReportShareService(IReportShareRepository shares, IReportRepository reports,
    IReportPdfRenderer pdf, ICurrentTenant tenant, TimeProvider clock) : IReportShareService
{
    public async Task<IReadOnlyList<ReportShareLinkModel>> ListAsync(Guid reportId, CancellationToken ct = default)
    {
        RequireManager();
        await RequireReport(reportId, ct);
        return (await shares.ListAsync(tenant.AgencyId, reportId, ct)).Select(Map).ToArray();
    }

    public async Task<CreatedReportShareLinkModel> CreateAsync(Guid reportId, int expirationDays, CancellationToken ct = default)
    {
        RequireManager();
        if (expirationDays is < 1 or > 30) throw new ArgumentException("Expiration days must be between 1 and 30.");
        var report = await RequireReport(reportId, ct);
        var now = clock.GetUtcNow();
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var link = ReportShareLink.Create(report.Id, tenant.AgencyId, report.ClientId, tenant.UserId,
            Hash(token), now, now.AddDays(expirationDays));
        shares.Add(link);
        await shares.SaveAsync(ct);
        return new(Map(link), token, $"/reportes-compartidos#{token}");
    }

    public async Task RevokeAsync(Guid reportId, Guid shareLinkId, CancellationToken ct = default)
    {
        RequireManager();
        await RequireReport(reportId, ct);
        var link = await shares.FindAsync(tenant.AgencyId, reportId, shareLinkId, ct)
            ?? throw new EntityNotFoundException("Report share link was not found.");
        link.Revoke(clock.GetUtcNow());
        await shares.SaveAsync(ct);
    }

    public async Task<SharedReportDataModel> AccessAsync(string token, string operation, CancellationToken ct = default)
    {
        if (operation is not ("View" or "Pdf")) throw new ArgumentException("Invalid shared report operation.");
        if (string.IsNullOrWhiteSpace(token) || token.Length != 43) throw InvalidLink();
        var found = await shares.FindAvailableByHashAsync(Hash(token), clock.GetUtcNow(), ct);
        if (found is null) throw InvalidLink();
        var snapshot = ReportSnapshot.ReadAndVerify(found.Value.Report);
        shares.Add(ReportShareAuditEvent.Create(found.Value.Link.Id, operation, clock.GetUtcNow()));
        await shares.SaveAsync(ct);
        return new(snapshot.Title, snapshot.CreatedAtUtc, snapshot.Analysis);
    }

    public async Task<byte[]> PdfAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 43) throw InvalidLink();
        var found = await shares.FindAvailableByHashAsync(Hash(token), clock.GetUtcNow(), ct);
        if (found is null) throw InvalidLink();
        var snapshot = ReportSnapshot.ReadAndVerify(found.Value.Report);
        shares.Add(ReportShareAuditEvent.Create(found.Value.Link.Id, "Pdf", clock.GetUtcNow()));
        await shares.SaveAsync(ct);
        return pdf.Render(snapshot);
    }

    private async Task<Report> RequireReport(Guid id, CancellationToken ct) =>
        await reports.FindAsync(id, false, ct) ?? throw new EntityNotFoundException("Report was not found.");
    private void RequireManager()
    {
        if (tenant.Role is not (nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))
            throw new ForbiddenException("Only Owner and Admin can manage report share links.");
    }
    private ReportShareLinkModel Map(ReportShareLink x) => new(x.Id, x.ReportId, x.Status(clock.GetUtcNow()), x.CreatedAtUtc, x.ExpiresAtUtc, x.RevokedAtUtc);
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static EntityNotFoundException InvalidLink() => new("The shared report link is invalid, expired or revoked.");
}
