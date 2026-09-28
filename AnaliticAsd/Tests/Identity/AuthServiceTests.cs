using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.Identity;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Register_hashes_password_and_login_issues_token_with_tenant()
    {
        var repository = new MemoryIdentityRepository();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "a-secure-test-signing-key-with-more-than-32-bytes",
            ["Jwt:Issuer"] = "Tests",
            ["Jwt:Audience"] = "Tests"
        }).Build();
        var service = new AuthService(repository, repository, new PasswordHasher<User>(), new JwtTokenIssuer(configuration, TimeProvider.System), TimeProvider.System);

        var registered = await service.RegisterAsync(new("Agency A", "owner@example.com", "SecurePassword123"));
        var loggedIn = await service.LoginAsync(new("owner@example.com", "SecurePassword123", null));

        Assert.NotEqual("SecurePassword123", repository.User!.PasswordHash);
        Assert.Equal(registered.AgencyId, loggedIn.AgencyId);
        Assert.Equal(AgencyRole.Owner, loggedIn.Role);
        Assert.NotEmpty(loggedIn.AccessToken);
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        var repository = new MemoryIdentityRepository();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "a-secure-test-signing-key-with-more-than-32-bytes" }).Build();
        var service = new AuthService(repository, repository, new PasswordHasher<User>(), new JwtTokenIssuer(configuration, TimeProvider.System), TimeProvider.System);
        await service.RegisterAsync(new("Agency A", "owner@example.com", "SecurePassword123"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new("owner@example.com", "WrongPassword123", null)));
    }

    private sealed class MemoryIdentityRepository : IIdentityRepository, IUnitOfWork
    {
        public Agency? Agency { get; private set; }
        public User? User { get; private set; }
        public Membership? Membership { get; private set; }
        public Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(User?.NormalizedEmail == normalizedEmail ? User : null);
        public Task<IReadOnlyList<(Membership Membership, Agency Agency)>> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<(Membership, Agency)>>(Membership is not null && Agency is not null && Membership.UserId == userId ? [(Membership, Agency)] : []);
        public void Add(Agency agency) => Agency = agency;
        public void Add(User user) => User = user;
        public void Add(Membership membership) => Membership = membership;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
