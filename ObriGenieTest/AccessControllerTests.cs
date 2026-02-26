using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Controllers;
using seragenda.Models;
using seragenda.Services;
using System.Security.Claims;
using System.Text.Json;

namespace ObriGenieTest;

/// <summary>
/// Tests unitaires pour AccessController (validation et vérification de licences).
/// </summary>
public class AccessControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static AgendaContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AgendaContext>()
            .UseInMemoryDatabase(dbName).Options);

    private static AccessController CreateController(AgendaContext ctx, string email = "user@test.com")
    {
        var ctrl = new AccessController(ctx);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, email) }, "Test"))
            }
        };
        return ctrl;
    }

    private static Utilisateur AddUser(AgendaContext ctx, string email = "user@test.com")
    {
        var u = new Utilisateur { Email = email, PasswordHash = "h", RoleSysteme = "PROF", IsConfirmed = true, CreatedAt = DateTime.UtcNow };
        ctx.Utilisateurs.Add(u);
        ctx.SaveChanges();
        return u;
    }

    private static License AddLicense(AgendaContext ctx, string plainCode, bool isActive = true, DateTime? expiresAt = null)
    {
        var lic = new License
        {
            Code      = LicenseHelper.HashCode(plainCode),
            IsActive  = isActive,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
        ctx.Licenses.Add(lic);
        ctx.SaveChanges();
        return lic;
    }

    // Lit la propriété "valid" depuis la valeur d'un OkObjectResult (objet anonyme)
    private static bool ReadValid(IActionResult result)
    {
        var ok   = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        return JsonDocument.Parse(json).RootElement.GetProperty("valid").GetBoolean();
    }

    // ══════════════════════════════════════════════════════════════════
    // Validate — POST /api/access/validate
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validate_CodeValide_RetourneOkEtAssigneLicence()
    {
        using var ctx = CreateContext(nameof(Validate_CodeValide_RetourneOkEtAssigneLicence));
        var user = AddUser(ctx);
        AddLicense(ctx, "PROMO2025");

        var result = await CreateController(ctx).Validate(new AccessCodeDto { Code = "PROMO2025" });

        Assert.IsType<OkObjectResult>(result);
        // La licence doit maintenant être assignée à l'utilisateur
        var lic = await ctx.Licenses.FirstAsync();
        Assert.Equal(user.IdUser, lic.AssignedUserId);
    }

    [Fact]
    public async Task Validate_CodeInexistant_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(Validate_CodeInexistant_RetourneBadRequest));
        AddUser(ctx);
        // Aucune licence en base

        var result = await CreateController(ctx).Validate(new AccessCodeDto { Code = "FAUX" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Validate_LicenceRevoquee_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(Validate_LicenceRevoquee_RetourneBadRequest));
        AddUser(ctx);
        AddLicense(ctx, "REVOKE123", isActive: false);

        var result = await CreateController(ctx).Validate(new AccessCodeDto { Code = "REVOKE123" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Validate_LicenceExpiree_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(Validate_LicenceExpiree_RetourneBadRequest));
        AddUser(ctx);
        AddLicense(ctx, "EXPIRE2020", expiresAt: DateTime.UtcNow.AddDays(-1)); // expirée hier

        var result = await CreateController(ctx).Validate(new AccessCodeDto { Code = "EXPIRE2020" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Validate_CodeVide_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(Validate_CodeVide_RetourneBadRequest));
        AddUser(ctx);

        var result = await CreateController(ctx).Validate(new AccessCodeDto { Code = "   " });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ══════════════════════════════════════════════════════════════════
    // Check — GET /api/access/check
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Check_LicenceActive_RetourneValidTrue()
    {
        using var ctx = CreateContext(nameof(Check_LicenceActive_RetourneValidTrue));
        AddUser(ctx);
        AddLicense(ctx, "ACTIF2025");

        var result = await CreateController(ctx).Check("ACTIF2025");

        Assert.True(ReadValid(result));
    }

    [Fact]
    public async Task Check_LicenceRevoquee_RetourneValidFalse()
    {
        using var ctx = CreateContext(nameof(Check_LicenceRevoquee_RetourneValidFalse));
        AddUser(ctx);
        AddLicense(ctx, "REVOKED", isActive: false);

        var result = await CreateController(ctx).Check("REVOKED");

        Assert.False(ReadValid(result));
    }

    [Fact]
    public async Task Check_LicenceExpiree_RetourneValidFalse()
    {
        using var ctx = CreateContext(nameof(Check_LicenceExpiree_RetourneValidFalse));
        AddUser(ctx);
        AddLicense(ctx, "EXPIRED", expiresAt: DateTime.UtcNow.AddHours(-1)); // expirée

        var result = await CreateController(ctx).Check("EXPIRED");

        Assert.False(ReadValid(result));
    }

    [Fact]
    public async Task Check_LicenceNonExistante_RetourneValidFalse()
    {
        using var ctx = CreateContext(nameof(Check_LicenceNonExistante_RetourneValidFalse));
        AddUser(ctx);

        var result = await CreateController(ctx).Check("INEXISTANT");

        Assert.False(ReadValid(result));
    }
}
