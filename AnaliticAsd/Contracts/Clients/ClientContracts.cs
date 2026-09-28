using System.ComponentModel.DataAnnotations;
using AnaliticAsd.Domain.Clients;

namespace AnaliticAsd.Contracts.Clients;

public sealed class CreateClientRequest
{
    [Required, StringLength(Client.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateClientRequest
{
    [Required, StringLength(Client.MaxNameLength)]
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed record ClientResponse(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
