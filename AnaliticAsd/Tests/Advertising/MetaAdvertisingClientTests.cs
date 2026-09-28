using System.Net;
using System.Text;
using AnaliticAsd.Infrastructure.Meta;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.Advertising;

public sealed class MetaAdvertisingClientTests
{
    [Fact]
    public async Task Requests_exact_hierarchy_endpoints_with_bearer_and_maps_meta_ids()
    {
        var handler = new Handler(); var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meta:GraphVersion"] = "v25.0" }).Build();

        var result = await new MetaAdvertisingClient(http, configuration).GetHierarchyAsync("123", "secret-token");

        Assert.Single(result.Campaigns); Assert.Single(result.AdSets); Assert.Single(result.Ads);
        Assert.Equal("campaign-1", result.AdSets[0].MetaCampaignId); Assert.Equal("set-1", result.Ads[0].MetaAdSetId);
        Assert.All(handler.AuthorizationSchemes, value => Assert.Equal("Bearer", value));
        Assert.All(handler.Urls, value => Assert.DoesNotContain("secret-token", value, StringComparison.Ordinal));
        Assert.Contains(handler.Urls, value => value.Contains("v25.0/act_123/campaigns", StringComparison.Ordinal));
        Assert.Contains(handler.Urls, value => value.Contains("v25.0/act_123/adsets", StringComparison.Ordinal));
        Assert.Contains(handler.Urls, value => value.Contains("v25.0/act_123/ads", StringComparison.Ordinal));
    }

    private sealed class Handler : HttpMessageHandler
    {
        private int campaignPages;
        public List<string> Urls { get; } = []; public List<string?> AuthorizationSchemes { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString(); Urls.Add(url); AuthorizationSchemes.Add(request.Headers.Authorization?.Scheme);
            var json = url.Contains("/campaigns", StringComparison.Ordinal)
                ? ++campaignPages == 1
                    ? "{\"data\":[{\"id\":\"campaign-1\",\"name\":\"Campaign\",\"objective\":\"OUTCOME_TRAFFIC\",\"status\":\"ACTIVE\",\"effective_status\":\"ACTIVE\"}],\"paging\":{\"cursors\":{\"after\":\"cursor-2\"},\"next\":\"https://graph.facebook.com/next?access_token=secret-token\"}}"
                    : "{\"data\":[]}"
                : url.Contains("/adsets", StringComparison.Ordinal)
                    ? "{\"data\":[{\"id\":\"set-1\",\"campaign_id\":\"campaign-1\",\"name\":\"Set\",\"status\":\"ACTIVE\",\"effective_status\":\"ACTIVE\"}]}"
                    : "{\"data\":[{\"id\":\"ad-1\",\"adset_id\":\"set-1\",\"name\":\"Ad\",\"status\":\"ACTIVE\",\"effective_status\":\"ACTIVE\"}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
