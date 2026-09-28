using AnaliticAsd.Application.Abstractions;
using AnaliticAsd.Application.AdAccounts;
using AnaliticAsd.Application.Analysis;
using AnaliticAsd.Application.Advertising;
using AnaliticAsd.Application.Clients;
using AnaliticAsd.Application.DataSources;
using AnaliticAsd.Application.Dashboards;
using AnaliticAsd.Application.DashboardQueries;
using AnaliticAsd.Application.Folders;
using AnaliticAsd.Application.Identity;
using AnaliticAsd.Application.Meta;
using AnaliticAsd.Application.Metrics;
using AnaliticAsd.Application.Mcp;
using AnaliticAsd.Application.ProviderIntegrations;
using AnaliticAsd.Application.Reports;
using AnaliticAsd.ErrorHandling;
using AnaliticAsd.Domain.Identity;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Infrastructure.Identity;
using AnaliticAsd.Infrastructure.Persistence;
using AnaliticAsd.Infrastructure.Persistence.Repositories;
using AnaliticAsd.Infrastructure.Meta;
using AnaliticAsd.Infrastructure.ProviderIntegrations;
using AnaliticAsd.Infrastructure.Reports;
using AnaliticAsd.Infrastructure.DashboardExports;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// The Windows Event Log provider can require elevated permissions and must not
// prevent the development server from starting. Console output also gives the
// developer the complete error directly in the terminal.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("AnalitiAds");
if (builder.Environment.IsDevelopment())
{
    // Keep development keys with this checkout. This avoids reusing DPAPI keys
    // created by another Windows account or IDE sandbox.
    var keyDirectory = Path.Combine(builder.Environment.ContentRootPath, ".data-protection");
    Directory.CreateDirectory(keyDirectory);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();
builder.Services.AddScoped<IClientAccessService, ClientAccessService>();
builder.Services.AddScoped<IClientAccessRepository, ClientAccessRepository>();
builder.Services.AddScoped<IClientEditorService, ClientEditorService>();
builder.Services.AddScoped<IClientEditorRepository, ClientEditorRepository>();
builder.Services.AddScoped<CurrentMembershipJwtEvents>();
builder.Services.AddScoped<McpJwtEvents>();
builder.Services.AddScoped<IMcpConnectionService, McpConnectionService>();
builder.Services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IMetaOAuthService, MetaOAuthService>();
builder.Services.AddScoped<IMetaOAuthStateProtector, MetaOAuthStateProtector>();
builder.Services.AddScoped<IMetaTokenProtector, MetaTokenProtector>();
builder.Services.AddScoped<IMetaConnectionRepository, MetaConnectionRepository>();
builder.Services.AddScoped<IProviderConnectionRepository, ProviderConnectionRepository>();
builder.Services.AddScoped<IProviderSourceRepository, ProviderSourceRepository>();
builder.Services.AddScoped<IGoogleIntegrationService, GoogleIntegrationService>();
builder.Services.AddScoped<ITikTokIntegrationService, TikTokIntegrationService>();
builder.Services.AddScoped<IProviderSyncService, ProviderSyncService>();
builder.Services.AddScoped<IProviderMetricRepository, ProviderMetricRepository>();
builder.Services.AddScoped<ProviderScheduleService>();
builder.Services.AddHostedService<ProviderSyncWorker>();
builder.Services.AddSingleton<IProviderOAuthStateProtector, ProviderOAuthStateProtector>();
builder.Services.AddSingleton<IProviderCredentialProtector, ProviderCredentialProtector>();
builder.Services.AddHttpClient<IGoogleProviderClient, GoogleProviderClient>();
builder.Services.AddHttpClient<ITikTokProviderClient, TikTokProviderClient>();
builder.Services.AddHttpClient<IMetaGraphClient, MetaGraphClient>(client => client.BaseAddress = new Uri("https://graph.facebook.com/"));
builder.Services.AddHttpClient<IMetaAdvertisingClient, MetaAdvertisingClient>(client => client.BaseAddress = new Uri("https://graph.facebook.com/"));
builder.Services.AddHttpClient<IMetaInsightsClient, MetaInsightsClient>(client => client.BaseAddress = new Uri("https://graph.facebook.com/"));
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAdAccountService, AdAccountService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddSingleton(builder.Configuration.GetSection("AnalysisRules").Get<AnalysisRulesOptions>() ?? new AnalysisRulesOptions());
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddSingleton<IReportPdfRenderer, SimpleReportPdfRenderer>();
builder.Services.AddScoped<IReportShareService, ReportShareService>();
builder.Services.AddScoped<IReportShareRepository, ReportShareRepository>();
builder.Services.AddScoped<IAdvertisingService, AdvertisingService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<IDashboardRepository>(provider => provider.GetRequiredService<DashboardRepository>());
builder.Services.AddScoped<IDashboardSourceAccess>(provider => provider.GetRequiredService<DashboardRepository>());
builder.Services.AddScoped<IDashboardDefinitionValidator, DashboardDefinitionValidator>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<DashboardShareService>();
builder.Services.AddScoped<DefaultDashboardTemplateService>();
builder.Services.AddScoped<DashboardExportService>();
builder.Services.AddScoped<IDashboardExportRenderer, DashboardExportRenderer>();
builder.Services.AddScoped<IDashboardDeliverySender, SmtpDashboardDeliverySender>();
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<DashboardExportWorker>();
builder.Services.AddScoped<IPasswordHasher<DashboardShareLink>, PasswordHasher<DashboardShareLink>>();
builder.Services.AddScoped<IDashboardQueryRepository, DashboardQueryRepository>();
builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<CrossChannelQueryService>();
builder.Services.AddScoped<IAdAccountRepository, AdAccountRepository>();
builder.Services.AddScoped<IAdvertisingRepository, AdvertisingRepository>();
builder.Services.AddScoped<IMetricsRepository, MetricsRepository>();
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AnalitiAdsDbContext>());
builder.Services.AddScoped<DevelopmentDataSeeder>();
var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Jwt:SigningKey with at least 32 bytes is required in production.");
    jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:SigningKey"] = jwtKey;
}
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AnalitiAds";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AnalitiAds.Frontend";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.EventsType = typeof(CurrentMembershipJwtEvents);
    options.IncludeErrorDetails = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Email,
        RoleClaimType = ClaimTypes.Role
    };
}).AddJwtBearer("McpBearer", options =>
{
    options.EventsType = typeof(McpJwtEvents); options.IncludeErrorDetails = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Mcp:Audience"] ?? "AnalitiAds.Mcp",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        RoleClaimType = ClaimTypes.Role
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("McpRead", policy =>
{
    policy.AddAuthenticationSchemes("McpBearer"); policy.RequireAuthenticatedUser(); policy.RequireClaim("scope", McpConnectionService.ReadScope);
}));
builder.Services.AddMcpServer().WithHttpTransport(options => options.Stateless = true).AddAuthorizationFilters().WithTools<AnalitiAdsMcpTools>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ClientInvitationAcceptance", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("SharedReportAccess", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        { Status = 429, Title = "Too many invitation attempts", Detail = "Wait one minute and try again." },
            options: null, contentType: "application/problem+json", cancellationToken: ct);
    };
});
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddDbContext<AnalitiAdsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AnalitiAds");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'AnalitiAds' is required. Configure it outside source control for non-development environments.");
    }

    options.UseNpgsql(PostgresConnectionStringNormalizer.Normalize(connectionString));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

// The local HTTP profile intentionally has no HTTPS endpoint. Enabling the
// redirect there only emits a misleading warning on every request.
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100) correlationId = Guid.NewGuid().ToString("N");
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (context.Request.Path.StartsWithSegments("/api/v1")) context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapGet("/.well-known/oauth-protected-resource/mcp", (HttpRequest request, IConfiguration configuration) => Results.Json(new
{
    resource = configuration["Mcp:Resource"] ?? $"{request.Scheme}://{request.Host}/mcp",
    authorization_servers = new[] { configuration["Mcp:AuthorizationServer"] ?? $"{request.Scheme}://{request.Host}" },
    scopes_supported = new[] { McpConnectionService.ReadScope }
}));
app.MapMcp("/mcp").RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "McpBearer", Policy = "McpRead" });

app.Run();

public partial class Program;
