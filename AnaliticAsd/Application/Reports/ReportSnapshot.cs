using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnaliticAsd.Domain.Reports;

namespace AnaliticAsd.Application.Reports;

internal static class ReportSnapshot
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(ReportDataModel value) => JsonSerializer.Serialize(value, JsonOptions);

    public static string HashCanonical(ReportDataModel value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(value with { SnapshotHash = string.Empty }))));

    public static ReportDataModel ReadAndVerify(Report report)
    {
        var snapshot = JsonSerializer.Deserialize<ReportDataModel>(report.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("The report snapshot could not be read.");
        var calculated = Convert.FromHexString(HashCanonical(snapshot));
        var expected = Convert.FromHexString(report.SnapshotHash);
        if (!CryptographicOperations.FixedTimeEquals(calculated, expected)
            || snapshot.ReportId != report.Id || snapshot.ClientId != report.ClientId
            || snapshot.AdAccountId != report.AdAccountId || snapshot.CreatedByUserId != report.CreatedByUserId
            || snapshot.SchemaVersion != report.SchemaVersion || snapshot.SnapshotHash != report.SnapshotHash)
            throw new InvalidOperationException("The report snapshot failed its integrity check.");
        return snapshot;
    }
}
