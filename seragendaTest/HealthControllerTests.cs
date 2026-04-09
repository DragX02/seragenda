using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace seragendaTest;

/// <summary>
/// Tests d'intégration pour <see cref="seragenda.Controllers.HealthController"/>.
///
/// Utilise <see cref="WebApplicationFactory{TEntryPoint}"/> pour démarrer un serveur
/// en mémoire sans base de données ni réseau réel.
///
/// Couvre :
///   - GET /api/health → 200 OK.
///   - Corps JSON contient les champs status, timestamp, server, version.
///   - Le champ status vaut "online".
///   - Le champ server vaut "AgendaProf API".
///   - Le champ version vaut "1.0.0".
///   - Le champ timestamp est une date/heure UTC récente.
/// </summary>
public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Statut HTTP
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/health doit retourner 200 OK.
    /// </summary>
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Présence des champs JSON
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// La réponse doit contenir les quatre champs attendus.
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────────
    // Valeurs attendues
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Le champ status doit valoir "online".
    /// </summary>
    [Fact]
    public async Task Get_StatusIsOnline()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("online", json.GetProperty("status").GetString());
    }

    /// <summary>
    /// Le champ server doit valoir "AgendaProf API".
    /// </summary>
    [Fact]
    public async Task Get_ServerNameIsAgendaProfApi()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("AgendaProf API", json.GetProperty("server").GetString());
    }

    /// <summary>
    /// Le champ version doit valoir "1.0.0".
    /// </summary>
    [Fact]
    public async Task Get_VersionIs100()
    {
        var response = await _client.GetAsync("/api/health");
        var json     = await ParseJsonAsync(response);

        Assert.Equal("1.0.0", json.GetProperty("version").GetString());
    }

    /// <summary>
    /// Le champ timestamp doit être une date/heure UTC dans les 60 secondes précédant l'appel.
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement;
    }
}
