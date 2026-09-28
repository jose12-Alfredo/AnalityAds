namespace AnaliticAsd.Domain.Advertising;

public sealed record MetaAdAccountId
{
    public const int MaxLength = 64;

    private MetaAdAccountId(string value) => Value = value;

    public string Value { get; }

    public static MetaAdAccountId Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.StartsWith("act_", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[4..];
        }

        if (normalized.Length is 0 or > MaxLength || !normalized.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Meta ad account ID must contain only digits.", nameof(value));
        }

        return new MetaAdAccountId(normalized);
    }

    public override string ToString() => Value;
}
