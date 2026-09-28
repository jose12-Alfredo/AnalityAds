using AnaliticAsd.Domain.Dashboards;

namespace AnaliticAsd.Tests.Dashboards;

public sealed class DashboardExportTests
{
    private static readonly DateTimeOffset Now = new(2026,9,23,10,0,0,TimeSpan.Zero);

    [Fact]
    public void Export_preserves_publication_and_has_terminal_content()
    {
        var dashboard=Dashboard.Create(Guid.NewGuid(),Guid.NewGuid(),null,"Ventas",null,Guid.NewGuid(),Now);
        dashboard.RegisterPublication(Now);
        var export=DashboardExport.Create(dashboard,1,DashboardExportFormat.Pdf,Now,userId:Guid.NewGuid());
        export.Start(Now.AddSeconds(1)); export.Complete("ventas.pdf","application/pdf",[1,2,3],Now.AddSeconds(2));
        Assert.Equal(DashboardExportStatus.Completed,export.Status); Assert.Equal(1,export.PublicationNumber);
        Assert.Equal("ventas.pdf",export.FileName); Assert.Equal(3,export.Content!.Length);
    }

    [Fact]
    public void Weekly_schedule_calculates_the_next_requested_weekday()
    {
        var dashboard=Dashboard.Create(Guid.NewGuid(),Guid.NewGuid(),null,"Ventas",null,Guid.NewGuid(),Now);
        var schedule=DashboardDeliverySchedule.Create(dashboard,Guid.NewGuid(),"[\"a@example.com\"]",
            DashboardExportFormat.Pdf,DashboardDeliveryFrequency.Weekly,12,1,null,true,Now);
        Assert.Equal(DayOfWeek.Monday,schedule.NextRunAtUtc.DayOfWeek); Assert.Equal(12,schedule.NextRunAtUtc.Hour);
        Assert.True(schedule.NextRunAtUtc>Now);
    }

    [Fact]
    public void Monthly_schedule_rejects_days_that_do_not_exist_every_month()
    {
        var dashboard=Dashboard.Create(Guid.NewGuid(),Guid.NewGuid(),null,"Ventas",null,Guid.NewGuid(),Now);
        Assert.Throws<ArgumentException>(()=>DashboardDeliverySchedule.Create(dashboard,Guid.NewGuid(),"[]",
            DashboardExportFormat.Csv,DashboardDeliveryFrequency.Monthly,12,null,31,true,Now));
    }
}
