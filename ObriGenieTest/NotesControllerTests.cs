using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Controllers;
using seragenda.Models;
using System.Security.Claims;

namespace ObriGenieTest;

/// <summary>
/// Tests unitaires pour NotesController.
/// Chaque test utilise une base InMemory isolée pour éviter les interférences.
/// </summary>
public class NotesControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static AgendaContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AgendaContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AgendaContext(options);
    }

    private static NotesController CreateController(AgendaContext context, string email = "user@test.com")
    {
        var controller = new NotesController(context);
        var claims = new List<Claim> { new(ClaimTypes.Name, email) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    private static Utilisateur AddUser(AgendaContext ctx, string email = "user@test.com")
    {
        var user = new Utilisateur
        {
            Email        = email,
            PasswordHash = "hash",
            RoleSysteme  = "PROF",
            IsConfirmed  = true,
            CreatedAt    = DateTime.UtcNow
        };
        ctx.Utilisateurs.Add(user);
        ctx.SaveChanges();
        return user;
    }

    private static UserNote MakeNote(int userId, DateTime date, int hour = 9, int endHour = 10, string content = "Note test")
        => new()
        {
            IdUserFk   = userId,
            Date       = date,
            Hour       = hour,
            EndHour    = endHour,
            Content    = content,
            CreatedAt  = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

    // ══════════════════════════════════════════════════════════════════
    // Save — création
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Save_NoteValide_RetourneOkEtEnregistre()
    {
        using var ctx = CreateContext(nameof(Save_NoteValide_RetourneOkEtEnregistre));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        var result = await ctrl.Save(new UserNote
            { Date = DateTime.Today, Hour = 9, EndHour = 11, Content = "Révision" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, await ctx.UserNotes.CountAsync());
    }

    [Fact]
    public async Task Save_ContenuHtml_EstSanitise()
    {
        using var ctx = CreateContext(nameof(Save_ContenuHtml_EstSanitise));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        await ctrl.Save(new UserNote
        {
            Date    = DateTime.Today,
            Hour    = 9,
            EndHour = 10,
            Content = "<b>Important</b><script>alert('xss')</script>Note"
        });

        var saved = await ctx.UserNotes.FirstAsync();
        // Balises et contenu <script> supprimés, texte normal conservé
        Assert.Equal("ImportantNote", saved.Content);
        Assert.DoesNotContain("alert", saved.Content);
        Assert.DoesNotContain("<", saved.Content);
    }

    [Fact]
    public async Task Save_ContenuTropLong_EstTronque()
    {
        using var ctx = CreateContext(nameof(Save_ContenuTropLong_EstTronque));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        await ctrl.Save(new UserNote
            { Date = DateTime.Today, Hour = 9, EndHour = 10, Content = new string('X', 3000) });

        var saved = await ctx.UserNotes.FirstAsync();
        Assert.Equal(2000, saved.Content.Length);
    }

    [Fact]
    public async Task Save_HeureDebutInvalide_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(Save_HeureDebutInvalide_RetourneBadRequest));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        // Heure 3 < 6 → invalide
        var result = await ctrl.Save(new UserNote
            { Date = DateTime.Today, Hour = 3, EndHour = 4, Content = "Trop tôt" });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, await ctx.UserNotes.CountAsync());
    }

    [Fact]
    public async Task Save_DateNormalisee_SupprimeLaPartieHeure()
    {
        using var ctx = CreateContext(nameof(Save_DateNormalisee_SupprimeLaPartieHeure));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        // Simule une date avec composante heure (bug fuseau horaire UTC+1→UTC)
        await ctrl.Save(new UserNote
        {
            Date    = new DateTime(2025, 2, 23, 23, 0, 0, DateTimeKind.Utc),
            Hour    = 9,
            EndHour = 10,
            Content = "Test fuseau"
        });

        var saved = await ctx.UserNotes.FirstAsync();
        Assert.Equal(new DateTime(2025, 2, 23, 0, 0, 0), saved.Date);
        Assert.Equal(DateTimeKind.Unspecified, saved.Date.Kind);
    }

    [Fact]
    public async Task Save_UtilisateurInconnu_RetourneUnauthorized()
    {
        using var ctx = CreateContext(nameof(Save_UtilisateurInconnu_RetourneUnauthorized));
        // Pas d'utilisateur en DB → GetUserId() retourne null
        var ctrl = CreateController(ctx, "fantome@test.com");

        var result = await ctrl.Save(new UserNote
            { Date = DateTime.Today, Hour = 9, EndHour = 10, Content = "Note" });

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ══════════════════════════════════════════════════════════════════
    // Save — édition
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Save_EditionNoteExistante_MisAJour()
    {
        using var ctx = CreateContext(nameof(Save_EditionNoteExistante_MisAJour));
        var user = AddUser(ctx);
        var note = MakeNote(user.IdUser, DateTime.Today, content: "Ancien contenu");
        ctx.UserNotes.Add(note);
        await ctx.SaveChangesAsync();

        var ctrl = CreateController(ctx);
        var result = await ctrl.Save(new UserNote
        {
            Id      = note.Id,
            Date    = DateTime.Today,
            Hour    = 10,
            EndHour = 12,
            Content = "Nouveau contenu"
        });

        Assert.IsType<OkObjectResult>(result);
        var updated = await ctx.UserNotes.FirstAsync();
        Assert.Equal("Nouveau contenu", updated.Content);
        Assert.Equal(10, updated.Hour);
    }

    [Fact]
    public async Task Save_EditionNoteAutreUser_RetourneNotFound()
    {
        using var ctx = CreateContext(nameof(Save_EditionNoteAutreUser_RetourneNotFound));
        var user1 = AddUser(ctx, "user1@test.com");
        var user2 = AddUser(ctx, "user2@test.com");
        var note  = MakeNote(user2.IdUser, DateTime.Today);
        ctx.UserNotes.Add(note);
        await ctx.SaveChangesAsync();

        // user1 essaie d'éditer la note de user2
        var ctrl   = CreateController(ctx, "user1@test.com");
        var result = await ctrl.Save(new UserNote
            { Id = note.Id, Date = DateTime.Today, Hour = 9, EndHour = 10, Content = "Piratage" });

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal("Note test", (await ctx.UserNotes.FirstAsync()).Content); // inchangée
    }

    // ══════════════════════════════════════════════════════════════════
    // GetNotesForDate
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetNotesForDate_RetourneSeulementNotesUtilisateurCourant()
    {
        using var ctx = CreateContext(nameof(GetNotesForDate_RetourneSeulementNotesUtilisateurCourant));
        var user1 = AddUser(ctx, "user1@test.com");
        var user2 = AddUser(ctx, "user2@test.com");
        var date  = new DateTime(2025, 3, 15);

        ctx.UserNotes.AddRange(
            MakeNote(user1.IdUser, date, content: "Note user1"),
            MakeNote(user2.IdUser, date, content: "Note user2")
        );
        await ctx.SaveChangesAsync();

        var ctrl   = CreateController(ctx, "user1@test.com");
        var result = await ctrl.GetNotesForDate(date);

        var ok    = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<List<UserNote>>(ok.Value);
        Assert.Single(notes);
        Assert.Equal("Note user1", notes[0].Content);
    }

    [Fact]
    public async Task GetNotesForDate_MauvaisJour_RetourneListeVide()
    {
        using var ctx = CreateContext(nameof(GetNotesForDate_MauvaisJour_RetourneListeVide));
        var user = AddUser(ctx);
        ctx.UserNotes.Add(MakeNote(user.IdUser, new DateTime(2025, 3, 15)));
        await ctx.SaveChangesAsync();

        var ctrl   = CreateController(ctx);
        var result = await ctrl.GetNotesForDate(new DateTime(2025, 3, 16));

        var ok    = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<List<UserNote>>(ok.Value);
        Assert.Empty(notes);
    }

    // ══════════════════════════════════════════════════════════════════
    // GetNotesForRange
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetNotesForRange_PlageTropGrande_RetourneBadRequest()
    {
        using var ctx = CreateContext(nameof(GetNotesForRange_PlageTropGrande_RetourneBadRequest));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        var result = await ctrl.GetNotesForRange(new DateTime(2025, 1, 1), new DateTime(2025, 4, 1));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetNotesForRange_PlageValide_RetourneNotesIncluses()
    {
        using var ctx = CreateContext(nameof(GetNotesForRange_PlageValide_RetourneNotesIncluses));
        var user = AddUser(ctx);
        ctx.UserNotes.AddRange(
            MakeNote(user.IdUser, new DateTime(2025, 3, 10), content: "Dans la plage"),
            MakeNote(user.IdUser, new DateTime(2025, 4, 20), content: "Hors plage")
        );
        await ctx.SaveChangesAsync();

        var ctrl   = CreateController(ctx);
        var result = await ctrl.GetNotesForRange(new DateTime(2025, 3, 1), new DateTime(2025, 3, 31));

        var ok    = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<List<UserNote>>(ok.Value);
        Assert.Single(notes);
        Assert.Equal("Dans la plage", notes[0].Content);
    }

    // ══════════════════════════════════════════════════════════════════
    // Delete
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Delete_PropriaNota_RetourneOkEtSupprime()
    {
        using var ctx = CreateContext(nameof(Delete_PropriaNota_RetourneOkEtSupprime));
        var user = AddUser(ctx);
        var note = MakeNote(user.IdUser, DateTime.Today);
        ctx.UserNotes.Add(note);
        await ctx.SaveChangesAsync();

        var ctrl   = CreateController(ctx);
        var result = await ctrl.Delete(note.Id);

        Assert.IsType<OkResult>(result);
        Assert.Equal(0, await ctx.UserNotes.CountAsync());
    }

    [Fact]
    public async Task Delete_NoteAutreUser_RetourneNotFound()
    {
        using var ctx = CreateContext(nameof(Delete_NoteAutreUser_RetourneNotFound));
        var user1 = AddUser(ctx, "user1@test.com");
        var user2 = AddUser(ctx, "user2@test.com");
        var note  = MakeNote(user2.IdUser, DateTime.Today);
        ctx.UserNotes.Add(note);
        await ctx.SaveChangesAsync();

        // user1 essaie de supprimer la note de user2
        var ctrl   = CreateController(ctx, "user1@test.com");
        var result = await ctrl.Delete(note.Id);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, await ctx.UserNotes.CountAsync()); // note toujours présente
    }

    [Fact]
    public async Task Delete_NoteInexistante_RetourneNotFound()
    {
        using var ctx = CreateContext(nameof(Delete_NoteInexistante_RetourneNotFound));
        AddUser(ctx);
        var ctrl = CreateController(ctx);

        var result = await ctrl.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
