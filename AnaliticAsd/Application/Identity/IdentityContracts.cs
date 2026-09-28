using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Application.Identity;

public sealed record RegisterCommand(string AgencyName, string Email, string Password);
public sealed record LoginCommand(string Email, string Password, Guid? AgencyId);
public sealed record AuthModel(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email, Guid AgencyId, string AgencyName, AgencyRole Role);
public sealed record CurrentUserModel(Guid UserId, string Email, Guid AgencyId, string AgencyName, AgencyRole Role);

public interface IIdentityRepository
{
    Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Membership Membership, Agency Agency)>> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Agency agency);
    void Add(User user);
    void Add(Membership membership);
}

public interface ITokenIssuer
{
    (string Token, DateTimeOffset ExpiresAtUtc) Issue(User user, Agency agency, Membership membership);
}

public interface IAuthService
{
    Task<AuthModel> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);
    Task<AuthModel> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
}

public interface ICurrentTenant
{
    Guid AgencyId { get; }
    Guid UserId { get; }
    string Role { get; }
}
