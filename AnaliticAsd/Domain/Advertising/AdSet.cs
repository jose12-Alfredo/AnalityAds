namespace AnaliticAsd.Domain.Advertising;

public sealed class AdSet
{
    private AdSet() { }
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public string MetaAdSetId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string OptimizationGoal { get; private set; } = string.Empty;
    public string BillingEvent { get; private set; } = string.Empty;
    public string ConfiguredStatus { get; private set; } = string.Empty;
    public string EffectiveStatus { get; private set; } = string.Empty;
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public DateTimeOffset? MetaCreatedAtUtc { get; private set; }
    public DateTimeOffset? MetaUpdatedAtUtc { get; private set; }
    public DateTimeOffset LastSyncedAtUtc { get; private set; }
    public bool IsPresentOnMeta { get; private set; }
    public static AdSet Create(Guid campaignId, RemoteAdSet value, DateTimeOffset now) { var entity = new AdSet { Id = Guid.NewGuid(), CampaignId = campaignId, MetaAdSetId = Required(value.MetaId, "Meta ad set ID") }; entity.Update(value, now); return entity; }
    public void Update(RemoteAdSet value, DateTimeOffset now)
    {
        if (Required(value.MetaId, "Meta ad set ID") != MetaAdSetId) throw new ArgumentException("Meta ad set ID cannot change.");
        Name = Required(value.Name, "Ad set name"); OptimizationGoal = value.OptimizationGoal?.Trim() ?? string.Empty; BillingEvent = value.BillingEvent?.Trim() ?? string.Empty;
        ConfiguredStatus = value.ConfiguredStatus?.Trim() ?? string.Empty; EffectiveStatus = value.EffectiveStatus?.Trim() ?? string.Empty;
        StartsAtUtc = value.StartsAtUtc?.ToUniversalTime(); EndsAtUtc = value.EndsAtUtc?.ToUniversalTime(); MetaCreatedAtUtc = value.CreatedAtUtc?.ToUniversalTime(); MetaUpdatedAtUtc = value.UpdatedAtUtc?.ToUniversalTime();
        LastSyncedAtUtc = now.ToUniversalTime(); IsPresentOnMeta = true;
    }
    public void MarkMissing() => IsPresentOnMeta = false;
    private static string Required(string value, string label) { ArgumentException.ThrowIfNullOrWhiteSpace(value); var result = value.Trim(); if (result.Length > 200) throw new ArgumentException($"{label} cannot exceed 200 characters."); return result; }
}

public sealed record RemoteAdSet(string MetaId, string MetaCampaignId, string Name, string? OptimizationGoal, string? BillingEvent, string? ConfiguredStatus, string? EffectiveStatus, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, DateTimeOffset? CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
