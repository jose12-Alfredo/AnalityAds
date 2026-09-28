using AnaliticAsd.Infrastructure.Persistence;
using Npgsql;

namespace AnaliticAsd.Tests.Infrastructure;

public sealed class PostgresConnectionStringNormalizerTests
{
    [Fact]
    public void Neon_uri_preserves_encoded_credentials_and_security_options()
    {
        const string uri = "postgresql://neondb_owner:pass%3Aword%40example@ep-blue-cloud-123.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require&application_name=AnalitiAds";

        var result = new NpgsqlConnectionStringBuilder(PostgresConnectionStringNormalizer.Normalize(uri));

        Assert.Equal("ep-blue-cloud-123.us-east-2.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neondb_owner", result.Username);
        Assert.Equal("pass:word@example", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
        Assert.Equal("AnalitiAds", result.ApplicationName);
    }

    [Fact]
    public void Database_url_prefix_and_regular_npgsql_connection_string_are_supported()
    {
        var fromEnvironmentSyntax = new NpgsqlConnectionStringBuilder(PostgresConnectionStringNormalizer.Normalize(
            "DATABASE_URL=postgres://user:password@example.neon.tech/db?sslmode=require"));
        const string standard = "Host=localhost;Port=5432;Database=analitiads;Username=analitiads;Password=secret";

        Assert.Equal("example.neon.tech", fromEnvironmentSyntax.Host);
        Assert.Equal(SslMode.Require, fromEnvironmentSyntax.SslMode);
        Assert.Equal(standard, PostgresConnectionStringNormalizer.Normalize(standard));
    }

    [Theory]
    [InlineData("postgresql://user@example.neon.tech/db")]
    [InlineData("postgresql://user:password@example.neon.tech/")]
    public void Incomplete_postgresql_uri_is_rejected_without_echoing_it(string uri) =>
        Assert.Throws<ArgumentException>(() => PostgresConnectionStringNormalizer.Normalize(uri));
}
