// Import ASP.NET Core authorization and controller infrastructure
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// Import authentication middleware and OAuth-specific schemes
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
// Import rate limiting support
using Microsoft.AspNetCore.RateLimiting;
// Import Entity Framework Core for async database queries
using Microsoft.EntityFrameworkCore;
// Import JWT token generation libraries
using Microsoft.IdentityModel.Tokens;
// Import project-specific models, services, and validators
using seragenda.Models;
using seragenda.Services;
using seragenda.Validators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// Import BCrypt for password hashing and verification
using BCrypt.Net;

namespace seragenda.Controllers
{
    // Maps all routes in this controller to /api/auth/...
    [Route("api/[controller]")]
    // Marks this class as an API controller (enables automatic model validation, binding source inference, etc.)
    [ApiController]
    // All endpoints in this controller are publicly accessible — no JWT required
    [AllowAnonymous]
    /// <summary>
    /// Handles all authentication-related operations: local login/register,
    /// email confirmation, Google OAuth, and Microsoft OAuth flows.
    /// </summary>
    public class AuthController : ControllerBase
    {
        // Provides access to appsettings.json and environment variables
        private readonly IConfiguration _configuration;
        // Entity Framework database context for querying user records
        private readonly AgendaContext _context;
        // Service responsible for sending transactional emails (confirmation, welcome)
        private readonly IEmailService _emailService;

        /// <summary>
        /// Constructor — injects dependencies via ASP.NET Core DI container.
        /// </summary>
        /// <param name="configuration">App configuration (JWT secrets, SMTP settings, etc.)</param>
        /// <param name="context">EF Core database context</param>
        /// <param name="emailService">Email sending service</param>
        public AuthController(IConfiguration configuration, AgendaContext context, IEmailService emailService)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        // POST /api/auth/login
        // Rate-limited to 5 requests per 15 minutes per IP to prevent brute-force attacks
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        /// <summary>
        /// Authenticates a user with email and password.
        /// Returns a JWT token and basic user info on success.
        /// </summary>
        /// <param name="loginDto">DTO containing the user's email and password</param>
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Reject if either required field is missing
            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest("L'email et le mot de passe sont requis.");
            }

            // Validate the email format using the custom validator
            if (!InputValidator.IsValidEmail(loginDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Block inputs that contain SQL injection or XSS payloads
            if (InputValidator.ContainsDangerousCharacters(loginDto.Email) ||
                InputValidator.ContainsDangerousCharacters(loginDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Look up the user by email address (case-sensitive match stored lowercased at register time)
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // Return the same generic error for both "user not found" and "wrong password"
            // to avoid leaking whether the email exists in the system
            if (user == null)
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Verify the submitted plaintext password against the stored BCrypt hash
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Prevent login for accounts that have not yet confirmed their email address
            if (!user.IsConfirmed)
            {
                return Unauthorized("Veuillez confirmer votre email avant de vous connecter.");
            }

            // Generate a signed JWT token valid for 7 days
            var jwt = GenerateToken(user);

            // Return the token together with basic profile information the client needs immediately
            return Ok(new {
                Token = jwt,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom
            });
        }

        // POST /api/auth/register
        // Rate-limited to 5 requests per 15 minutes per IP to prevent account farming
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        /// <summary>
        /// Creates a new local user account.
        /// The account is inactive until the email confirmation link is clicked.
        /// </summary>
        /// <param name="registerDto">DTO with email, password, confirm password, first name, and last name</param>
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // All four fields are mandatory for registration
            if (string.IsNullOrEmpty(registerDto.Email) ||
                string.IsNullOrEmpty(registerDto.Password) ||
                string.IsNullOrEmpty(registerDto.Nom) ||
                string.IsNullOrEmpty(registerDto.Prenom))
            {
                return BadRequest("Tous les champs sont requis.");
            }

            // Validate the email format (must match user@domain.tld pattern)
            if (!InputValidator.IsValidEmail(registerDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Validate the last name: only letters, spaces, hyphens, and apostrophes; max 50 chars
            if (!InputValidator.IsValidName(registerDto.Nom))
            {
                return BadRequest("Le nom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            // Validate the first name with the same rules as the last name
            if (!InputValidator.IsValidName(registerDto.Prenom))
            {
                return BadRequest("Le prénom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            // Validate the password length: between 6 and 100 characters
            if (!InputValidator.IsValidPassword(registerDto.Password))
            {
                return BadRequest("Le mot de passe doit contenir entre 6 et 100 caractères.");
            }

            // Block any dangerous characters across all string inputs
            if (InputValidator.ContainsDangerousCharacters(registerDto.Email) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Nom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Prenom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Make sure the password and its confirmation match before continuing
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return BadRequest("Les mots de passe ne correspondent pas.");
            }

            // Check for a pre-existing account with the same email address
            var existingUser = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                // Return a 400 rather than 409 to keep the error wording consistent with other validations
                return BadRequest("Un compte avec cet email existe déjà.");
            }

            // Escape HTML special characters in the names before storing them in the database
            var sanitizedNom = InputValidator.SanitizeInput(registerDto.Nom);
            var sanitizedPrenom = InputValidator.SanitizeInput(registerDto.Prenom);

            // Generate a cryptographically random confirmation token by combining two GUIDs (64 hex chars)
            var confirmationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // Build the new user record; the account is marked unconfirmed until the email link is clicked
            var user = new Utilisateur
            {
                // Store the email in lower case to ensure case-insensitive uniqueness
                Email = registerDto.Email.Trim().ToLower(),
                // Hash the password with BCrypt (work factor = default ~10 rounds)
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Nom = sanitizedNom,
                Prenom = sanitizedPrenom,
                // New users are registered as regular teachers by default
                RoleSysteme = "PROF",
                CreatedAt = DateTime.UtcNow,
                // Account is locked until email confirmation
                IsConfirmed = false,
                // No OAuth provider for local accounts
                AuthProvider = null,
                ConfirmationToken = confirmationToken,
                // Token expires after 24 hours
                ConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Persist the new user record
            _context.Utilisateurs.Add(user);
            await _context.SaveChangesAsync();

            // Build the confirmation URL pointing to the frontend page that handles confirmation
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
            var confirmUrl  = $"{frontendUrl}/confirm-email?token={confirmationToken}";
            try
            {
                // Send the confirmation email; failure here must not block account creation
                await _emailService.SendConfirmationEmailAsync(user.Email, user.Prenom ?? "Utilisateur", confirmUrl);
            }
            catch
            {
                // Swallow the exception — the account has already been created.
                // The user can request a new confirmation email separately.
            }

            return Ok(new { message = "Compte créé ! Vérifiez votre email pour confirmer votre inscription." });
        }

        // GET /api/auth/confirm?token=...
        // Publicly accessible — the link is sent by email and must work without authentication
        [HttpGet("confirm")]
        /// <summary>
        /// Confirms a user's email address using a one-time token sent by email.
        /// Activates the account so the user can log in.
        /// </summary>
        /// <param name="token">The confirmation token from the email link query string</param>
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            // A missing token is definitely invalid — reject immediately
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token manquant.");

            // Find the user that owns this exact confirmation token
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.ConfirmationToken == token);

            // If no user matches, the token is either forged or already cleared
            if (user == null)
                return BadRequest("Token invalide.");

            // If the account was already confirmed, just return success (idempotent)
            if (user.IsConfirmed)
                return Ok(new { message = "Compte déjà confirmé." });

            // Reject tokens that have passed their 24-hour expiry window
            if (user.ConfirmationTokenExpiresAt < DateTime.UtcNow)
                return BadRequest("Ce lien de confirmation a expiré. Créez un nouveau compte.");

            // Activate the account and clear the token so it cannot be reused
            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.ConfirmationTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Compte confirmé ! Vous pouvez maintenant vous connecter." });
        }

        // GET /api/auth/test-email
        // Development/debugging endpoint — should be removed or restricted before going to production
        [HttpGet("test-email")]
        /// <summary>
        /// Sends a test confirmation email to a hardcoded address for SMTP smoke-testing.
        /// Remove or protect this endpoint before deploying to production.
        /// </summary>
        public async Task<IActionResult> TestEmail()
        {
            // Read the frontend URL from configuration (same as in the real flow)
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
            // Build a fake confirmation URL using a placeholder token
            var testUrl = $"{frontendUrl}/confirm-email?token=TEST_TOKEN_123";
            // Send the email to the developer's test address
            await _emailService.SendConfirmationEmailAsync("dragx03@gmail.com", "Test", testUrl);
            return Ok(new { message = "Email envoyé !" });
        }

        // GET /api/auth/google
        // Initiates the Google OAuth 2.0 authorization code flow
        [HttpGet("google")]
        /// <summary>
        /// Redirects the browser to Google's OAuth consent screen.
        /// After the user grants permission, Google redirects back to the callback endpoint.
        /// </summary>
        public IActionResult GoogleLogin()
        {
            // Set the redirect URI so ASP.NET knows where to handle the OAuth callback
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
                // Store the provider name so the callback handler knows which scheme was used
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };
            // Issue a Challenge response — this triggers the Google redirect
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET /api/auth/google-callback
        // Google redirects here after the user has authenticated on their consent screen
        [HttpGet("google-callback")]
        /// <summary>
        /// Handles the OAuth callback from Google.
        /// Delegates to the shared OAuth handler to find or create the local user account.
        /// </summary>
        public async Task<IActionResult> GoogleCallback()
        {
            // Share the callback logic with the Microsoft flow via a common private method
            return await HandleOAuthCallback("Google");
        }

        // GET /api/auth/microsoft
        // Initiates the Microsoft Account / Outlook OAuth 2.0 authorization code flow
        [HttpGet("microsoft")]
        /// <summary>
        /// Redirects the browser to Microsoft's OAuth consent screen.
        /// </summary>
        public IActionResult MicrosoftLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(MicrosoftCallback)),
                Items = { { "scheme", MicrosoftAccountDefaults.AuthenticationScheme } }
            };
            // Trigger the Microsoft OAuth challenge
            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }

        // GET /api/auth/microsoft-callback
        // Microsoft redirects here after the user has authenticated
        [HttpGet("microsoft-callback")]
        /// <summary>
        /// Handles the OAuth callback from Microsoft.
        /// </summary>
        public async Task<IActionResult> MicrosoftCallback()
        {
            return await HandleOAuthCallback("Microsoft");
        }

        /// <summary>
        /// Shared handler for OAuth callbacks from any supported provider.
        /// Reads claims from the cookie set by the OAuth middleware, finds or creates
        /// the matching local user account, generates a JWT, and stores it in a
        /// short-lived HttpOnly cookie for the client to exchange.
        /// </summary>
        /// <param name="provider">The name of the OAuth provider ("Google" or "Microsoft")</param>
        private async Task<IActionResult> HandleOAuthCallback(string provider)
        {
            // The OAuth middleware stores the authenticated identity in the cookie scheme
            // after the external provider redirects back; attempt to read it
            var authenticateResult = await HttpContext.AuthenticateAsync(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            // If authentication failed (e.g., user denied permission), redirect to the frontend error page
            if (!authenticateResult.Succeeded)
            {
                return Redirect("/auth-callback?error=oauth_failed");
            }

            // Extract the standard OpenID claims from the authenticated principal
            var claims = authenticateResult.Principal?.Claims;
            var email  = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var nom    = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";
            var prenom = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";

            // An email address is required to identify the user; reject if missing
            if (string.IsNullOrEmpty(email))
            {
                return Redirect("/auth-callback?error=no_email");
            }

            // Try to find an existing account with the same email address
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // No existing account — create a new one automatically (social sign-in)
                user = new Utilisateur
                {
                    Email = email.Trim().ToLower(),
                    // Generate a random unusable password hash since the user will authenticate via OAuth
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    // Sanitize names received from the external provider
                    Nom    = InputValidator.SanitizeInput(nom),
                    Prenom = InputValidator.SanitizeInput(prenom),
                    RoleSysteme = "PROF",
                    CreatedAt   = DateTime.UtcNow,
                    // OAuth accounts are considered confirmed immediately (provider already verified the email)
                    IsConfirmed  = true,
                    AuthProvider = provider
                };

                _context.Utilisateurs.Add(user);
                await _context.SaveChangesAsync();

                // Send a welcome email; failure must not break the login flow
                try { await _emailService.SendWelcomeEmailAsync(user.Email, user.Prenom ?? ""); }
                catch { /* Non-blocking: email failure does not roll back account creation */ }
            }
            else if (user.AuthProvider == null)
            {
                // The user already has a local email/password account;
                // link the OAuth provider to it so both methods work going forward
                user.AuthProvider = provider;
                await _context.SaveChangesAsync();
            }

            // Issue a JWT for the authenticated (or newly created) user
            var jwt = GenerateToken(user);

            // Serialize the auth payload as JSON so it can be stored in the cookie
            var authPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Token  = jwt,
                Email  = user.Email,
                Nom    = user.Nom    ?? "",
                Prenom = user.Prenom ?? ""
            });

            // Store the JWT in an HttpOnly cookie valid for 5 minutes.
            // This avoids putting the token in the URL (visible in browser history / server logs).
            // The Blazor client will call /api/auth/exchange to retrieve and clear it immediately.
            Response.Cookies.Append("auth_pending", authPayload, new CookieOptions
            {
                HttpOnly   = true,       // Not accessible from JavaScript — prevents XSS theft
                Secure     = true,       // Only sent over HTTPS
                SameSite   = SameSiteMode.Lax, // Lax allows the cookie to survive the external redirect
                MaxAge     = TimeSpan.FromMinutes(5), // Short-lived: exchange must happen quickly
                Path       = "/"
            });

            // Redirect the browser to the Blazor page that will call /exchange to collect the token
            return Redirect("/auth-callback");
        }

        // GET /api/auth/exchange
        // Called by the Blazor front-end immediately after the /auth-callback redirect
        [HttpGet("exchange")]
        /// <summary>
        /// Exchanges the short-lived HttpOnly auth_pending cookie for the JSON auth payload.
        /// The cookie is deleted immediately after reading so it can only be used once.
        /// </summary>
        public IActionResult Exchange()
        {
            // Read the temporary cookie that was set during the OAuth callback
            var authPayload = Request.Cookies["auth_pending"];

            // If the cookie is missing, the exchange window has expired or this is an invalid request
            if (string.IsNullOrEmpty(authPayload))
                return NotFound();

            // Immediately invalidate the cookie after reading to enforce single-use semantics
            Response.Cookies.Delete("auth_pending", new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Path     = "/"
            });

            try
            {
                // Deserialize the stored JSON payload and return it as the response body
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(authPayload);
                return Ok(data);
            }
            catch
            {
                // If the stored cookie value is malformed, reject it
                return BadRequest();
            }
        }

        /// <summary>
        /// Generates a signed JWT token for the given user.
        /// The token contains the user's email (as Name claim) and system role.
        /// It is valid for 7 days from the moment of issue.
        /// </summary>
        /// <param name="user">The authenticated user entity</param>
        /// <returns>A signed JWT string</returns>
        private string GenerateToken(Utilisateur user)
        {
            // Build the claims that will be embedded inside the token payload
            var claims = new List<Claim>
            {
                // The Name claim stores the email — used throughout the app to identify the current user
                new Claim(ClaimTypes.Name, user.Email),
                // The Role claim is used by [Authorize(Roles = "ADMIN")] etc.
                new Claim(ClaimTypes.Role, user.RoleSysteme)
            };

            // Read the JWT secret key from configuration
            var secretKey = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                // A missing secret key is a configuration error — fail loudly
                throw new ApplicationException("La clé secrète JWT n'est pas configurée.");
            }

            // Convert the secret key string to bytes for the symmetric signing algorithm
            var key   = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            // Use HMAC-SHA256 as the signing algorithm
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Describe the token: who it belongs to, when it expires, and how it is signed
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                SigningCredentials = creds
            };

            // Create and serialize the JWT to a compact string
            var tokenHandler = new JwtSecurityTokenHandler();
            var token        = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    /// <summary>
    /// Data Transfer Object for the login endpoint.
    /// Contains only the fields needed to authenticate an existing user.
    /// </summary>
    public class LoginDto
    {
        // The user's registered email address
        public string? Email { get; set; }
        // The user's plaintext password (never stored; compared against the hash)
        public string? Password { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for the register endpoint.
    /// Contains all fields required to create a new local user account.
    /// </summary>
    public class RegisterDto
    {
        // The email address that will be used as the login identifier
        public string? Email { get; set; }
        // The desired password in plaintext (will be BCrypt-hashed before storage)
        public string? Password { get; set; }
        // Must match Password exactly; validated server-side to prevent typos
        public string? ConfirmPassword { get; set; }
        // User's family name
        public string? Nom { get; set; }
        // User's given name
        public string? Prenom { get; set; }
    }
}
