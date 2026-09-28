namespace AnaliticAsd.Domain.Advertising;

public sealed record CurrencyCode
{
    public const int Length = 3;

    private CurrencyCode(string value) => Value = value;

    public string Value { get; }

    public static CurrencyCode Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != Length || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(value));
        }

        return new CurrencyCode(normalized);
    }

    public override string ToString() => Value;
}
