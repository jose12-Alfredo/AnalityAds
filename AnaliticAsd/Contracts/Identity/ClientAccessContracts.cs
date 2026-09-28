using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnaliticAsd.Contracts.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateClientInvitationRequest([Required, EmailAddress, StringLength(320)] string Email);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AcceptClientInvitationRequest(
    [Required, StringLength(43, MinimumLength = 43)] string InvitationToken,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);
