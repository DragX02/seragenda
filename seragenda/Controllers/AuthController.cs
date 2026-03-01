using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using seragenda.Models;
using seragenda.Services;
using seragenda.Validators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace seragenda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AgendaContext _context;
        private readonly IEmailService _emailService;

        public AuthController(IConfiguration configuration, AgendaContext context, IEmailService emailService)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest("L'email et le mot de passe sont requis.");
            }

            // Validation de l'email
            if (!InputValidator.IsValidEmail(loginDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Vérifier les caractères dangereux
            if (InputValidator.ContainsDangerousCharacters(loginDto.Email) ||
                InputValidator.ContainsDangerousCharacters(loginDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Rechercher l'utilisateur par email
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Vérifier le mot de passe
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Vérifier que le compte est confirmé
            if (!user.IsConfirmed)
            {
                return Unauthorized("Veuillez confirmer votre email avant de vous connecter.");
            }

            var jwt = GenerateToken(user);

            return Ok(new {
                Token = jwt,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom
            });
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Validation des champs requis
            if (string.IsNullOrEmpty(registerDto.Email) ||
                string.IsNullOrEmpty(registerDto.Password) ||
                string.IsNullOrEmpty(registerDto.Nom) ||
                string.IsNullOrEmpty(registerDto.Prenom))
            {
                return BadRequest("Tous les champs sont requis.");
            }

            // Validation de l'email
            if (!InputValidator.IsValidEmail(registerDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Validation du nom et prénom
            if (!InputValidator.IsValidName(registerDto.Nom))
            {
                return BadRequest("Le nom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            if (!InputValidator.IsValidName(registerDto.Prenom))
            {
                return BadRequest("Le prénom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            // Validation du mot de passe
            if (!InputValidator.IsValidPassword(registerDto.Password))
            {
                return BadRequest("Le mot de passe doit contenir entre 6 et 100 caractères.");
            }

            // Vérifier les caractères dangereux
            if (InputValidator.ContainsDangerousCharacters(registerDto.Email) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Nom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Prenom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Vérifier que le mot de passe et la confirmation correspondent
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return BadRequest("Les mots de passe ne correspondent pas.");
            }

            // Vérifier si l'utilisateur existe déjà
            var existingUser = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest("Un compte avec cet email existe déjà.");
            }

            // Sanitize les entrées avant de les enregistrer
            var sanitizedNom = InputValidator.SanitizeInput(registerDto.Nom);
            var sanitizedPrenom = InputValidator.SanitizeInput(registerDto.Prenom);

            // Générer un token de confirmation (valable 24h)
            var confirmationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // Créer le nouvel utilisateur (non confirmé jusqu'à la validation par email)
            var user = new Utilisateur
            {
                Email = registerDto.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Nom = sanitizedNom,
                Prenom = sanitizedPrenom,
                RoleSysteme = "PROF",
                CreatedAt = DateTime.UtcNow,
                IsConfirmed = false,
                AuthProvider = null,
                ConfirmationToken = confirmationToken,
                ConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.Utilisateurs.Add(user);
            await _context.SaveChangesAsync();

            // Envoyer le mail de confirmation
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
            var confirmUrl  = $"{frontendUrl}/confirm-email?token={confirmationToken}";
            try
            {
                await _emailService.SendConfirmationEmailAsync(user.Email, user.Prenom ?? "Utilisateur", confirmUrl);
            }
            catch
            {
                // L'envoi du mail a échoué mais le compte est créé : on laisse passer sans bloquer
            }

            return Ok(new { message = "Compte créé ! Vérifiez votre email pour confirmer votre inscription." });
        }

        // ===== CONFIRMATION EMAIL =====
        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token manquant.");

            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.ConfirmationToken == token);

            if (user == null)
                return BadRequest("Token invalide.");

            if (user.IsConfirmed)
                return Ok(new { message = "Compte déjà confirmé." });

            if (user.ConfirmationTokenExpiresAt < DateTime.UtcNow)
                return BadRequest("Ce lien de confirmation a expiré. Créez un nouveau compte.");

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.ConfirmationTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Compte confirmé ! Vous pouvez maintenant vous connecter." });
        }

        // ===== TEST EMAIL (à supprimer après test) =====
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
            var testUrl = $"{frontendUrl}/confirm-email?token=TEST_TOKEN_123";
            await _emailService.SendConfirmationEmailAsync("dragx03@gmail.com", "Test", testUrl);
            return Ok(new { message = "Email envoyé !" });
        }

        // ===== GOOGLE OAUTH =====
        [HttpGet("google")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            return await HandleOAuthCallback("Google");
        }

        // ===== MICROSOFT/OUTLOOK OAUTH =====
        [HttpGet("microsoft")]
        public IActionResult MicrosoftLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(MicrosoftCallback)),
                Items = { { "scheme", MicrosoftAccountDefaults.AuthenticationScheme } }
            };
            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }

        [HttpGet("microsoft-callback")]
        public async Task<IActionResult> MicrosoftCallback()
        {
            return await HandleOAuthCallback("Microsoft");
        }

        private async Task<IActionResult> HandleOAuthCallback(string provider)
        {
            // Les claims sont stockees dans le cookie par le middleware OAuth
            var authenticateResult = await HttpContext.AuthenticateAsync(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                return Redirect("/auth-callback?error=oauth_failed");
            }

            var claims = authenticateResult.Principal?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var nom = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";
            var prenom = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";

            if (string.IsNullOrEmpty(email))
            {
                return Redirect("/auth-callback?error=no_email");
            }

            // Chercher ou creer l'utilisateur
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Creer un nouveau compte
                user = new Utilisateur
                {
                    Email = email.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Nom = InputValidator.SanitizeInput(nom),
                    Prenom = InputValidator.SanitizeInput(prenom),
                    RoleSysteme = "PROF",
                    CreatedAt = DateTime.UtcNow,
                    IsConfirmed = true,
                    AuthProvider = provider
                };

                _context.Utilisateurs.Add(user);
                await _context.SaveChangesAsync();

                try { await _emailService.SendWelcomeEmailAsync(user.Email, user.Prenom ?? ""); }
                catch { /* envoi non bloquant */ }
            }
            else if (user.AuthProvider == null)
            {
                // L'utilisateur existait deja avec email/password, on lie le provider
                user.AuthProvider = provider;
                await _context.SaveChangesAsync();
            }

            var jwt = GenerateToken(user);

            // Stocker le token dans un cookie HttpOnly temporaire (5 min)
            // au lieu de le passer dans l'URL (visible dans logs/historique)
            var authPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Token = jwt,
                Email = user.Email,
                Nom = user.Nom ?? "",
                Prenom = user.Prenom ?? ""
            });
            Response.Cookies.Append("auth_pending", authPayload, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(5),
                Path = "/"
            });
            return Redirect("/auth-callback");
        }

        // Echange le cookie temporaire auth_pending contre les données d'auth (JSON)
        // Appele par le client Blazor apres la redirection OAuth
        [HttpGet("exchange")]
        public IActionResult Exchange()
        {
            var authPayload = Request.Cookies["auth_pending"];
            if (string.IsNullOrEmpty(authPayload))
                return NotFound();

            // Supprimer le cookie immédiatement après lecture (usage unique)
            Response.Cookies.Delete("auth_pending", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(authPayload);
                return Ok(data);
            }
            catch
            {
                return BadRequest();
            }
        }

        private string GenerateToken(Utilisateur user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.RoleSysteme)
            };

            var secretKey = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ApplicationException("La clé secrète JWT n'est pas configurée.");
            }
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
    }
}