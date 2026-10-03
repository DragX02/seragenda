using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace seragendaTest;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ResponseContainsRequiredFields()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.True(json.TryGetProperty("status",    out _), "Champ 'status' manquant.");
        Assert.True(json.TryGetProperty("timestamp", out _), "Champ 'timestamp' manquant.");
        Assert.True(json.TryGetProperty("server",    out _), "Champ 'server' manquant.");
        Assert.True(json.TryGetProperty("version",   out _), "Champ 'version' manquant.");
    }

    [Fact]
    public async Task Get_StatusIsOnline()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("online", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Get_ServerNameIsAgendaProfApi()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("AgendaProf API", json.GetProperty("server").GetString());
    }

    [Fact]
    public async Task Get_VersionIs100()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("1.0.0", json.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Get_TimestampIsRecentUtc()
    {
        var before   = DateTime.UtcNow.AddSeconds(-5);
        var response = await _client.GetAsync("/api/health");
        var after    = DateTime.UtcNow.AddSeconds(5);
        var json     = await ParseJsonAsync(response);

        var ts = json.GetProperty("timestamp").GetDateTime();

        Assert.True(ts >= before && ts <= after,
            $"timestamp {ts:O} doit être compris entre {before:O} et {after:O}.");
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement;
    }
}
