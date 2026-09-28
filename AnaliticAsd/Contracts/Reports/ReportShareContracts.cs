using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnaliticAsd.Contracts.Reports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReportShareLinkRequest([Range(1, 30)] int ExpirationDays);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AccessSharedReportRequest([Required, StringLength(43, MinimumLength = 43)] string AccessToken);
