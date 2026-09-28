using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnaliticAsd.Application.Common;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using AnaliticAsd.Domain.Advertising;

namespace AnaliticAsd.Application.Dashboards;
public sealed record DashboardShareModel(Guid Id,string Status,DateTimeOffset? ExpiresAtUtc,string? RecipientEmail,
    bool RequiresPassword,bool AllowFilters,bool AllowExport,bool AllowEmbed,long AccessCount,DateTimeOffset? LastAccessedAtUtc);
public sealed record CreatedDashboardShareModel(DashboardShareModel ShareLink,string AccessToken,string RelativeUrl);
public sealed record SharedDashboardModel(string Title,string Description,int PublicationNumber,JsonElement Definition,
    DateTimeOffset PublishedAtUtc,bool AllowFilters,bool AllowExport,bool AllowEmbed,SharedBrandModel Branding);
public sealed record SharedBrandModel(string? LogoUrl,string PrimaryColor,string SecondaryColor,string BackgroundColor,string FontFamily);
public sealed record PublicDashboardMetric(decimal? Value,string Availability,string Unit);
public sealed record PublicDashboardRow(string Key,string Label,IReadOnlyDictionary<string,PublicDashboardMetric> Metrics);
public sealed record PublicDashboardQueryModel(string Provider,string? Currency,string TimeZone,IReadOnlyList<PublicDashboardRow> Rows);

public sealed class DashboardShareService(AnalitiAdsDbContext db, ICurrentTenant tenant,
    IPasswordHasher<DashboardShareLink> passwords, TimeProvider clock)
{
    public async Task<IReadOnlyList<DashboardShareModel>> ListAsync(Guid dashboardId,CancellationToken ct)
    { RequireManager(); await RequiredDashboard(dashboardId,ct); return (await db.DashboardShareLinks.AsNoTracking().Where(x=>x.AgencyId==tenant.AgencyId&&x.DashboardId==dashboardId).OrderByDescending(x=>x.CreatedAtUtc).ToArrayAsync(ct)).Select(Map).ToArray(); }
    public async Task<CreatedDashboardShareModel> CreateAsync(Guid dashboardId,int? expirationDays,string? password,string? recipientEmail,bool allowFilters,bool allowExport,bool allowEmbed,CancellationToken ct)
    {
        RequireManager(); var dashboard=await RequiredDashboard(dashboardId,ct); if(dashboard.CurrentPublicationNumber is null) throw new ConflictException("Publish the dashboard before sharing it.");
        if(expirationDays is < 1 or > 365) throw new ArgumentException("Expiration days must be between 1 and 365.");
        if(password is {Length:<8}) throw new ArgumentException("Password must contain at least 8 characters.");
        if(!string.IsNullOrWhiteSpace(recipientEmail) && !recipientEmail.Contains('@')) throw new ArgumentException("Recipient email is invalid.");
        var now=clock.GetUtcNow(); var token=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var link=DashboardShareLink.Create(dashboard.Id,dashboard.AgencyId,dashboard.ClientId,tenant.UserId,Hash(token),null,recipientEmail,
            expirationDays.HasValue?now.AddDays(expirationDays.Value):null,allowFilters,allowExport,allowEmbed,now);
        if(!string.IsNullOrWhiteSpace(password)) link.SetPasswordHash(passwords.HashPassword(link,password));
        db.Add(link); await db.SaveChangesAsync(ct); return new(Map(link),token,$"/dashboards-compartidos#{token}");
    }
    public async Task RevokeAsync(Guid dashboardId,Guid linkId,CancellationToken ct) { RequireManager(); await RequiredDashboard(dashboardId,ct); var link=await db.DashboardShareLinks.SingleOrDefaultAsync(x=>x.Id==linkId&&x.DashboardId==dashboardId&&x.AgencyId==tenant.AgencyId,ct)??throw new EntityNotFoundException("Dashboard share link was not found."); link.Revoke(clock.GetUtcNow()); await db.SaveChangesAsync(ct); }
    public async Task<SharedDashboardModel> AccessAsync(string token,string? password,string? recipientEmail,bool embed,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(token)||token.Length!=43) throw Invalid(); var hash=Hash(token); var now=clock.GetUtcNow();
        var (link,dashboard,version)=await Authorized(token,password,recipientEmail,embed,ct);
        var publication=version.PublicationNumber;
        link.RecordAccess(now); await db.SaveChangesAsync(ct); using var json=JsonDocument.Parse(version.DefinitionJson);
        var clientBrand=await db.BrandProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.AgencyId==link.AgencyId&&x.ClientId==link.ClientId,ct);
        var agencyBrand=await db.BrandProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.AgencyId==link.AgencyId&&x.ClientId==null,ct); var brand=clientBrand??agencyBrand;
        return new(dashboard.Title,dashboard.Description,publication,json.RootElement.Clone(),version.PublishedAtUtc,link.AllowFilters,link.AllowExport,link.AllowEmbed,
            brand is null?new(null,"#8054D8","#3C9CA0","#F5F6F8","Arial"):new(brand.LogoUrl,brand.PrimaryColor,brand.SecondaryColor,brand.BackgroundColor,brand.FontFamily));
    }
    public async Task<PublicDashboardQueryModel> QueryAsync(string token,string? password,string? email,Guid sourceId,DateOnly since,DateOnly until,string dimension,IReadOnlyList<string> metrics,CancellationToken ct)
    {
        var (link,_,version)=await Authorized(token,password,email,false,ct);
        using var definition=JsonDocument.Parse(version.DefinitionJson);
        PublishedDashboardQueryPolicy.Validate(definition.RootElement,sourceId,since,until,dimension,metrics,link.AllowFilters);
        var source=await db.DataSources.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==sourceId&&x.AgencyId==link.AgencyId&&x.ClientId==link.ClientId,ct)??throw Invalid();
        if(since>until||until.DayNumber-since.DayNumber>90)throw new ArgumentException("Invalid public date range.");
        var rows=new List<PublicDashboardRow>();
        if(source.Provider==Domain.DataSources.DataProvider.MetaAds)
        {
            var level=dimension=="campaign"?InsightLevel.Campaign:InsightLevel.Account;var values=await db.InsightSnapshots.AsNoTracking().Where(x=>x.AdAccountId==sourceId&&x.Level==level&&x.SnapshotDate>=since&&x.SnapshotDate<=until).ToArrayAsync(ct);
            var groups=dimension=="date"?values.GroupBy(x=>x.SnapshotDate.ToString("yyyy-MM-dd")):dimension=="campaign"?values.GroupBy(x=>x.CampaignId?.ToString()??""):values.GroupBy(_=>"total");
            foreach(var g in groups)rows.Add(new(g.Key,g.Key,metrics.ToDictionary(m=>m,m=>MetaMetric(m,g))));
        } else {
            var values=await db.ProviderMetricSnapshots.AsNoTracking().Where(x=>x.DataSourceId==sourceId&&x.Date>=since&&x.Date<=until).ToArrayAsync(ct);
            var groups=dimension=="date"?values.GroupBy(x=>x.Date.ToString("yyyy-MM-dd")):dimension=="campaign"?values.GroupBy(x=>x.DimensionKey):values.GroupBy(_=>"total");
            foreach(var g in groups)rows.Add(new(g.Key,dimension=="campaign"?g.First().DimensionName:g.Key,metrics.ToDictionary(m=>m,m=>ProviderMetric(m,g))));
        }
        return new(source.Provider.ToString(),source.Currency,source.TimeZone,rows);
    }
    private async Task<(DashboardShareLink,Dashboard,DashboardVersion)> Authorized(string token,string? password,string? email,bool embed,CancellationToken ct){if(string.IsNullOrWhiteSpace(token)||token.Length!=43)throw Invalid();var link=await db.DashboardShareLinks.SingleOrDefaultAsync(x=>x.TokenHash==Hash(token),ct);var now=clock.GetUtcNow();if(link is null||!link.IsAvailable(now)||embed&&!link.AllowEmbed)throw Invalid();if(link.PasswordHash is not null&&passwords.VerifyHashedPassword(link,link.PasswordHash,password??"")==PasswordVerificationResult.Failed)throw Invalid();if(link.RecipientEmail is not null&&!string.Equals(link.RecipientEmail,email?.Trim(),StringComparison.OrdinalIgnoreCase))throw Invalid();var dashboard=await db.Dashboards.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==link.DashboardId,ct)??throw Invalid();if(dashboard.CurrentPublicationNumber is not int publication)throw Invalid();var version=await db.DashboardVersions.AsNoTracking().SingleOrDefaultAsync(x=>x.DashboardId==dashboard.Id&&x.PublicationNumber==publication,ct)??throw Invalid();return(link,dashboard,version);}
    private static PublicDashboardMetric MetaMetric(string key,IEnumerable<InsightSnapshot> rows){var a=rows.ToArray();var spend=Sum(a.Select(x=>x.Spend));var impressions=Sum(a.Select(x=>(decimal?)x.Impressions));var clicks=Sum(a.Select(x=>(decimal?)x.LinkClicks));var purchases=Sum(a.Select(x=>x.Purchases));var purchaseValue=Sum(a.Select(x=>x.PurchaseValue));decimal? value=key switch{"spend"=>spend,"impressions"=>impressions,"linkClicks"=>clicks,"purchases"=>purchases,"purchaseValue"=>purchaseValue,"ctr"=>Ratio(clicks,impressions,100),"cpc"=>Ratio(spend,clicks),"cpm"=>Ratio(spend,impressions,1000),"cpa"=>Ratio(spend,purchases),"roas"=>Ratio(purchaseValue,spend),_=>null};return Metric(key,value);}
    private static PublicDashboardMetric ProviderMetric(string key,IEnumerable<Domain.DataSources.ProviderMetricSnapshot> rows){var a=rows.ToArray();var spend=Sum(a.Select(x=>x.Spend));var impressions=Sum(a.Select(x=>(decimal?)x.Impressions));var clicks=Sum(a.Select(x=>(decimal?)x.Clicks));var conversions=Sum(a.Select(x=>x.Conversions));var conversionValue=Sum(a.Select(x=>x.ConversionValue));decimal? value=key switch{"spend"=>spend,"impressions"=>impressions,"clicks"=>clicks,"conversions"=>conversions,"conversionValue"=>conversionValue,"activeUsers"=>Sum(a.Select(x=>(decimal?)x.ActiveUsers)),"sessions"=>Sum(a.Select(x=>(decimal?)x.Sessions)),"views"=>Sum(a.Select(x=>(decimal?)x.Views)),"ctr"=>Ratio(clicks,impressions,100),"cpc"=>Ratio(spend,clicks),"cpm"=>Ratio(spend,impressions,1000),"cpa"=>Ratio(spend,conversions),"roas"=>Ratio(conversionValue,spend),_=>null};return Metric(key,value);}
    private static decimal? Sum(IEnumerable<decimal?> values){var present=values.Where(x=>x.HasValue).Select(x=>x!.Value).ToArray();return present.Length==0?null:present.Sum();}
    private static decimal? Ratio(decimal? numerator,decimal? denominator,decimal multiplier=1)=>numerator.HasValue&&denominator is not null and not 0?numerator.Value/denominator.Value*multiplier:null;
    private static PublicDashboardMetric Metric(string key,decimal? value)=>new(value,value.HasValue?"CompleteForSnapshots":"Incomplete",key switch{"spend" or "purchaseValue" or "conversionValue" or "cpc" or "cpm" or "cpa"=>"currency","ctr"=>"percentage","roas"=>"ratio",_=>"count"});
    private async Task<Dashboard> RequiredDashboard(Guid id,CancellationToken ct)=>await db.Dashboards.SingleOrDefaultAsync(x=>x.Id==id&&x.AgencyId==tenant.AgencyId,ct)??throw new EntityNotFoundException("Dashboard was not found.");
    private void RequireManager(){if(tenant.Role is not(nameof(AgencyRole.Owner) or nameof(AgencyRole.Admin)))throw new ForbiddenException("Only Owner and Admin can manage dashboard links.");}
    private DashboardShareModel Map(DashboardShareLink x)=>new(x.Id,x.Status(clock.GetUtcNow()),x.ExpiresAtUtc,x.RecipientEmail,x.PasswordHash is not null,x.AllowFilters,x.AllowExport,x.AllowEmbed,x.AccessCount,x.LastAccessedAtUtc);
    internal static string Hash(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static EntityNotFoundException Invalid()=>new("The shared dashboard link is invalid, expired, revoked or unauthorized.");
}

internal static class PublishedDashboardQueryPolicy
{
    internal static void Validate(JsonElement definition,Guid sourceId,DateOnly since,DateOnly until,string dimension,IReadOnlyList<string> metrics,bool allowFilters)
    {
        if(metrics.Count is < 1 or > 3||metrics.Any(string.IsNullOrWhiteSpace))throw Invalid();
        var candidates=new List<(DateOnly Since,DateOnly Until,string Dimension,HashSet<string> Metrics)>();
        if(!definition.TryGetProperty("pages",out var pages)||pages.ValueKind!=JsonValueKind.Array)throw Invalid();
        foreach(var page in pages.EnumerateArray())
        {
            if(!page.TryGetProperty("components",out var components)||components.ValueKind!=JsonValueKind.Array)continue;
            foreach(var component in components.EnumerateArray())
            {
                if(!component.TryGetProperty("data",out var data)||!data.TryGetProperty("sources",out var sources)||sources.ValueKind!=JsonValueKind.Array)continue;
                var usesSource=sources.EnumerateArray().Any(source=>source.TryGetProperty("dataSourceId",out var id)&&Guid.TryParse(id.GetString(),out var parsed)&&parsed==sourceId);
                if(!usesSource||!data.TryGetProperty("configuration",out var config))continue;
                if(!config.TryGetProperty("since",out var from)||!DateOnly.TryParse(from.GetString(),out var configuredSince)||!config.TryGetProperty("until",out var to)||!DateOnly.TryParse(to.GetString(),out var configuredUntil))continue;
                var configuredDimension=config.TryGetProperty("dimension",out var dim)?dim.GetString()??"none":"none";
                if(component.TryGetProperty("type",out var type)&&type.GetString() is "kpiCard" or "goalGauge")configuredDimension="none";
                var allowedMetrics=new HashSet<string>(StringComparer.Ordinal);
                if(config.TryGetProperty("metrics",out var array)&&array.ValueKind==JsonValueKind.Array)foreach(var metric in array.EnumerateArray())if(metric.GetString() is {Length:>0} value)allowedMetrics.Add(value);
                if(config.TryGetProperty("metric",out var single)&&single.GetString() is {Length:>0} value2)allowedMetrics.Add(value2);
                candidates.Add((configuredSince,configuredUntil,configuredDimension,allowedMetrics));
            }
        }
        var compatible=candidates.Where(x=>x.Dimension==dimension&&metrics.All(x.Metrics.Contains)).ToArray();
        if(compatible.Length==0)throw Invalid();
        if(!allowFilters&&compatible.All(x=>x.Since!=since||x.Until!=until))throw new ForbiddenException("This link does not allow changing published filters.");
    }
    private static EntityNotFoundException Invalid()=>new("The shared dashboard link is invalid, expired, revoked or unauthorized.");
}
