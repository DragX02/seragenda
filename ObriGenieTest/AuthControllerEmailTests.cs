using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using seragenda;
using seragenda.Controllers;
using seragenda.Models;
using seragenda.Services;

namespace ObriGenieTest;

/// <summary>
/// Tests unitaires pour le flux email de AuthController :
/// inscription → mail de confirmation, confirmation du token, login bloqué/autorisé.
/// IEmailService est mocké pour ne pas appeler le vrai SMTP.
/// </summary>
public class AuthControllerEmailTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static AgendaContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AgendaContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AgendaContext(options);
    }

    private static (AuthController ctrl, Mock<IEmailService> mockEmail) CreateController(AgendaContext ctx)
    {
        var mockEmail = new Mock<IEmailService>();
        mockEmail
            .Setup(s => s.SendConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mockEmail
            .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]      = "test-secret-key-minimum-32-characters!!",
                ["AppSettings:FrontendUrl"]    = "http://localhost:5276"
            })
            .Build();

        return (new AuthController(config, ctx, mockEmail.Object), mockEmail);
    }

    private static RegisterDto MakeRegister(string email = "prof@test.com") => new()
    {
        Email           = email,
        Password        = "Password123",
        ConfirmPassword = "Password123",
        Nom             = "Dupont",
        Prenom          = "Alice"
    };

    // ── Tests Register ────────────────────────────────────────────────────

    [Fact]
    public async Task Register_EnvoieMailDeConfirmation_AvecBonEmail()
    {
        using var ctx = CreateContext(nameof(Register_EnvoieMailDeConfirmation_AvecBonEmail));
        var (ctrl, mockEmail) = CreateController(ctx);

        await ctrl.Register(MakeRegister("prof@test.com"));

        mockEmail.Verify(
            s => s.SendConfirmationEmailAsync("prof@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Register_UrlConfirmation_ContientToken()
    {
        using var ctx = CreateContext(nameof(Register_UrlConfirmation_ContientToken));
        var (ctrl, mockEmail) = CreateController(ctx);

        await ctrl.Register(MakeRegister());

        var user = await ctx.Utilisateurs.FirstAsync();
        mockEmail.Verify(
            s => s.SendConfirmationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(url => url.Contains(user.ConfirmationToken!))),
            Times.Once);
    }

    [Fact]
    public async Task Register_MailEchoue_CompteCreéQuandMeme()
    {
        using var ctx = CreateContext(nameof(Register_MailEchoue_CompteCreéQuandMeme));
        var (ctrl, mockEmail) = CreateController(ctx);
        mockEmail
            .Setup(s => s.SendConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP inaccessible"));

        var result = await ctrl.Register(MakeRegister());

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, await ctx.Utilisateurs.CountAsync());
    }

    [Fact]
    public async Task Register_TokenDeConfirmation_EstStocke()
    {
        using var ctx = CreateContext(nameof(Register_TokenDeConfirmation_EstStocke));
        var (ctrl, _) = CreateController(ctx);

        await ctrl.Register(MakeRegister());

        var user = await ctx.Utilisateurs.FirstAsync();
        Assert.NotNull(user.ConfirmationToken);
        Assert.Equal(64, user.ConfirmationToken!.Length);
        Assert.NotNull(user.ConfirmationTokenExpiresAt);
        Assert.False(user.IsConfirmed);
    }

    [Fact]
    public async Task Register_CompteNonConfirme_LoginBloque()
    {
        using var ctx = CreateContext(nameof(Register_CompteNonConfirme_LoginBloque));
        var (ctrl, _) = CreateController(ctx);

        await ctrl.Register(MakeRegister());

        var result = await ctrl.Login(new LoginDto { Email = "prof@test.com", Password = "Password123" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── Tests ConfirmEmail ────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_TokenValide_CompteActive()
    {
        using var ctx = CreateContext(nameof(Confirm_TokenValide_CompteActive));
        var (ctrl, _) = CreateController(ctx);

        await ctrl.Register(MakeRegister());
        var user = await ctx.Utilisateurs.FirstAsync();

        var result = await ctrl.ConfirmEmail(user.ConfirmationToken!);

        Assert.IsType<OkObjectResult>(result);
        await ctx.Entry(user).ReloadAsync();
        Assert.True(user.IsConfirmed);
        Assert.Null(user.ConfirmationToken);
        Assert.Null(user.ConfirmationTokenExpiresAt);
    }

    [Fact]
    public async Task Confirm_ApresConfirmation_LoginAutorise()
    {
        using var ctx = CreateContext(nameof(Confirm_ApresConfirmation_LoginAutorise));
        var (ctrl, _) = CreateController(ctx);

        await ctrl.Register(MakeRegister());
        var user = await ctx.Utilisateurs.FirstAsync();
        await ctrl.ConfirmEmail(user.ConfirmationToken!);

        var result = await ctrl.Login(new LoginDto { Email = "prof@test.com", Password = "Password123" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_TokenInvalide_Refuse()
    {
        using var ctx = CreateContext(nameof(Confirm_TokenInvalide_Refuse));
        var (ctrl, _) = CreateController(ctx);

        var result = await ctrl.ConfirmEmail("token_qui_nexiste_pas");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_TokenVide_Refuse()
    {
        using var ctx = CreateContext(nameof(Confirm_TokenVide_Refuse));
        var (ctrl, _) = CreateController(ctx);

        var result = await ctrl.ConfirmEmail("");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_TokenExpire_Refuse()
    {
        using var ctx = CreateContext(nameof(Confirm_TokenExpire_Refuse));
        var (ctrl, _) = CreateController(ctx);

        await ctrl.Register(MakeRegister());
        var user = await ctx.Utilisateurs.FirstAsync();
        user.ConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(-1); // expiré
        await ctx.SaveChangesAsync();

        var result = await ctrl.ConfirmEmail(user.ConfirmationToken!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Confirm_DejaConfirme_RetourneOk()
    {
        using var ctx = CreateContext(nameof(Confirm_DejaConfirme_RetourneOk));
        var (ctrl, _) = CreateController(ctx);

        // Créer un utilisateur déjà confirmé avec un token
        ctx.Utilisateurs.Add(new Utilisateur
        {
            Email                       = "already@test.com",
            PasswordHash                = BCrypt.Net.BCrypt.HashPassword("x"),
            Nom                         = "A",
            Prenom                      = "B",
            RoleSysteme                 = "PROF",
            IsConfirmed                 = true,
            ConfirmationToken           = "token_deja_confirme",
            ConfirmationTokenExpiresAt  = DateTime.UtcNow.AddHours(24)
        });
        await ctx.SaveChangesAsync();

        var result = await ctrl.ConfirmEmail("token_deja_confirme");

        Assert.IsType<OkObjectResult>(result);
    }

    // ── Tests WelcomeEmail ────────────────────────────────────────────────

    [Fact]
    public async Task WelcomeEmail_PasEnvoyeLorsInscriptionClassique()
    {
        using var ctx = CreateContext(nameof(WelcomeEmail_PasEnvoyeLorsInscriptionClassique));
        var (ctrl, mockEmail) = CreateController(ctx);

        await ctrl.Register(MakeRegister());

        mockEmail.Verify(
            s => s.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
