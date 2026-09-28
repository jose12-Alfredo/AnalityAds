namespace AnaliticAsd.Domain.Identity;

public sealed class McpConnection
{
    private McpConnection() { }

    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Scopes { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static McpConnection Create(Guid agencyId, Guid userId, string name, string scopes, DateTimeOffset now, DateTimeOffset expires)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("Connection name is required and cannot exceed 120 characters.");
        return new McpConnection { Id = Guid.NewGuid(), AgencyId = agencyId, UserId = userId, Name = name.Trim(), Scopes = scopes, CreatedAtUtc = now, ExpiresAtUtc = expires };
    }

    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;
}

public sealed class McpAuditEvent
{
    private McpAuditEvent() { }
    public Guid Id { get; private set; }
    public Guid ConnectionId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public bool Succeeded { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static McpAuditEvent Create(Guid connectionId, Guid agencyId, Guid userId, string operation, bool succeeded, DateTimeOffset now) =>
        new() { Id = Guid.NewGuid(), ConnectionId = connectionId, AgencyId = agencyId, UserId = userId, Operation = operation[..Math.Min(operation.Length, 100)], Succeeded = succeeded, OccurredAtUtc = now };
}
