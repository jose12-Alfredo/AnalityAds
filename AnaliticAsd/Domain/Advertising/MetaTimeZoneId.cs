namespace AnaliticAsd.Domain.Advertising;

public sealed record MetaTimeZoneId
{
    public const int MaxLength = 100;

    private MetaTimeZoneId(string value) => Value = value;

    public string Value { get; }

    public static MetaTimeZoneId Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Time zone ID cannot exceed {MaxLength} characters.",
                nameof(value));
        }

        return new MetaTimeZoneId(normalized);
    }

    public override string ToString() => Value;
}
