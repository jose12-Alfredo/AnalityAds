namespace AnaliticAsd.Domain.Dashboards;
public sealed class BrandProfile
{
 private BrandProfile(){}
 public Guid Id{get;private set;} public Guid AgencyId{get;private set;} public Guid? ClientId{get;private set;}
 public string? LogoUrl{get;private set;} public string PrimaryColor{get;private set;}="#8054D8";
 public string SecondaryColor{get;private set;}="#3C9CA0"; public string BackgroundColor{get;private set;}="#F5F6F8";
 public string FontFamily{get;private set;}="Arial"; public DateTimeOffset UpdatedAtUtc{get;private set;} public Guid Version{get;private set;}
 public static BrandProfile Create(Guid agencyId,Guid? clientId,string? logo,string primary,string secondary,string background,string font,DateTimeOffset now){var x=new BrandProfile{Id=Guid.NewGuid(),AgencyId=agencyId,ClientId=clientId};x.Update(logo,primary,secondary,background,font,now);return x;}
 public void Update(string? logo,string primary,string secondary,string background,string font,DateTimeOffset now){var normalizedLogo=string.IsNullOrWhiteSpace(logo)?null:logo!.Trim();if(normalizedLogo is not null&&(!Uri.TryCreate(normalizedLogo,UriKind.Absolute,out var uri)||uri is null||uri.Scheme!="https"))throw new ArgumentException("Logo URL must use HTTPS."); var normalizedFont=string.IsNullOrWhiteSpace(font)?throw new ArgumentException("Font is required."):font.Trim(); LogoUrl=normalizedLogo;PrimaryColor=Color(primary);SecondaryColor=Color(secondary);BackgroundColor=Color(background);FontFamily=normalizedFont[..Math.Min(normalizedFont.Length,100)];UpdatedAtUtc=now.ToUniversalTime();Version=Guid.NewGuid();}
 private static string Color(string value)=>System.Text.RegularExpressions.Regex.IsMatch(value,"^#[0-9A-Fa-f]{6}$")?value.ToUpperInvariant():throw new ArgumentException("Colors must use #RRGGBB.");
}


