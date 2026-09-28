namespace AnaliticAsd.Domain.Advertising;

public sealed class Campaign
{
    private Campaign() { }
    public Guid Id { get; private set; }
    public Guid AdAccountId { get; private set; }
    public string MetaCampaignId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string ConfiguredStatus { get; private set; } = string.Empty;
    public string EffectiveStatus { get; private set; } = string.Empty;
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? StopsAtUtc { get; private set; }
    public DateTimeOffset? MetaCreatedAtUtc { get; private set; }
    public DateTimeOffset? MetaUpdatedAtUtc { get; private set; }
    public DateTimeOffset LastSyncedAtUtc { get; private set; }
    public bool IsPresentOnMeta { get; private set; }

    public static Campaign Create(Guid accountId, RemoteCampaign value, DateTimeOffset now)
    {
        var entity = new Campaign { Id = Guid.NewGuid(), AdAccountId = accountId, MetaCampaignId = RequiredId(value.MetaId) };
        entity.Update(value, now);
        return entity;
    }

    public void Update(RemoteCampaign value, DateTimeOffset now)
    {
        if (RequiredId(value.MetaId) != MetaCampaignId) throw new ArgumentException("Meta campaign ID cannot change.");
        Name = Required(value.Name, "Campaign name");
        Objective = value.Objective?.Trim() ?? string.Empty;
        ConfiguredStatus = value.ConfiguredStatus?.Trim() ?? string.Empty;
        EffectiveStatus = value.EffectiveStatus?.Trim() ?? string.Empty;
        StartsAtUtc = value.StartsAtUtc?.ToUniversalTime(); StopsAtUtc = value.StopsAtUtc?.ToUniversalTime();
        MetaCreatedAtUtc = value.CreatedAtUtc?.ToUniversalTime(); MetaUpdatedAtUtc = value.UpdatedAtUtc?.ToUniversalTime();
        LastSyncedAtUtc = now.ToUniversalTime(); IsPresentOnMeta = true;
    }
    public void MarkMissing() => IsPresentOnMeta = false;
    private static string RequiredId(string value) => Required(value, "Meta campaign ID");
    private static string Required(string value, string label) { ArgumentException.ThrowIfNullOrWhiteSpace(value); var result = value.Trim(); if (result.Length > 200) throw new ArgumentException($"{label} cannot exceed 200 characters."); return result; }
}

public sealed record RemoteCampaign(string MetaId, string Name, string? Objective, string? ConfiguredStatus, string? EffectiveStatus, DateTimeOffset? StartsAtUtc, DateTimeOffset? StopsAtUtc, DateTimeOffset? CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
