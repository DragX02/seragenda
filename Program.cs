// Import de la protection des données ASP.NET Core pour chiffrer les cookies (corrélation OAuth, auth_pending)
using Microsoft.AspNetCore.DataProtection;
// Import du middleware de limitation de débit et des politiques associées
using Microsoft.AspNetCore.RateLimiting;
// Import d'Entity Framework Core pour l'enregistrement du contexte de base de données
using Microsoft.EntityFrameworkCore;
// Import de l'espace de noms du projet pour AgendaContext
using seragenda;
// Import des implémentations de services du projet
using seragenda.Services;
// Import du gestionnaire d'authentification JWT Bearer
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Import du gestionnaire d'authentification par cookie (utilisé comme schéma de connexion pour OAuth)
using Microsoft.AspNetCore.Authentication.Cookies;
// Import des paramètres de validation des jetons JWT
using Microsoft.IdentityModel.Tokens;
// Import du middleware ForwardedHeaders pour fonctionner derrière nginx/proxy inverse
using Microsoft.AspNetCore.HttpOverrides;
// Import de l'encodage texte pour convertir la clé secrète JWT en octets
using System.Text;
// Import des options de limitation de débit et des partitions
using System.Threading.RateLimiting;

// Activation du comportement d'horodatage hérité pour Npgsql afin que les valeurs DateTime soient traitées comme
// "timestamp without time zone" plutôt que le nouveau type UTC.
// Nécessaire pour maintenir la compatibilité avec le schéma PostgreSQL existant
// où les colonnes timestamp sont stockées sans information de fuseau horaire.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Création du constructeur WebApplication — point de départ de toute la configuration
var builder = WebApplication.CreateBuilder(args);

// Lecture de la clé secrète JWT depuis la configuration (appsettings.json ou variable d'environnement)
var secretkey = builder.Configuration["JwtSettings:SecretKey"];
// Échec immédiat au démarrage si la clé secrète n'est pas configurée — une clé manquante est une erreur de sécurité
if (string.IsNullOrEmpty(secretkey))
{
    throw new Exception("Pas de clef");
}
// Conversion de la clé secrète en tableau d'octets pour l'utiliser dans la clé de signature JWT
var key = Encoding.ASCII.GetBytes(secretkey);

// --- Configuration CORS ---
// Lecture de la liste des origines frontend autorisées depuis la configuration (section Cors:AllowedOrigins)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

// Enregistrement du middleware CORS avec une politique par défaut
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Autoriser uniquement les requêtes provenant des origines configurées (ex. le frontend Blazor WASM)
        policy.WithOrigins(allowedOrigins)
              // Autoriser toute méthode HTTP (GET, POST, PUT, DELETE, etc.)
              .AllowAnyMethod()
              // Autoriser tout en-tête de requête
              .AllowAnyHeader()
              // Autoriser l'envoi de cookies avec les requêtes cross-origin (nécessaire pour l'échange de cookie OAuth)
              .AllowCredentials();
    });
});

// --- Configuration de l'authentification ---
// Configuration du pipeline d'authentification avec trois schémas :
//   1. JWT Bearer — schéma principal pour les requêtes API (valide l'en-tête Authorization: Bearer <token>)
//   2. Cookie — schéma de connexion utilisé pour persister l'identité OAuth entre le callback et l'échange
//   3. OAuth 2.0 Google — fournisseur externe pour la connexion sociale
//   4. OAuth Microsoft Account — fournisseur externe pour la connexion sociale
builder.Services.AddAuthentication(x =>
{
    // Utiliser JWT Bearer comme schéma par défaut pour vérifier les jetons des requêtes API
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // Utiliser JWT Bearer pour émettre les réponses de défi 401
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // Utiliser Cookie comme schéma de connexion pour que le middleware OAuth puisse persister
    // l'identité authentifiée dans un cookie jusqu'à ce que le client Blazor appelle /api/auth/exchange
    x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
// Enregistrement du gestionnaire d'authentification Cookie (requis par OAuth pour stocker l'identité temporaire)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
// Enregistrement de la validation des jetons JWT Bearer
.AddJwtBearer(x =>
{
    // Autoriser HTTP en développement (mettre à true en production pour exiger HTTPS)
    x.RequireHttpsMetadata = false;
    // Stocker le jeton validé dans le contexte HTTP pour une utilisation en aval
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        // Vérifier la signature du jeton avec notre clé secrète
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        // Ignorer la validation de l'émetteur et de l'audience (API mono-service, pas de fédération)
        ValidateIssuer   = false,
        ValidateAudience = false,
        // Aucune tolérance de décalage d'horloge — les jetons expirent exactement à l'heure de la revendication Expires
        ClockSkew = TimeSpan.Zero
    };
})
// Enregistrement du fournisseur OAuth 2.0 Google
.AddGoogle(googleOptions =>
{
    var googleAuth = builder.Configuration.GetSection("GoogleAuth");
    // Identifiants client OAuth enregistrés dans la Google Cloud Console
    googleOptions.ClientId     = googleAuth["ClientId"]     ?? "";
    googleOptions.ClientSecret = googleAuth["ClientSecret"] ?? "";
    // Chemin vers lequel Google redirige après que l'utilisateur s'est authentifié sur son écran de consentement
    googleOptions.CallbackPath = "/api/auth/google-signin";

    // Correction des erreurs "Correlation failed" derrière nginx où TLS est terminé par le proxy.
    // Le cookie de corrélation doit être SameSite=None pour survivre à la redirection externe,
    // et Secure=Always car Chrome rejette SameSite=None sans HTTPS.
    googleOptions.CorrelationCookie.SameSite     = SameSiteMode.None;
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    googleOptions.CorrelationCookie.HttpOnly     = true;

    // Correction de TaskCanceledException : ExchangeCodeAsync passe Context.RequestAborted au backchannel
    // HttpClient. Si nginx ferme la connexion ou que le navigateur se déconnecte, RequestAborted se déclenche
    // et annule l'échange de jeton avant que Google puisse répondre.
    // DetachedCancellationHandler (défini ci-dessous) encapsule le gestionnaire interne et remplace
    // RequestAborted par CancellationToken.None, ne laissant actif que le BackchannelTimeout (30s).
    googleOptions.BackchannelTimeout = TimeSpan.FromSeconds(30);
    googleOptions.BackchannelHttpHandler = new DetachedCancellationHandler(new SocketsHttpHandler
    {
        // Durée maximale d'attente d'une connexion TCP initiale vers les serveurs Google
        ConnectTimeout           = TimeSpan.FromSeconds(10),
        // Recyclage des connexions en pool après 2 minutes pour éviter les connexions périmées
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
})
// Enregistrement du fournisseur OAuth Microsoft Account / Outlook
.AddMicrosoftAccount(msOptions =>
{
    var msAuth = builder.Configuration.GetSection("MicrosoftAuth");
    // Identifiants client OAuth enregistrés dans le portail Azure
    msOptions.ClientId     = msAuth["ClientId"]     ?? "";
    msOptions.ClientSecret = msAuth["ClientSecret"] ?? "";
    // Chemin vers lequel Microsoft redirige après l'authentification
    msOptions.CallbackPath = "/api/auth/microsoft-signin";
    // Mêmes corrections SameSite/Secure que Google (voir ci-dessus)
    msOptions.CorrelationCookie.SameSite     = SameSiteMode.None;
    msOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    msOptions.CorrelationCookie.HttpOnly     = true;
});

// --- Configuration de la base de données ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Enregistrement d'AgendaContext en tant que service scopé avec le fournisseur Npgsql
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseNpgsql(connectionString));

// --- Configuration de la protection des données ---
// Les clés de protection des données servent à chiffrer le cookie de corrélation OAuth et le cookie auth_pending.
// Sur les déploiements Linux, les clés doivent être persistées sur disque pour survivre aux redémarrages de l'application.
// Sans cela, tous les flux OAuth échoueront après un redémarrage car le cookie de corrélation
// ne peut pas être déchiffré par un nouveau jeu de clés.
builder.Services.AddDataProtection()
    // Stockage des clés dans un répertoire fixe du système de fichiers du serveur
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/var/www/serapi/dataprotection-keys"))
    // Association des clés à ce nom d'application spécifique pour éviter le partage de clés avec d'autres applications
    .SetApplicationName("serapi");

// --- Politiques d'autorisation ---
// Enregistrement de la politique "AdminOnly" qui requiert le rôle ADMIN (utilisée par la route /api/update-scolaire)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));

// --- Configuration de la limitation de débit ---
// Protection des points de terminaison de connexion et d'inscription contre les attaques par force brute et la création de comptes en masse.
// Politique "auth" : fenêtre fixe de 5 requêtes par 15 minutes, indexée par adresse IP du client.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Utilisation de l'adresse IP distante comme clé de partition ; repli sur "unknown" si indisponible
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Maximum 5 requêtes autorisées dans une seule fenêtre
                PermitLimit            = 5,
                // Durée de la fenêtre : 15 minutes
                Window                 = TimeSpan.FromMinutes(15),
                // Traitement des requêtes les plus anciennes en premier si certaines sont en file d'attente (0 ici, donc pas de mise en file)
                QueueProcessingOrder   = QueueProcessingOrder.OldestFirst,
                // Pas de mise en file d'attente — les requêtes excédentaires sont immédiatement rejetées avec 429
                QueueLimit             = 0
            }));
    // HTTP 429 Too Many Requests est retourné lorsque la limite de débit est dépassée
    options.RejectionStatusCode = 429;
});

// --- Services de l'application ---
// Enregistrement de ScolaireScraper en tant que service scopé (créé une fois par requête HTTP)
builder.Services.AddScoped<ScolaireScraper>();
// Enregistrement du service e-mail via son interface pour la testabilité / l'inversion de dépendance
builder.Services.AddScoped<seragenda.Services.IEmailService, seragenda.Services.EmailService>();
// Enregistrement de tous les contrôleurs MVC
builder.Services.AddControllers();
// Enregistrement de l'explorateur d'API (utilisé par Swagger pour générer la spécification OpenAPI)
builder.Services.AddEndpointsApiExplorer();
// Enregistrement de la génération Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// --- Configuration des en-têtes redirigés ---
// L'API s'exécute derrière un proxy inverse nginx qui termine TLS.
// UseForwardedHeaders réécrit l'Host, le Scheme et le RemoteIP de la requête en utilisant les
// en-têtes X-Forwarded-For, X-Forwarded-Host et X-Forwarded-Proto envoyés par nginx.
// KnownNetworks et KnownProxies sont vidés pour accepter les en-têtes de n'importe quel proxy
// (adapté quand nginx et l'API sont sur le même segment de réseau de confiance).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor    |  // Restaure l'IP réelle du client
        ForwardedHeaders.XForwardedHost   |  // Restaure l'en-tête Host d'origine
        ForwardedHeaders.XForwardedProto;    // Restaure "https" comme schéma de la requête
    // Faire confiance aux en-têtes redirigés de n'importe quel réseau (nginx est sur le même hôte/sous-réseau)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Construction de l'application à partir des services configurés
var app = builder.Build();

// --- Pipeline de middleware (l'ordre compte dans ASP.NET Core) ---

// Exposition de l'interface Swagger uniquement en environnement de développement pour éviter de divulguer les docs API en production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ForwardedHeaders doit être le PREMIER middleware pour que tous les suivants
// (CORS, authentification, limiteur de débit) voient l'IP réelle du client et le schéma HTTPS
app.UseForwardedHeaders();

// Application de la limitation de débit avant toute authentification — les requêtes rejetées ne doivent pas gaspiller des ressources d'auth
app.UseRateLimiter();

// La redirection HTTPS est désactivée car l'application s'exécute en HTTP derrière nginx,
// qui gère la terminaison TLS. La réactiver causerait des boucles de redirection.
// app.UseHttpsRedirection();

// Servir les fichiers statiques Blazor WASM depuis wwwroot (index.html, .js, .wasm, etc.)
app.UseDefaultFiles();
app.UseStaticFiles();

// Application de la politique CORS — doit précéder l'authentification pour gérer les requêtes OPTIONS de pré-contrôle
app.UseCors();

// Authentification du jeton JWT sur chaque requête (alimente User.Identity / User.Claims)
app.UseAuthentication();
// Application des attributs [Authorize] et des exigences de politique
app.UseAuthorization();

// Mappage de toutes les routes de contrôleur (ex. /api/auth, /api/courses, etc.)
app.MapControllers();

// Point de terminaison minimal-API réservé aux administrateurs qui déclenche le scraper de calendrier scolaire à la demande.
// Protégé par la politique "AdminOnly" (requiert le rôle ADMIN).
app.MapGet("/api/update-scolaire", async (ScolaireScraper scraper) =>
{
    // Exécution du scraper et attente de sa completion
    await scraper.DemarrerScraping();
    return Results.Ok("Scraping terminé !");
}).RequireAuthorization("AdminOnly");

// Route de repli : tout chemin non correspondant est servi avec le point d'entrée Blazor WASM (index.html).
// Cela active le routage côté client dans l'application Blazor (les liens profonds fonctionnent au rechargement de page).
app.MapFallbackToFile("index.html");

// Démarrage du serveur Kestrel et début du traitement des requêtes
app.Run();

// DelegatingHandler personnalisé qui remplace le CancellationToken passé à
// HttpMessageHandler.SendAsync par CancellationToken.None.
// Cela empêche l'échange de jeton OAuth du backchannel d'être annulé quand la
// connexion HTTP du navigateur est fermée (ce qui déclenche Context.RequestAborted).
// Seul le BackchannelTimeout (30 secondes) reste comme source d'annulation.
public class DetachedCancellationHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    // Transmet la requête HTTP au gestionnaire interne en ignorant le jeton d'annulation fourni.
    // request : le message de requête HTTP sortant
    // cancellationToken : ignoré — remplacé par CancellationToken.None
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        // Passage de CancellationToken.None pour que la déconnexion du navigateur ne puisse pas interrompre l'échange de code
        => base.SendAsync(request, CancellationToken.None);
}

// Expose Program pour WebApplicationFactory dans les tests d'intégration
public partial class Program { }
