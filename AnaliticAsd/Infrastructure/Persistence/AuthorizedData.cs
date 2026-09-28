using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.Clients;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Folders;
using AnaliticAsd.Domain.Identity;

namespace AnaliticAsd.Infrastructure.Persistence;

// Shared query roots for REST and future authenticated adapters. No grant IDs are cached in JWTs.
internal sealed class AuthorizedData(AnalitiAdsDbContext db, ICurrentTenant tenant)
{
    public IQueryable<Client> Clients
    {
        get
        {
            var clients = db.Clients.Where(x => x.AgencyId == tenant.AgencyId);
            return tenant.Role switch
            {
                nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin) or nameof(AgencyRole.Viewer) => clients,
                nameof(AgencyRole.Analyst) => clients.Where(client => db.ClientEditorAssignments.Any(assignment =>
                    assignment.AgencyId == tenant.AgencyId && assignment.ClientId == client.Id
                    && assignment.UserId == tenant.UserId)),
                nameof(AgencyRole.ClientViewer) => clients.Where(x => x.IsActive && db.ClientAccesses.Any(g =>
                    g.AgencyId == tenant.AgencyId && g.ClientId == x.Id && g.UserId == tenant.UserId)),
                _ => clients.Where(_ => false)
            };
        }
    }
    public IQueryable<Folder> Folders
    {
        get { var clients = Clients; return db.Folders.Where(folder => clients.Any(client => client.Id == folder.ClientId)); }
    }
    public IQueryable<Dashboard> Dashboards
    {
        get { var clients = Clients; return db.Dashboards.Where(dashboard => clients.Any(client => client.Id == dashboard.ClientId)); }
    }
    public IQueryable<DataSource> DataSources
    {
        get { var clients = Clients; return db.DataSources.Where(source => clients.Any(client => client.Id == source.ClientId)); }
    }
    public IQueryable<AdAccount> Accounts
    {
        get { var clients = Clients; return db.AdAccounts.Where(x => clients.Any(c => c.Id == x.ClientId)); }
    }
    public IQueryable<Campaign> Campaigns
    {
        get { var accounts = Accounts; return db.Campaigns.Where(x => accounts.Any(a => a.Id == x.AdAccountId)); }
    }
    public IQueryable<AdSet> AdSets
    {
        get { var campaigns = Campaigns; return db.AdSets.Where(x => campaigns.Any(c => c.Id == x.CampaignId)); }
    }
    public IQueryable<Ad> Ads
    {
        get { var sets = AdSets; return db.Ads.Where(x => sets.Any(s => s.Id == x.AdSetId)); }
    }
}
