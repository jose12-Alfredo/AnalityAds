namespace AnaliticAsd.Domain.Advertising;

public enum SyncStatus { NeverSynced = 1, Running = 2, Succeeded = 3, Failed = 4 }

public sealed class AdAccountSync
{
    private AdAccountSync() { }
    private AdAccountSync(Guid adAccountId) { AdAccountId = adAccountId; Status = SyncStatus.NeverSynced; }
    public Guid AdAccountId { get; private set; }
    public SyncStatus Status { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int CampaignsSynced { get; private set; }
    public int AdSetsSynced { get; private set; }
    public int AdsSynced { get; private set; }
    public string? ErrorCode { get; private set; }
    public static AdAccountSync Create(Guid accountId) => new(accountId);
    public void Start(DateTimeOffset now) { Status = SyncStatus.Running; StartedAtUtc = now.ToUniversalTime(); CompletedAtUtc = null; ErrorCode = null; CampaignsSynced = AdSetsSynced = AdsSynced = 0; }
    public void Succeed(int campaigns, int adSets, int ads, DateTimeOffset now) { Status = SyncStatus.Succeeded; CompletedAtUtc = now.ToUniversalTime(); CampaignsSynced = campaigns; AdSetsSynced = adSets; AdsSynced = ads; ErrorCode = null; }
    public void Fail(string errorCode, DateTimeOffset now) { Status = SyncStatus.Failed; CompletedAtUtc = now.ToUniversalTime(); ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "sync_failed" : errorCode; }
}
