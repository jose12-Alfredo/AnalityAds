using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Meta;
using AnaliticAsd.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.Persistence;

public sealed class AnalitiAdsDbContext(DbContextOptions<AnalitiAdsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<AdAccount> AdAccounts => Set<AdAccount>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<ProviderConnection> ProviderConnections => Set<ProviderConnection>();
    public DbSet<ProviderMetricSnapshot> ProviderMetricSnapshots => Set<ProviderMetricSnapshot>();
    public DbSet<ProviderSyncSchedule> ProviderSyncSchedules => Set<ProviderSyncSchedule>();
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();
    public DbSet<DashboardDraft> DashboardDrafts => Set<DashboardDraft>();
    public DbSet<DashboardVersion> DashboardVersions => Set<DashboardVersion>();
    public DbSet<DashboardTemplate> DashboardTemplates => Set<DashboardTemplate>();
    public DbSet<DashboardShareLink> DashboardShareLinks => Set<DashboardShareLink>();
    public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();
    public DbSet<DashboardExport> DashboardExports => Set<DashboardExport>();
    public DbSet<DashboardDeliverySchedule> DashboardDeliverySchedules => Set<DashboardDeliverySchedule>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<ClientAccess> ClientAccesses => Set<ClientAccess>();
    public DbSet<ClientEditorAssignment> ClientEditorAssignments => Set<ClientEditorAssignment>();
    public DbSet<ClientInvitation> ClientInvitations => Set<ClientInvitation>();
    public DbSet<MetaConnection> MetaConnections => Set<MetaConnection>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<AdSet> AdSets => Set<AdSet>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<AdAccountSync> AdAccountSyncs => Set<AdAccountSync>();
    public DbSet<InsightSnapshot> InsightSnapshots => Set<InsightSnapshot>();
    public DbSet<McpConnection> McpConnections => Set<McpConnection>();
    public DbSet<McpAuditEvent> McpAuditEvents => Set<McpAuditEvent>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportShareLink> ReportShareLinks => Set<ReportShareLink>();
    public DbSet<ReportShareAuditEvent> ReportShareAuditEvents => Set<ReportShareAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnalitiAdsDbContext).Assembly);
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }
}
