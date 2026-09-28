using System.Net;
using System.Net.Http.Json;
using AnaliticAsd.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace AnaliticAsd.Tests;

public sealed class SystemEndpointTests
{
    [Fact]
    public async Task Status_endpoint_is_available()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();
        Assert.NotNull(status);
        Assert.Equal("AnaliticAsd.Api", status.Service);
        Assert.Equal("Healthy", status.Status);
    }

    [Fact]
    public async Task OpenApi_document_is_available_in_development()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/system/status", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Responses_include_correlation_and_security_headers()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        { builder.UseEnvironment("Testing"); builder.ConfigureLogging(logging => logging.ClearProviders()); });
        using var client=factory.CreateClient();using var request=new HttpRequestMessage(HttpMethod.Get,"/api/system/status");
        request.Headers.Add("X-Correlation-ID","test-correlation-42");var response=await client.SendAsync(request);
        Assert.Equal("test-correlation-42",response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("nosniff",response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer",response.Headers.GetValues("Referrer-Policy").Single());
    }
}
