using System.Text.Json;
using System.Text.Json.Serialization;
using AnaliticAsd.Application.Dashboards;

namespace AnaliticAsd.Tests.Dashboards;
public sealed class AdvancedVisualizationContractTests
{
 [Fact]
 public void Advanced_visualizations_keep_stable_camel_case_contract_names()
 {
  var options=new JsonSerializerOptions();options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
  var expected=new Dictionary<DashboardComponentType,string>{[DashboardComponentType.Map]="map",[DashboardComponentType.Bullet]="bullet",[DashboardComponentType.Treemap]="treemap",[DashboardComponentType.Sankey]="sankey",[DashboardComponentType.Waterfall]="waterfall",[DashboardComponentType.BoxPlot]="boxPlot",[DashboardComponentType.Candlestick]="candlestick",[DashboardComponentType.Timeline]="timeline"};
  foreach(var pair in expected)Assert.Equal($"\"{pair.Value}\"",JsonSerializer.Serialize(pair.Key,options));
 }
}
