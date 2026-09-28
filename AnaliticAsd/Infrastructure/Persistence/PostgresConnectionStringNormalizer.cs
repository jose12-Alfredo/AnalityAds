using Npgsql;

namespace AnaliticAsd.Infrastructure.Persistence;

// Neon commonly displays PostgreSQL URIs, while Npgsql's EF configuration uses key/value strings.
internal static class PostgresConnectionStringNormalizer
{
    public static string Normalize(string configuredConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredConnectionString);
        var value = configuredConnectionString.Trim().Trim('"');
        if (value.StartsWith("DATABASE_URL=", StringComparison.OrdinalIgnoreCase))
            value = value["DATABASE_URL=".Length..].Trim().Trim('"');

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme is not "postgres" and not "postgresql"))
            return value;

        if (string.IsNullOrWhiteSpace(uri.Host) || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
            throw new ArgumentException("PostgreSQL URI must include a host and database name.", nameof(configuredConnectionString));

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2 || string.IsNullOrWhiteSpace(userInfo[0]) || string.IsNullOrWhiteSpace(userInfo[1]))
            throw new ArgumentException("PostgreSQL URI must include a username and password.", nameof(configuredConnectionString));

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        };

        foreach (var segment in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            var key = Uri.UnescapeDataString(separator < 0 ? segment : segment[..separator]);
            var option = Uri.UnescapeDataString(separator < 0 ? string.Empty : segment[(separator + 1)..]);
            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = ParseSslMode(option);
                    break;
                case "channel_binding":
                case "channelbinding":
                    builder.ChannelBinding = ParseChannelBinding(option);
                    break;
                case "application_name":
                case "applicationname":
                    builder.ApplicationName = option;
                    break;
                case "connect_timeout" or "timeout" when int.TryParse(option, out var timeout) && timeout >= 0:
                    builder.Timeout = timeout;
                    break;
                case "command_timeout" when int.TryParse(option, out var commandTimeout) && commandTimeout >= 0:
                    builder.CommandTimeout = commandTimeout;
                    break;
                case "options":
                    builder.Options = option;
                    break;
                    // Neon supplies the endpoint in the hostname; unknown URI options are intentionally not copied.
            }
        }

        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string value) => NormalizeEnum<SslMode>(value, "sslmode");
    private static ChannelBinding ParseChannelBinding(string value) => NormalizeEnum<ChannelBinding>(value, "channel_binding");

    private static T NormalizeEnum<T>(string value, string option) where T : struct, Enum
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<T>(normalized, true, out var result)) return result;
        throw new ArgumentException($"Unsupported PostgreSQL URI option '{option}'.");
    }
}
