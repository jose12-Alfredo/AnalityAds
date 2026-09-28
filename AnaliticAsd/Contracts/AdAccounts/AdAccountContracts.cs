using System.ComponentModel.DataAnnotations;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Contracts.AdAccounts;

public sealed class CreateAdAccountRequest
{
    [Required, StringLength(MetaAdAccountId.MaxLength)]
    public string MetaAccountId { get; init; } = string.Empty;
    [Required, StringLength(AdAccount.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
    [Required, StringLength(CurrencyCode.Length, MinimumLength = CurrencyCode.Length)]
    public string Currency { get; init; } = string.Empty;
    [Required, StringLength(MetaTimeZoneId.MaxLength)]
    public string TimeZone { get; init; } = string.Empty;
}

public sealed class UpdateAdAccountRequest
{
    [Required, StringLength(AdAccount.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
    [Required, StringLength(CurrencyCode.Length, MinimumLength = CurrencyCode.Length)]
    public string Currency { get; init; } = string.Empty;
    [Required, StringLength(MetaTimeZoneId.MaxLength)]
    public string TimeZone { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed record AdAccountResponse(
    Guid Id,
    Guid ClientId,
    string MetaAccountId,
    string Name,
    string Currency,
    string TimeZone,
    string ConnectionStatus,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
