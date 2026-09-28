using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AnaliticAsd.Application.Dashboards;
using AnaliticAsd.Domain.Advertising;
using AnaliticAsd.Domain.DataSources;
using AnaliticAsd.Domain.Dashboards;
using AnaliticAsd.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnaliticAsd.Infrastructure.DashboardExports;

public sealed record RenderedDashboardExport(string FileName,string ContentType,byte[] Content);
public interface IDashboardExportRenderer { Task<RenderedDashboardExport> RenderAsync(Dashboard dashboard,DashboardVersion version,DashboardExportFormat format,CancellationToken ct); }

public sealed class DashboardExportRenderer(AnalitiAdsDbContext db):IDashboardExportRenderer
{
 public async Task<RenderedDashboardExport> RenderAsync(Dashboard dashboard,DashboardVersion version,DashboardExportFormat format,CancellationToken ct)
 {
  using var document=JsonDocument.Parse(version.DefinitionJson);var rows=await Rows(dashboard,document.RootElement,ct);var safe=string.Concat(dashboard.Title.Select(c=>char.IsLetterOrDigit(c)?c:'-')).Trim('-');if(safe.Length==0)safe="dashboard";
  return format switch { DashboardExportFormat.Pdf=>new($"{safe}-v{version.PublicationNumber}.pdf","application/pdf",Pdf(dashboard,version,rows)),DashboardExportFormat.Csv=>new($"{safe}-v{version.PublicationNumber}.csv","text/csv; charset=utf-8",Csv(rows)),_=>new($"{safe}-v{version.PublicationNumber}.xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",Xlsx(rows)) };
 }
 private async Task<List<string[]>> Rows(Dashboard dashboard,JsonElement definition,CancellationToken ct)
 {
  var rows=new List<string[]> { new[] { "Página","Componente","Proveedor","Fuente","Desde","Hasta","Métrica","Valor","Disponibilidad" } };
  foreach(var page in definition.GetProperty("pages").EnumerateArray())foreach(var component in page.GetProperty("components").EnumerateArray())
  {
   if(!component.TryGetProperty("data",out var data)||!data.TryGetProperty("configuration",out var config)||config.ValueKind!=JsonValueKind.Object)continue;
   if(!data.TryGetProperty("sources",out var sources)||sources.GetArrayLength()==0||!sources[0].TryGetProperty("dataSourceId",out var sourceValue)||!Guid.TryParse(sourceValue.GetString(),out var sourceId))continue;
   var source=await db.DataSources.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==sourceId&&x.AgencyId==dashboard.AgencyId&&x.ClientId==dashboard.ClientId,ct);if(source is null)continue;
   if(!config.TryGetProperty("since",out var sinceValue)||!DateOnly.TryParse(sinceValue.GetString(),out var since)||!config.TryGetProperty("until",out var untilValue)||!DateOnly.TryParse(untilValue.GetString(),out var until))continue;
   var metrics=new List<string>();if(config.TryGetProperty("metrics",out var many)&&many.ValueKind==JsonValueKind.Array)metrics.AddRange(many.EnumerateArray().Select(x=>x.GetString()).Where(x=>x is not null)!);else if(config.TryGetProperty("metric",out var one)&&one.ValueKind==JsonValueKind.String)metrics.Add(one.GetString()!);
   var values=source.Provider==DataProvider.MetaAds?await Meta(sourceId,since,until,metrics,ct):await Provider(sourceId,since,until,metrics,ct);
   var pageName=page.GetProperty("name").GetString()??"";var title=config.TryGetProperty("title",out var t)?t.GetString()??component.GetProperty("type").GetString()??"":component.GetProperty("type").GetString()??"";
   foreach(var metric in metrics){var value=values.GetValueOrDefault(metric);rows.Add([pageName,title,source.Provider.ToString(),source.Name,since.ToString("yyyy-MM-dd"),until.ToString("yyyy-MM-dd"),metric,value?.ToString(CultureInfo.InvariantCulture)??"",""+(value.HasValue?"Available":"Unavailable")]);}
  }
  return rows;
 }
 private async Task<Dictionary<string,decimal?>> Meta(Guid id,DateOnly since,DateOnly until,IReadOnlyList<string> metrics,CancellationToken ct)
 {var x=await db.InsightSnapshots.AsNoTracking().Where(v=>v.AdAccountId==id&&v.Level==InsightLevel.Account&&v.SnapshotDate>=since&&v.SnapshotDate<=until).ToArrayAsync(ct);decimal? S(Func<InsightSnapshot,decimal?> f){var a=x.Select(f).Where(v=>v.HasValue).Select(v=>v!.Value).ToArray();return a.Length==0?null:a.Sum();}decimal? L(Func<InsightSnapshot,long?> f)=>S(v=>f(v));var spend=S(v=>v.Spend);var impressions=L(v=>v.Impressions);var clicks=L(v=>v.LinkClicks);var leads=S(v=>v.Leads);var purchases=S(v=>v.Purchases);var revenue=S(v=>v.PurchaseValue);return metrics.ToDictionary(m=>m,m=>m switch{"spend"=>spend,"impressions"=>impressions,"linkClicks"=>clicks,"leads"=>leads,"purchases"=>purchases,"purchaseValue"=>revenue,"ctr"=>Divide(clicks,impressions,100),"cpc"=>Divide(spend,clicks),"cpm"=>Divide(spend,impressions,1000),"cpl"=>Divide(spend,leads),"cpa"=>Divide(spend,purchases),"roas"=>Divide(revenue,spend),_=>null});}
 private async Task<Dictionary<string,decimal?>> Provider(Guid id,DateOnly since,DateOnly until,IReadOnlyList<string> metrics,CancellationToken ct)
 {var x=await db.ProviderMetricSnapshots.AsNoTracking().Where(v=>v.DataSourceId==id&&v.Date>=since&&v.Date<=until).ToArrayAsync(ct);decimal? S(Func<ProviderMetricSnapshot,decimal?> f){var a=x.Select(f).Where(v=>v.HasValue).Select(v=>v!.Value).ToArray();return a.Length==0?null:a.Sum();}decimal? L(Func<ProviderMetricSnapshot,long?> f)=>S(v=>f(v));var spend=S(v=>v.Spend);var impressions=L(v=>v.Impressions);var clicks=L(v=>v.Clicks);var conversions=S(v=>v.Conversions);var revenue=S(v=>v.ConversionValue);return metrics.ToDictionary(m=>m,m=>m switch{"spend"=>spend,"impressions"=>impressions,"clicks"=>clicks,"conversions"=>conversions,"conversionValue"=>revenue,"activeUsers"=>L(v=>v.ActiveUsers),"sessions"=>L(v=>v.Sessions),"views"=>L(v=>v.Views),"ctr"=>Divide(clicks,impressions,100),"cpc"=>Divide(spend,clicks),"cpm"=>Divide(spend,impressions,1000),"cpa"=>Divide(spend,conversions),"roas"=>Divide(revenue,spend),_=>null});}
 private static decimal? Divide(decimal? a,decimal? b,decimal factor=1)=>a.HasValue&&b is >0?a.Value/b.Value*factor:null;
 private static byte[] Csv(IEnumerable<string[]> rows)=>Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n",rows.Select(r=>string.Join(',',r.Select(Escape)))))).ToArray();
 private static string Escape(string value)=>$"\"{value.Replace("\"","\"\"")}\"";
 private static byte[] Xlsx(IReadOnlyList<string[]> rows){using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){Write(zip,"[Content_Types].xml","<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");Write(zip,"_rels/.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");Write(zip,"xl/_rels/workbook.xml.rels","<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");Write(zip,"xl/workbook.xml","<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Datos\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");var xml="<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>"+string.Join("",rows.Select((r,i)=>$"<row r=\"{i+1}\">"+string.Join("",r.Select((v,j)=>$"<c r=\"{(char)('A'+j)}{i+1}\" t=\"inlineStr\"><is><t>{System.Security.SecurityElement.Escape(v)}</t></is></c>"))+"</row>"))+"</sheetData></worksheet>";Write(zip,"xl/worksheets/sheet1.xml",xml);}return output.ToArray();}
 private static void Write(ZipArchive zip,string name,string text){var e=zip.CreateEntry(name);using var w=new StreamWriter(e.Open(),new UTF8Encoding(false));w.Write(text);}
 private static byte[] Pdf(Dashboard d,DashboardVersion v,IReadOnlyList<string[]> rows){var lines=new[]{d.Title,$"Publicación {v.PublicationNumber} · {v.PublishedAtUtc:yyyy-MM-dd HH:mm} UTC"}.Concat(rows.Skip(1).Select(r=>$"{r[0]} | {r[1]} | {r[6]}: {(r[7].Length==0?"Sin datos":r[7])}")).ToArray();var content=new StringBuilder("BT /F1 10 Tf 45 780 Td ");foreach(var line in lines.Take(48))content.Append($"({PdfText(line)}) Tj 0 -15 Td ");content.Append("ET");var stream=Encoding.Latin1.GetBytes(content.ToString());var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Count 1 /Kids [3 0 R] >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",$"<< /Length {stream.Length} >>\nstream\n{Encoding.Latin1.GetString(stream)}\nendstream"};using var o=new MemoryStream();var W=(string s)=>{var b=Encoding.Latin1.GetBytes(s);o.Write(b);};W("%PDF-1.4\n");var offsets=new List<long>{0};for(var i=0;i<objects.Length;i++){offsets.Add(o.Position);W($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}var x=o.Position;W($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");foreach(var p in offsets.Skip(1))W($"{p:0000000000} 00000 n \n");W($"trailer << /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{x}\n%%EOF");return o.ToArray();}
 private static string PdfText(string s)=>s.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)").Replace('–','-').Replace('—','-');
}
