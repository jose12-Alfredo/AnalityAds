namespace AnaliticAsd.Contracts.Identity;

public sealed record RegisterRequest(string AgencyName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password, Guid? AgencyId = null);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email, Guid AgencyId, string AgencyName, string Role);
public sealed record MeResponse(Guid UserId, string Email, Guid AgencyId, string AgencyName, string Role);
