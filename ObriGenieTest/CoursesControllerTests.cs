using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Controllers;
using seragenda.Models;
using System.Security.Claims;

namespace ObriGenieTest;

/// <summary>
/// Tests unitaires pour CoursesController.
/// Couvre le filtre par jour (flags binaires), la plage de dates, l'isolation par user.
/// </summary>
public class CoursesControllerTests
{
    // Flags binaires identiques à CoursesController
    private const int LUNDI    = 1;
    private const int MARDI    = 2;
    private const int MERCREDI = 4;
    private const int JEUDI    = 8;
    private const int VENDREDI = 16;

    // 2025-03-17 = lundi, 2025-03-18 = mardi, 2025-03-19 = mercredi…
    private static readonly DateTime Lundi    = new(2025, 3, 17);
    private static readonly DateTime Mardi    = new(2025, 3, 18);
    private static readonly DateTime Mercredi = new(2025, 3, 19);

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AgendaContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<AgendaContext>()
            .UseInMemoryDatabase(dbName).Options);

    private static CoursesController CreateController(AgendaContext ctx, string email = "user@test.com")
    {
        var ctrl = new CoursesController(ctx);
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

    private static UserCourse MakeCours(int userId, int days, DateTime? start = null, DateTime? end = null)
        => new()
        {
            IdUserFk   = userId,
            Name       = "Test",
            Color      = "#FF0000",
            DaysOfWeek = days,
            StartDate  = start ?? new DateTime(2025, 1, 1),  // couvre toute l'année 2025
            EndDate    = end   ?? new DateTime(2025, 12, 31),
            StartTime  = TimeSpan.FromHours(8),
            EndTime    = TimeSpan.FromHours(10)
        };

    // ══════════════════════════════════════════════════════════════════
    // Filtre par jour de la semaine (flags binaires)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCoursesForDate_CoursLundi_RetourneSurLundi()
    {
        using var ctx = CreateContext(nameof(GetCoursesForDate_CoursLundi_RetourneSurLundi));
        var user = AddUser(ctx);
        ctx.UserCourses.Add(MakeCours(user.IdUser, LUNDI));
        await ctx.SaveChangesAsync();

        var result = await CreateController(ctx).GetCoursesForDate(Lundi);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<UserCourse>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetCoursesForDate_CoursLundi_PasRetourneSurMardi()
    {
        using var ctx = CreateContext(nameof(GetCoursesForDate_CoursLundi_PasRetourneSurMardi));
        var user = AddUser(ctx);
        ctx.UserCourses.Add(MakeCours(user.IdUser, LUNDI)); // uniquement lundi
        await ctx.SaveChangesAsync();

        var result = await CreateController(ctx).GetCoursesForDate(Mardi);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<UserCourse>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCoursesForDate_CoursMultiJours_RetourneSurChaqueBonJour()
    {
        using var ctx = CreateContext(nameof(GetCoursesForDate_CoursMultiJours_RetourneSurChaqueBonJour));
        var user = AddUser(ctx);
        // Cours lundi + mercredi + vendredi (1 + 4 + 16 = 21)
        ctx.UserCourses.Add(MakeCours(user.IdUser, LUNDI | MERCREDI | VENDREDI));
        await ctx.SaveChangesAsync();

        var ctrl = CreateController(ctx);

        var resLundi = await ctrl.GetCoursesForDate(Lundi);
        var resMardi = await ctrl.GetCoursesForDate(Mardi);    // pas dans la liste
        var resMerc  = await ctrl.GetCoursesForDate(Mercredi);

        Assert.Single(((OkObjectResult)resLundi).Value as List<UserCourse> ?? new());
        Assert.Empty(((OkObjectResult)resMardi).Value as List<UserCourse> ?? new());
        Assert.Single(((OkObjectResult)resMerc).Value as List<UserCourse>  ?? new());
    }

    // ══════════════════════════════════════════════════════════════════
    // Filtre par plage de dates
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCoursesForDate_DateHorsPlage_PasRetourne()
    {
        using var ctx = CreateContext(nameof(GetCoursesForDate_DateHorsPlage_PasRetourne));
        var user = AddUser(ctx);
        // Cours valable uniquement en janvier 2025
        ctx.UserCourses.Add(MakeCours(user.IdUser, LUNDI | MARDI | MERCREDI | JEUDI | VENDREDI,
            start: new DateTime(2025, 1, 1), end: new DateTime(2025, 1, 31)));
        await ctx.SaveChangesAsync();

        // On demande un lundi de mars → hors plage
        var result = await CreateController(ctx).GetCoursesForDate(Lundi); // 2025-03-17

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((List<UserCourse>)ok.Value!);
    }

    // ══════════════════════════════════════════════════════════════════
    // Isolation par utilisateur
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCoursesForDate_RetourneSeulementCoursUtilisateurCourant()
    {
        using var ctx = CreateContext(nameof(GetCoursesForDate_RetourneSeulementCoursUtilisateurCourant));
        var user1 = AddUser(ctx, "user1@test.com");
        var user2 = AddUser(ctx, "user2@test.com");

        ctx.UserCourses.AddRange(
            MakeCours(user1.IdUser, LUNDI),
            MakeCours(user2.IdUser, LUNDI)
        );
        await ctx.SaveChangesAsync();

        var result = await CreateController(ctx, "user1@test.com").GetCoursesForDate(Lundi);

        var ok   = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<UserCourse>>(ok.Value);
        Assert.Single(list);
        Assert.All(list, c => Assert.Equal(user1.IdUser, c.IdUserFk));
    }
}
