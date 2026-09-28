using System.Net;
using System.Text;
using AnaliticAsd.Infrastructure.Meta;
using Microsoft.Extensions.Configuration;

namespace AnaliticAsd.Tests.Metrics;

public sealed class MetaInsightsClientTests
{
    [Fact]
    public async Task Requests_daily_insights_at_all_levels_without_leaking_token_and_normalizes_actions()
    {
        var handler = new Handler(); var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meta:GraphVersion"] = "v25.0" }).Build();
        var result = await new MetaInsightsClient(http, config).GetDailyAsync("123", "secret-token", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));
        Assert.Equal(4, result.Values.Count); Assert.Contains(result.Values, x => x.MetaAdId == "ad-1" && x.Leads == 3m && x.Purchases == 2m && x.PurchaseValue == 90.5m);
        Assert.All(handler.Urls, x => { Assert.Contains("time_increment=1", x); Assert.Contains("time_range=", x); Assert.DoesNotContain("secret-token", x); });
        Assert.Equal(["account", "campaign", "adset", "ad"], handler.Levels); Assert.All(handler.Schemes, x => Assert.Equal("Bearer", x));
    }

    [Fact]
    public async Task Preserves_omitted_fields_as_null_and_explicit_zero_as_zero()
    {
        var handler = new JsonHandler(_ => "{\"data\":[{\"date_start\":\"2026-09-01\",\"spend\":\"0\",\"impressions\":\"0\",\"reach\":\"0\",\"inline_link_clicks\":\"0\",\"actions\":[{\"action_type\":\"lead\",\"value\":\"0\"}],\"action_values\":[{\"action_type\":\"omni_purchase\",\"value\":\"0\"}]}]}");
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meta:GraphVersion"] = "v25.0" }).Build();

        var result = await new MetaInsightsClient(http, config).GetDailyAsync("123", "secret-token", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));

        Assert.All(result.Values, value =>
        {
            Assert.Equal(0m, value.Spend); Assert.Equal(0, value.Impressions); Assert.Equal(0, value.Reach); Assert.Equal(0, value.LinkClicks);
            Assert.Equal(0m, value.Leads); Assert.Null(value.Purchases); Assert.Equal(0m, value.PurchaseValue);
        });
    }

    [Fact]
    public async Task Follows_cursor_pagination_and_rejects_a_repeated_cursor()
    {
        var handler = new PagedHandler(repeatCursor: false);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Meta:GraphVersion"] = "v25.0" }).Build();

        var result = await new MetaInsightsClient(http, config).GetDailyAsync("123", "secret-token", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1));

        Assert.Equal(8, result.Values.Count);
        Assert.Equal(4, handler.AfterRequests);

        var invalid = new MetaInsightsClient(new HttpClient(new PagedHandler(repeatCursor: true)) { BaseAddress = new Uri("https://graph.facebook.com/") }, config);
        await Assert.ThrowsAsync<AnaliticAsd.Application.Common.ExternalServiceException>(() => invalid.GetDailyAsync("123", "secret-token", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1)));
    }
    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Urls { get; } = []; public List<string> Levels { get; } = []; public List<string?> Schemes { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString(); Urls.Add(url); Schemes.Add(request.Headers.Authorization?.Scheme); var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query); var level = query["level"]!; Levels.Add(level);
            var ids = level switch { "campaign" => ",\"campaign_id\":\"cmp-1\"", "adset" => ",\"campaign_id\":\"cmp-1\",\"adset_id\":\"set-1\"", "ad" => ",\"campaign_id\":\"cmp-1\",\"adset_id\":\"set-1\",\"ad_id\":\"ad-1\"", _ => "" };
            var json = "{\"data\":[{\"date_start\":\"2026-09-01\",\"spend\":\"10.25\",\"impressions\":\"1000\",\"reach\":\"800\",\"inline_link_clicks\":\"20\",\"actions\":[{\"action_type\":\"lead\",\"value\":\"3\"},{\"action_type\":\"omni_purchase\",\"value\":\"2\"}],\"action_values\":[{\"action_type\":\"omni_purchase\",\"value\":\"90.5\"}]" + ids + "}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class JsonHandler(Func<HttpRequestMessage, string> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseFactory(request), Encoding.UTF8, "application/json") });
    }

    private sealed class PagedHandler(bool repeatCursor) : HttpMessageHandler
    {
        public int AfterRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
            var after = query["after"];
            var level = query["level"];
            if (after is not null) AfterRequests++;
            var ids = level switch { "campaign" => ",\"campaign_id\":\"cmp-1\"", "adset" => ",\"campaign_id\":\"cmp-1\",\"adset_id\":\"set-1\"", "ad" => ",\"campaign_id\":\"cmp-1\",\"adset_id\":\"set-1\",\"ad_id\":\"ad-1\"", _ => "" };
            var data = "{\"date_start\":\"2026-09-01\",\"spend\":\"1\",\"impressions\":\"1\",\"reach\":\"1\",\"inline_link_clicks\":\"1\"" + ids + "}";
            var json = after is null
                ? "{\"data\":[" + data + "],\"paging\":{\"next\":\"https://example.test/next\",\"cursors\":{\"after\":\"cursor-1\"}}}"
                : repeatCursor
                    ? "{\"data\":[" + data + "],\"paging\":{\"next\":\"https://example.test/next\",\"cursors\":{\"after\":\"cursor-1\"}}}"
                    : "{\"data\":[" + data + "]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
