namespace AnaliticAsd.Domain.Advertising;

public sealed class Ad
{
    private Ad() { }
    public Guid Id { get; private set; }
    public Guid AdSetId { get; private set; }
    public string MetaAdId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ConfiguredStatus { get; private set; } = string.Empty;
    public string EffectiveStatus { get; private set; } = string.Empty;
    public DateTimeOffset? MetaCreatedAtUtc { get; private set; }
    public DateTimeOffset? MetaUpdatedAtUtc { get; private set; }
    public DateTimeOffset LastSyncedAtUtc { get; private set; }
    public bool IsPresentOnMeta { get; private set; }
    public static Ad Create(Guid adSetId, RemoteAd value, DateTimeOffset now) { var entity = new Ad { Id = Guid.NewGuid(), AdSetId = adSetId, MetaAdId = Required(value.MetaId, "Meta ad ID") }; entity.Update(value, now); return entity; }
    public void Update(RemoteAd value, DateTimeOffset now)
    {
        if (Required(value.MetaId, "Meta ad ID") != MetaAdId) throw new ArgumentException("Meta ad ID cannot change.");
        Name = Required(value.Name, "Ad name"); ConfiguredStatus = value.ConfiguredStatus?.Trim() ?? string.Empty; EffectiveStatus = value.EffectiveStatus?.Trim() ?? string.Empty;
        MetaCreatedAtUtc = value.CreatedAtUtc?.ToUniversalTime(); MetaUpdatedAtUtc = value.UpdatedAtUtc?.ToUniversalTime(); LastSyncedAtUtc = now.ToUniversalTime(); IsPresentOnMeta = true;
    }
    public void MarkMissing() => IsPresentOnMeta = false;
    private static string Required(string value, string label) { ArgumentException.ThrowIfNullOrWhiteSpace(value); var result = value.Trim(); if (result.Length > 200) throw new ArgumentException($"{label} cannot exceed 200 characters."); return result; }
}

public sealed record RemoteAd(string MetaId, string MetaAdSetId, string Name, string? ConfiguredStatus, string? EffectiveStatus, DateTimeOffset? CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
