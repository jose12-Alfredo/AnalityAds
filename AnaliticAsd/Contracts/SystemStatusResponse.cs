namespace AnaliticAsd.Contracts;

public sealed record SystemStatusResponse(
    string Service,
    string Status,
    string Version,
    DateTimeOffset TimestampUtc);
