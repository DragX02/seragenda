// Importation des attributs d'autorisation et de l'infrastructure de contrôleur ASP.NET Core
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// Importation des middlewares d'authentification et des schémas OAuth spécifiques
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
// Importation du support de limitation de débit
using Microsoft.AspNetCore.RateLimiting;
// Importation d'Entity Framework Core pour les requêtes asynchrones en base de données
using Microsoft.EntityFrameworkCore;
// Importation des bibliothèques de génération de jetons JWT
using Microsoft.IdentityModel.Tokens;
// Importation des modèles, services et validateurs spécifiques au projet
using seragenda.Models;
using seragenda.Services;
using seragenda.Validators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// Importation de BCrypt pour le hachage et la vérification des mots de passe
using BCrypt.Net;

namespace seragenda.Controllers
{
    // Associe toutes les routes de ce contrôleur à /api/auth/...
    [Route("api/[controller]")]
    // Marque cette classe comme contrôleur API (active la validation automatique du modèle, l'inférence de source de liaison, etc.)
    [ApiController]
    // Tous les points de terminaison de ce contrôleur sont accessibles publiquement — aucun JWT requis
    [AllowAnonymous]
    // Gère toutes les opérations liées à l'authentification : connexion/inscription locale,
    // confirmation par email, OAuth Google et OAuth Microsoft.
    public class AuthController : ControllerBase
    {
        // Fournit l'accès à appsettings.json et aux variables d'environnement
        private readonly IConfiguration _configuration;
        // Contexte de base de données Entity Framework pour interroger les enregistrements utilisateur
        private readonly AgendaContext _context;
        // Service responsable de l'envoi des emails transactionnels (confirmation, bienvenue)
        private readonly IEmailService _emailService;

        // Constructeur — injecte les dépendances via le conteneur DI d'ASP.NET Core.
        // configuration : configuration de l'application (secrets JWT, paramètres SMTP, etc.)
        // context : contexte de base de données EF Core
        // emailService : service d'envoi d'emails
        public AuthController(IConfiguration configuration, AgendaContext context, IEmailService emailService)
        {
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        // POST /api/auth/login
        // Limité à 5 requêtes par 15 minutes par IP pour prévenir les attaques par force brute
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        // Authentifie un utilisateur avec son email et son mot de passe.
        // Retourne un jeton JWT et des informations de base sur l'utilisateur en cas de succès.
        // loginDto : DTO contenant l'email et le mot de passe de l'utilisateur
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Rejet si l'un des champs obligatoires est manquant
            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest("L'email et le mot de passe sont requis.");
            }

            // Validation du format de l'email avec le validateur personnalisé
            if (!InputValidator.IsValidEmail(loginDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Blocage des entrées contenant des charges utiles d'injection SQL ou XSS
            if (InputValidator.ContainsDangerousCharacters(loginDto.Email) ||
                InputValidator.ContainsDangerousCharacters(loginDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Recherche de l'utilisateur par adresse email (correspondance sensible à la casse, stockée en minuscules à l'inscription)
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // Retourne la même erreur générique pour "utilisateur introuvable" et "mauvais mot de passe"
            // afin d'éviter de révéler si l'email existe dans le système
            if (user == null)
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Vérification du mot de passe en clair soumis par rapport au hash BCrypt stocké
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Empêche la connexion pour les comptes n'ayant pas encore confirmé leur adresse email
            if (!user.IsConfirmed)
            {
                return Unauthorized("Veuillez confirmer votre email avant de vous connecter.");
            }

            // Génération d'un jeton JWT signé valable 7 jours
            var jwt = GenerateToken(user);

            // Retourne le jeton ainsi que les informations de profil de base dont le client a immédiatement besoin
            return Ok(new {
                Token = jwt,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom
            });
        }

        // POST /api/auth/register
        // Limité à 5 requêtes par 15 minutes par IP pour prévenir la création massive de comptes
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        // Crée un nouveau compte utilisateur local.
        // Le compte est inactif jusqu'au clic sur le lien de confirmation par email.
        // registerDto : DTO contenant l'email, le mot de passe, la confirmation, le prénom et le nom
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Les quatre champs sont obligatoires pour l'inscription
            if (string.IsNullOrEmpty(registerDto.Email) ||
                string.IsNullOrEmpty(registerDto.Password) ||
                string.IsNullOrEmpty(registerDto.Nom) ||
                string.IsNullOrEmpty(registerDto.Prenom))
            {
                return BadRequest("Tous les champs sont requis.");
            }

            // Validation du format de l'email (doit correspondre au motif utilisateur@domaine.tld)
            if (!InputValidator.IsValidEmail(registerDto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Validation du nom de famille : uniquement lettres, espaces, tirets et apostrophes ; max 50 caractères
            if (!InputValidator.IsValidName(registerDto.Nom))
            {
                return BadRequest("Le nom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            // Validation du prénom avec les mêmes règles que le nom de famille
            if (!InputValidator.IsValidName(registerDto.Prenom))
            {
                return BadRequest("Le prénom contient des caractères non autorisés ou est trop long (max 50 caractères).");
            }

            // Validation de la longueur du mot de passe : entre 6 et 100 caractères
            if (!InputValidator.IsValidPassword(registerDto.Password))
            {
                return BadRequest("Le mot de passe doit contenir entre 6 et 100 caractères.");
            }

            // Blocage de tout caractère dangereux dans tous les champs de type chaîne
            if (InputValidator.ContainsDangerousCharacters(registerDto.Email) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Nom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Prenom) ||
                InputValidator.ContainsDangerousCharacters(registerDto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Vérification que le mot de passe et sa confirmation correspondent avant de continuer
            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                return BadRequest("Les mots de passe ne correspondent pas.");
            }

            // Vérification de l'existence d'un compte avec la même adresse email
            var existingUser = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                // Retourne un 400 plutôt qu'un 409 pour garder un libellé d'erreur cohérent avec les autres validations
                return BadRequest("Un compte avec cet email existe déjà.");
            }

            // Échappement des caractères spéciaux HTML dans les noms avant de les stocker en base de données
            var sanitizedNom = InputValidator.SanitizeInput(registerDto.Nom);
            var sanitizedPrenom = InputValidator.SanitizeInput(registerDto.Prenom);

            // Génération d'un jeton de confirmation aléatoire cryptographiquement sécurisé en combinant deux GUID (64 caractères hexadécimaux)
            var confirmationToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // Construction du nouvel enregistrement utilisateur ; le compte est marqué non confirmé jusqu'au clic sur le lien email
            var user = new Utilisateur
            {
                // Stockage de l'email en minuscules pour garantir l'unicité insensible à la casse
                Email = registerDto.Email.Trim().ToLower(),
                // Hachage du mot de passe avec BCrypt (facteur de travail = ~10 tours par défaut)
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Nom = sanitizedNom,
                Prenom = sanitizedPrenom,
                // Les nouveaux utilisateurs sont inscrits comme professeurs ordinaires par défaut
                RoleSysteme = "PROF",
                CreatedAt = DateTime.UtcNow,
                // Le compte est bloqué jusqu'à la confirmation par email
                IsConfirmed = false,
                // Pas de fournisseur OAuth pour les comptes locaux
                AuthProvider = null,
                ConfirmationToken = confirmationToken,
                // Le jeton expire après 24 heures
                ConfirmationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Persistance du nouvel enregistrement utilisateur
            _context.Utilisateurs.Add(user);
            await _context.SaveChangesAsync();

            // Construction de l'URL de confirmation pointant vers la page frontend qui gère la confirmation
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.duckdns.org:8586";
            var confirmUrl  = $"{frontendUrl}/confirm-email?token={confirmationToken}";
            try
            {
                // Envoi de l'email de confirmation ; l'échec ici ne doit pas bloquer la création du compte
                await _emailService.SendConfirmationEmailAsync(user.Email, user.Prenom ?? "Utilisateur", confirmUrl);
            }
            catch
            {
                // Exception ignorée — le compte a déjà été créé.
                // L'utilisateur peut demander un nouvel email de confirmation séparément.
            }

            return Ok(new { message = "Compte créé ! Vérifiez votre email pour confirmer votre inscription." });
        }

        // GET /api/auth/confirm?token=...
        // Accessible publiquement — le lien est envoyé par email et doit fonctionner sans authentification
        [HttpGet("confirm")]
        // Confirme l'adresse email d'un utilisateur à l'aide d'un jeton à usage unique envoyé par email.
        // Active le compte afin que l'utilisateur puisse se connecter.
        // token : le jeton de confirmation extrait de la chaîne de requête du lien email
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            // Un jeton manquant est définitivement invalide — rejet immédiat
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token manquant.");

            // Recherche de l'utilisateur propriétaire de ce jeton de confirmation exact
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.ConfirmationToken == token);

            // Si aucun utilisateur ne correspond, le jeton est falsifié ou déjà effacé
            if (user == null)
                return BadRequest("Token invalide.");

            // Si le compte était déjà confirmé, retourner simplement un succès (idempotent)
            if (user.IsConfirmed)
                return Ok(new { message = "Compte déjà confirmé." });

            // Rejet des jetons ayant dépassé leur fenêtre d'expiration de 24 heures
            if (user.ConfirmationTokenExpiresAt < DateTime.UtcNow)
                return BadRequest("Ce lien de confirmation a expiré. Créez un nouveau compte.");

            // Activation du compte et effacement du jeton pour qu'il ne puisse pas être réutilisé
            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            user.ConfirmationTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Compte confirmé ! Vous pouvez maintenant vous connecter." });
        }

        // POST /api/auth/forgot-password
        // Limité à 5 requêtes par 15 minutes par IP : empêche d'utiliser ce point de terminaison
        // pour spammer une boîte mail ou sonder les adresses enregistrées
        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        // Démarre la procédure "mot de passe oublié" : génère un jeton de réinitialisation à usage
        // unique valable 1 heure et l'envoie par e-mail à l'adresse enregistrée sur le compte.
        // La réponse est toujours identique (succès générique), que l'adresse existe ou non,
        // afin de ne pas révéler quels e-mails sont inscrits (énumération de comptes).
        // dto : DTO contenant l'adresse e-mail du compte à récupérer
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            // Rejet si l'adresse est absente
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest("L'email est requis.");
            }

            // Validation du format de l'email avec le validateur personnalisé
            if (!InputValidator.IsValidEmail(dto.Email))
            {
                return BadRequest("Format d'email invalide.");
            }

            // Blocage des entrées contenant des charges utiles d'injection SQL ou XSS
            if (InputValidator.ContainsDangerousCharacters(dto.Email))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Normalisation de l'adresse : les emails sont stockés en minuscules à l'inscription
            var email = dto.Email.Trim().ToLower();
            var user  = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);

            // Si un compte correspond, on génère le jeton et on envoie l'e-mail.
            // Sinon on ne fait rien — mais la réponse renvoyée reste la même (voir plus bas).
            if (user != null)
            {
                // Jeton aléatoire de 64 caractères hexadécimaux (deux GUID concaténés), même format
                // que le jeton de confirmation d'inscription
                var resetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

                // Enregistrement du jeton et de son expiration : fenêtre courte de 1 heure
                // car ce jeton donne le contrôle du compte
                user.ResetToken          = resetToken;
                user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                // Construction du lien vers la page frontend qui affiche le formulaire de nouveau mot de passe
                var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.duckdns.org:8586";
                var resetUrl    = $"{frontendUrl}/reset-password?token={resetToken}";

                try
                {
                    // Envoi de l'e-mail à l'adresse enregistrée sur le compte (jamais à une adresse fournie autrement)
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.Prenom ?? "Utilisateur", resetUrl);
                }
                catch
                {
                    // Exception ignorée : l'échec SMTP est déjà journalisé par EmailService et
                    // ne doit pas révéler au client que l'adresse existe. L'utilisateur peut relancer la demande.
                }
            }

            // Réponse volontairement identique dans tous les cas (compte trouvé, inconnu ou e-mail en échec)
            return Ok(new { message = "Si un compte existe avec cette adresse, un e-mail de réinitialisation vient d'être envoyé." });
        }

        // GET /api/auth/validate-reset-token?token=...
        // Accessible publiquement — appelé par la page frontend au chargement du lien reçu par e-mail
        [HttpGet("validate-reset-token")]
        // Vérifie qu'un jeton de réinitialisation existe encore et n'est pas expiré, sans rien modifier.
        // Permet à la page frontend d'afficher immédiatement "lien expiré" plutôt que de laisser
        // l'utilisateur saisir un mot de passe pour rien.
        // token : le jeton extrait de la chaîne de requête du lien e-mail
        public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
        {
            // Un jeton manquant est définitivement invalide — rejet immédiat
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token manquant." });

            // Recherche du compte portant exactement ce jeton de réinitialisation
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.ResetToken == token);

            // Aucun compte : jeton falsifié, déjà utilisé ou remplacé par une demande plus récente
            if (user == null || user.ResetTokenExpiresAt == null)
                return BadRequest(new { message = "Ce lien de réinitialisation est invalide ou a déjà été utilisé." });

            // Rejet des jetons dont la fenêtre de 1 heure est dépassée
            if (user.ResetTokenExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "Ce lien de réinitialisation a expiré. Demandez-en un nouveau." });

            // Jeton exploitable : la page peut afficher le formulaire de nouveau mot de passe
            return Ok(new { valid = true, email = user.Email });
        }

        // POST /api/auth/reset-password
        // Limité à 5 requêtes par 15 minutes par IP pour empêcher le forçage brutal des jetons
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        // Termine la procédure "mot de passe oublié" : valide le jeton reçu par e-mail puis
        // remplace le hash du mot de passe par celui du nouveau mot de passe choisi.
        // Le jeton est effacé immédiatement après usage (usage unique).
        // dto : DTO contenant le jeton, le nouveau mot de passe et sa confirmation
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            // Les trois champs sont obligatoires
            if (string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrEmpty(dto.Password) ||
                string.IsNullOrEmpty(dto.ConfirmPassword))
            {
                return BadRequest("Token et mot de passe sont requis.");
            }

            // Validation de la longueur du mot de passe : entre 6 et 100 caractères (même règle qu'à l'inscription)
            if (!InputValidator.IsValidPassword(dto.Password))
            {
                return BadRequest("Le mot de passe doit contenir entre 6 et 100 caractères.");
            }

            // Blocage des charges utiles d'injection SQL ou XSS dans les champs texte
            if (InputValidator.ContainsDangerousCharacters(dto.Token) ||
                InputValidator.ContainsDangerousCharacters(dto.Password))
            {
                return BadRequest("Caractères non autorisés détectés.");
            }

            // Vérification que le mot de passe et sa confirmation correspondent
            if (dto.Password != dto.ConfirmPassword)
            {
                return BadRequest("Les mots de passe ne correspondent pas.");
            }

            // Recherche du compte portant exactement ce jeton de réinitialisation
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.ResetToken == dto.Token);

            // Aucun compte : jeton falsifié, déjà utilisé ou remplacé par une demande plus récente
            if (user == null || user.ResetTokenExpiresAt == null)
            {
                return BadRequest("Ce lien de réinitialisation est invalide ou a déjà été utilisé.");
            }

            // Rejet des jetons dont la fenêtre de 1 heure est dépassée
            if (user.ResetTokenExpiresAt < DateTime.UtcNow)
            {
                return BadRequest("Ce lien de réinitialisation a expiré. Demandez-en un nouveau.");
            }

            // Remplacement du hash par celui du nouveau mot de passe (BCrypt, facteur de travail par défaut)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Effacement du jeton pour qu'il ne puisse pas servir une seconde fois
            user.ResetToken          = null;
            user.ResetTokenExpiresAt = null;

            // Le clic sur le lien prouve que l'utilisateur contrôle la boîte mail : un compte local
            // resté non confirmé est donc validé ici, sinon la connexion resterait bloquée après
            // la réinitialisation. Le jeton d'inscription devenu inutile est effacé.
            if (!user.IsConfirmed)
            {
                user.IsConfirmed                = true;
                user.ConfirmationToken          = null;
                user.ConfirmationTokenExpiresAt = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mot de passe modifié ! Vous pouvez maintenant vous connecter." });
        }

        // GET /api/auth/test-email
        // Point de terminaison de développement/débogage — à supprimer ou restreindre avant la mise en production
        [HttpGet("test-email")]
        // Envoie un email de confirmation de test à une adresse codée en dur pour tester la configuration SMTP.
        // Supprimer ou protéger ce point de terminaison avant le déploiement en production.
        public async Task<IActionResult> TestEmail()
        {
            // Lecture de l'URL frontend depuis la configuration (identique au flux réel)
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://obrigenie.duckdns.org:8586";
            // Construction d'une URL de confirmation factice avec un jeton fictif
            var testUrl = $"{frontendUrl}/confirm-email?token=TEST_TOKEN_123";
            // Envoi de l'email à l'adresse de test du développeur
            await _emailService.SendConfirmationEmailAsync("dragx03@gmail.com", "Test", testUrl);
            return Ok(new { message = "Email envoyé !" });
        }

        // GET /api/auth/google
        // Initie le flux de code d'autorisation OAuth 2.0 de Google
        [HttpGet("google")]
        // Redirige le navigateur vers l'écran de consentement OAuth de Google.
        // Après que l'utilisateur a accordé la permission, Google redirige vers le point de terminaison de rappel.
        public IActionResult GoogleLogin()
        {
            // Définition de l'URI de redirection pour qu'ASP.NET sache où gérer le rappel OAuth
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)),
                // Stockage du nom du fournisseur pour que le gestionnaire de rappel sache quel schéma a été utilisé
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };
            // Émission d'une réponse Challenge — cela déclenche la redirection vers Google
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET /api/auth/google-callback
        // Google redirige ici après que l'utilisateur s'est authentifié sur son écran de consentement
        [HttpGet("google-callback")]
        // Gère le rappel OAuth en provenance de Google.
        // Délègue au gestionnaire OAuth partagé pour trouver ou créer le compte utilisateur local.
        public async Task<IActionResult> GoogleCallback()
        {
            // Partage de la logique de rappel avec le flux Microsoft via une méthode privée commune
            return await HandleOAuthCallback("Google");
        }

        // GET /api/auth/microsoft
        // Initie le flux de code d'autorisation OAuth 2.0 de Microsoft Account / Outlook
        [HttpGet("microsoft")]
        // Redirige le navigateur vers l'écran de consentement OAuth de Microsoft.
        public IActionResult MicrosoftLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(MicrosoftCallback)),
                Items = { { "scheme", MicrosoftAccountDefaults.AuthenticationScheme } }
            };
            // Déclenchement du challenge OAuth Microsoft
            return Challenge(properties, MicrosoftAccountDefaults.AuthenticationScheme);
        }

        // GET /api/auth/microsoft-callback
        // Microsoft redirige ici après que l'utilisateur s'est authentifié
        [HttpGet("microsoft-callback")]
        // Gère le rappel OAuth en provenance de Microsoft.
        public async Task<IActionResult> MicrosoftCallback()
        {
            return await HandleOAuthCallback("Microsoft");
        }

        // Gestionnaire partagé pour les rappels OAuth de tout fournisseur supporté.
        // Lit les claims depuis le cookie défini par le middleware OAuth, trouve ou crée
        // le compte utilisateur local correspondant, génère un JWT et le stocke dans un
        // cookie HttpOnly de courte durée pour que le client puisse l'échanger.
        // provider : le nom du fournisseur OAuth ("Google" ou "Microsoft")
        private async Task<IActionResult> HandleOAuthCallback(string provider)
        {
            // Le middleware OAuth stocke l'identité authentifiée dans le schéma cookie
            // après la redirection du fournisseur externe ; tentative de lecture
            var authenticateResult = await HttpContext.AuthenticateAsync(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            // Si l'authentification a échoué (par ex. l'utilisateur a refusé la permission), redirection vers la page d'erreur frontend
            if (!authenticateResult.Succeeded)
            {
                return Redirect("/auth-callback?error=oauth_failed");
            }

            // Extraction des claims OpenID standard depuis le principal authentifié
            var claims = authenticateResult.Principal?.Claims;
            var email  = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var nom    = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";
            var prenom = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "";

            // Une adresse email est requise pour identifier l'utilisateur ; rejet si absente
            if (string.IsNullOrEmpty(email))
            {
                return Redirect("/auth-callback?error=no_email");
            }

            // Tentative de trouver un compte existant avec la même adresse email
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Aucun compte existant — création automatique d'un nouveau (connexion sociale)
                user = new Utilisateur
                {
                    Email = email.Trim().ToLower(),
                    // Génération d'un hash de mot de passe aléatoire inutilisable car l'utilisateur s'authentifiera via OAuth
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    // Assainissement des noms reçus depuis le fournisseur externe
                    Nom    = InputValidator.SanitizeInput(nom),
                    Prenom = InputValidator.SanitizeInput(prenom),
                    RoleSysteme = "PROF",
                    CreatedAt   = DateTime.UtcNow,
                    // Les comptes OAuth sont considérés comme confirmés immédiatement (le fournisseur a déjà vérifié l'email)
                    IsConfirmed  = true,
                    AuthProvider = provider
                };

                _context.Utilisateurs.Add(user);
                await _context.SaveChangesAsync();

                // Envoi d'un email de bienvenue ; l'échec ne doit pas interrompre le flux de connexion
                try { await _emailService.SendWelcomeEmailAsync(user.Email, user.Prenom ?? ""); }
                catch { /* Non bloquant : l'échec de l'email n'annule pas la création du compte */ }
            }
            else if (user.AuthProvider == null)
            {
                // L'utilisateur possède déjà un compte local email/mot de passe ;
                // liaison du fournisseur OAuth pour que les deux méthodes fonctionnent à l'avenir
                user.AuthProvider = provider;
                await _context.SaveChangesAsync();
            }

            // Émission d'un JWT pour l'utilisateur authentifié (ou nouvellement créé)
            var jwt = GenerateToken(user);

            // Sérialisation du payload d'authentification en JSON pour le stocker dans le cookie
            var authPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Token  = jwt,
                Email  = user.Email,
                Nom    = user.Nom    ?? "",
                Prenom = user.Prenom ?? ""
            });

            // Stockage du JWT dans un cookie HttpOnly valable 5 minutes.
            // Cela évite de mettre le jeton dans l'URL (visible dans l'historique du navigateur / journaux serveur).
            // Le client Blazor appellera /api/auth/exchange pour le récupérer et le supprimer immédiatement.
            Response.Cookies.Append("auth_pending", authPayload, new CookieOptions
            {
                HttpOnly   = true,       // Non accessible depuis JavaScript — prévient le vol par XSS
                Secure     = true,       // Envoyé uniquement via HTTPS
                SameSite   = SameSiteMode.Lax, // Lax permet au cookie de survivre à la redirection externe
                MaxAge     = TimeSpan.FromMinutes(5), // Courte durée de vie : l'échange doit avoir lieu rapidement
                Path       = "/"
            });

            // Redirection du navigateur vers la page Blazor qui appellera /exchange pour récupérer le jeton
            return Redirect("/auth-callback");
        }

        // GET /api/auth/exchange
        // Appelé par le frontend Blazor immédiatement après la redirection /auth-callback
        [HttpGet("exchange")]
        // Échange le cookie temporaire HttpOnly auth_pending contre le payload JSON d'authentification.
        // Le cookie est supprimé immédiatement après lecture afin de n'être utilisable qu'une seule fois.
        public IActionResult Exchange()
        {
            // Lecture du cookie temporaire défini lors du rappel OAuth
            var authPayload = Request.Cookies["auth_pending"];

            // Si le cookie est absent, la fenêtre d'échange a expiré ou la requête est invalide
            if (string.IsNullOrEmpty(authPayload))
                return NotFound();

            // Invalidation immédiate du cookie après lecture pour garantir la sémantique d'usage unique
            Response.Cookies.Delete("auth_pending", new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Path     = "/"
            });

            try
            {
                // Désérialisation du payload JSON stocké et retour dans le corps de la réponse
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(authPayload);
                return Ok(data);
            }
            catch
            {
                // Si la valeur du cookie stocké est malformée, la rejeter
                return BadRequest();
            }
        }

        // Génère un jeton JWT signé pour l'utilisateur donné.
        // Le jeton contient l'email de l'utilisateur (en tant que claim Name) et son rôle système.
        // Il est valide pendant 7 jours à partir du moment de l'émission.
        // user : l'entité utilisateur authentifié
        // Retourne une chaîne JWT signée
        private string GenerateToken(Utilisateur user)
        {
            // Construction des claims qui seront intégrés dans le payload du jeton
            var claims = new List<Claim>
            {
                // Le claim Name stocke l'email — utilisé dans toute l'application pour identifier l'utilisateur courant
                new Claim(ClaimTypes.Name, user.Email),
                // Le claim Role est utilisé par [Authorize(Roles = "ADMIN")] etc.
                new Claim(ClaimTypes.Role, user.RoleSysteme)
            };

            // Lecture de la clé secrète JWT depuis la configuration
            var secretKey = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                // Une clé secrète manquante est une erreur de configuration — échec explicite
                throw new ApplicationException("La clé secrète JWT n'est pas configurée.");
            }

            // Conversion de la chaîne de la clé secrète en octets pour l'algorithme de signature symétrique
            var key   = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
            // Utilisation de HMAC-SHA256 comme algorithme de signature
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Description du jeton : à qui il appartient, quand il expire et comment il est signé
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject            = new ClaimsIdentity(claims),
                Expires            = DateTime.UtcNow.AddDays(7), // Jeton valide pendant 7 jours
                SigningCredentials = creds
            };

            // Création et sérialisation du JWT en chaîne compacte
            var tokenHandler = new JwtSecurityTokenHandler();
            var token        = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // Objet de transfert de données pour le point de terminaison de connexion.
    // Contient uniquement les champs nécessaires pour authentifier un utilisateur existant.
    public class LoginDto
    {
        // Adresse email enregistrée de l'utilisateur
        public string? Email { get; set; }
        // Mot de passe en clair de l'utilisateur (jamais stocké ; comparé au hash)
        public string? Password { get; set; }
    }

    // Objet de transfert de données pour le point de terminaison d'inscription.
    // Contient tous les champs requis pour créer un nouveau compte utilisateur local.
    public class RegisterDto
    {
        // Adresse email qui sera utilisée comme identifiant de connexion
        public string? Email { get; set; }
        // Mot de passe souhaité en clair (sera haché avec BCrypt avant stockage)
        public string? Password { get; set; }
        // Doit correspondre exactement à Password ; validé côté serveur pour éviter les fautes de frappe
        public string? ConfirmPassword { get; set; }
        // Nom de famille de l'utilisateur
        public string? Nom { get; set; }
        // Prénom de l'utilisateur
        public string? Prenom { get; set; }
    }

    // Objet de transfert de données pour le point de terminaison "mot de passe oublié".
    // Ne contient que l'adresse e-mail du compte à récupérer ; le lien de réinitialisation
    // est toujours envoyé à l'adresse enregistrée en base, jamais à une autre.
    public class ForgotPasswordDto
    {
        // Adresse e-mail du compte dont le mot de passe doit être réinitialisé
        public string? Email { get; set; }
    }

    // Objet de transfert de données pour le point de terminaison de réinitialisation du mot de passe.
    // Le jeton provient du lien reçu par e-mail et prouve la propriété de l'adresse.
    public class ResetPasswordDto
    {
        // Jeton de réinitialisation à usage unique extrait de l'URL du lien e-mail
        public string? Token { get; set; }
        // Nouveau mot de passe en clair (sera haché avec BCrypt avant stockage)
        public string? Password { get; set; }
        // Doit correspondre exactement à Password ; validé côté serveur pour éviter les fautes de frappe
        public string? ConfirmPassword { get; set; }
    }
}
