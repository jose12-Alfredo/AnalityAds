using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace AnaliticAsd.Application.Identity;

public sealed class AuthService(IIdentityRepository repository, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, ITokenIssuer tokenIssuer, TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthModel> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        ValidatePassword(command.Password);
        var normalizedEmail = User.NormalizeEmail(command.Email);
        if (await repository.FindUserByEmailAsync(normalizedEmail, cancellationToken) is not null)
            throw new ConflictException("An account with this email already exists.");

        var now = timeProvider.GetUtcNow();
        var agency = Agency.Create(command.AgencyName, now);
        var user = User.Create(command.Email, now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
        var membership = Membership.Create(agency.Id, user.Id, AgencyRole.Owner, now);
        repository.Add(agency);
        repository.Add(user);
        repository.Add(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(user, agency, membership);
    }

    public async Task<AuthModel> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await repository.FindUserByEmailAsync(User.NormalizeEmail(command.Email), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");
        if (!user.IsActive || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var memberships = await repository.ListMembershipsAsync(user.Id, cancellationToken);
        var selected = command.AgencyId is { } agencyId
            ? memberships.SingleOrDefault(item => item.Agency.Id == agencyId)
            : memberships.Count == 1 ? memberships[0] : default;
        if (selected.Agency is null || !selected.Agency.IsActive)
            throw new UnauthorizedAccessException("The requested agency is not available for this user.");
        return Map(user, selected.Agency, selected.Membership);
    }

    private AuthModel Map(User user, Agency agency, Membership membership)
    {
        var token = tokenIssuer.Issue(user, agency, membership);
        return new(token.Token, token.ExpiresAtUtc, user.Id, user.Email, agency.Id, agency.Name, membership.Role);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ArgumentException("Password must contain at least 12 characters, uppercase, lowercase and a number.", nameof(password));
    }
}
