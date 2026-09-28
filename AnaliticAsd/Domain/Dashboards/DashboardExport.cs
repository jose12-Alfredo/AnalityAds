namespace AnaliticAsd.Domain.Dashboards;

public enum DashboardExportFormat { Pdf, Csv, Excel }
public enum DashboardExportStatus { Pending, Processing, Completed, Failed }
public enum DashboardDeliveryFrequency { Daily, Weekly, Monthly }

public sealed class DashboardExport
{
    private DashboardExport() { }
    public Guid Id { get; private set; }
    public Guid DashboardId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public Guid? ShareLinkId { get; private set; }
    public Guid? DeliveryScheduleId { get; private set; }
    public int PublicationNumber { get; private set; }
    public DashboardExportFormat Format { get; private set; }
    public DashboardExportStatus Status { get; private set; }
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }
    public byte[]? Content { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static DashboardExport Create(Dashboard dashboard, int publication, DashboardExportFormat format,
        DateTimeOffset now, Guid? userId = null, Guid? shareLinkId = null, Guid? scheduleId = null)
    {
        if (publication <= 0 || userId is null && shareLinkId is null && scheduleId is null)
            throw new ArgumentException("A publication and export principal are required.");
        return new() { Id = Guid.NewGuid(), DashboardId = dashboard.Id, AgencyId = dashboard.AgencyId,
            ClientId = dashboard.ClientId, RequestedByUserId = userId, ShareLinkId = shareLinkId,
            DeliveryScheduleId = scheduleId, PublicationNumber = publication, Format = format,
            Status = DashboardExportStatus.Pending, CreatedAtUtc = now.ToUniversalTime() };
    }
    public void Start(DateTimeOffset now) { if (Status != DashboardExportStatus.Pending) return; Status = DashboardExportStatus.Processing; StartedAtUtc = now.ToUniversalTime(); }
    public void Complete(string fileName, string contentType, byte[] content, DateTimeOffset now)
    {
        if (content.Length == 0) throw new ArgumentException("Export content cannot be empty.");
        FileName = fileName; ContentType = contentType; Content = content; ErrorCode = null;
        Status = DashboardExportStatus.Completed; CompletedAtUtc = now.ToUniversalTime();
    }
    public void Fail(string code, DateTimeOffset now) { ErrorCode = code[..Math.Min(code.Length, 120)]; Status = DashboardExportStatus.Failed; CompletedAtUtc = now.ToUniversalTime(); Content = null; }
}

public sealed class DashboardDeliverySchedule
{
    private DashboardDeliverySchedule() { }
    public Guid Id { get; private set; }
    public Guid DashboardId { get; private set; }
    public Guid AgencyId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string RecipientsJson { get; private set; } = "[]";
    public DashboardExportFormat Format { get; private set; }
    public DashboardDeliveryFrequency Frequency { get; private set; }
    public int HourUtc { get; private set; }
    public int? DayOfWeek { get; private set; }
    public int? DayOfMonth { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset NextRunAtUtc { get; private set; }
    public DateTimeOffset? LastRunAtUtc { get; private set; }
    public DateTimeOffset? LastSucceededAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }
    public Guid Version { get; private set; }

    public static DashboardDeliverySchedule Create(Dashboard dashboard, Guid userId, string recipientsJson,
        DashboardExportFormat format, DashboardDeliveryFrequency frequency, int hourUtc, int? dayOfWeek,
        int? dayOfMonth, bool enabled, DateTimeOffset now)
    {
        var value = new DashboardDeliverySchedule { Id=Guid.NewGuid(),DashboardId=dashboard.Id,AgencyId=dashboard.AgencyId,
            ClientId=dashboard.ClientId,CreatedByUserId=userId };
        value.Configure(recipientsJson,format,frequency,hourUtc,dayOfWeek,dayOfMonth,enabled,now); return value;
    }
    public void Configure(string recipientsJson, DashboardExportFormat format, DashboardDeliveryFrequency frequency,
        int hourUtc, int? dayOfWeek, int? dayOfMonth, bool enabled, DateTimeOffset now)
    {
        if (hourUtc is < 0 or > 23) throw new ArgumentOutOfRangeException(nameof(hourUtc));
        if (frequency == DashboardDeliveryFrequency.Weekly && dayOfWeek is < 0 or > 6) throw new ArgumentException("Weekly schedules require dayOfWeek 0..6.");
        if (frequency == DashboardDeliveryFrequency.Monthly && dayOfMonth is < 1 or > 28) throw new ArgumentException("Monthly schedules require dayOfMonth 1..28.");
        RecipientsJson=recipientsJson;Format=format;Frequency=frequency;HourUtc=hourUtc;DayOfWeek=dayOfWeek;DayOfMonth=dayOfMonth;IsEnabled=enabled;
        NextRunAtUtc=Next(now);Version=Guid.NewGuid();
    }
    public void Complete(DateTimeOffset now) { LastRunAtUtc=LastSucceededAtUtc=now.ToUniversalTime();LastErrorCode=null;NextRunAtUtc=Next(now);Version=Guid.NewGuid(); }
    public void Fail(string code, DateTimeOffset now) { LastRunAtUtc=now.ToUniversalTime();LastErrorCode=code[..Math.Min(code.Length,120)];NextRunAtUtc=Next(now);Version=Guid.NewGuid(); }
    private DateTimeOffset Next(DateTimeOffset from)
    {
        var start=new DateTimeOffset(from.UtcDateTime.Date.AddHours(HourUtc),TimeSpan.Zero);
        if(start<=from)start=start.AddDays(1);
        if(Frequency==DashboardDeliveryFrequency.Weekly)while((int)start.DayOfWeek!=DayOfWeek)start=start.AddDays(1);
        if(Frequency==DashboardDeliveryFrequency.Monthly){var candidate=new DateTimeOffset(start.Year,start.Month,DayOfMonth!.Value,HourUtc,0,0,TimeSpan.Zero);if(candidate<=from)candidate=candidate.AddMonths(1);start=candidate;}
        return start;
    }
}
