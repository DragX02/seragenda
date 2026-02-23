using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;
using System.Threading.RateLimiting;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var secretkey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(secretkey))
{
    throw new Exception("Pas de clef");
}
var key = Encoding.ASCII.GetBytes(secretkey);

// CORS : origines autorisées lues depuis la config (Cors:AllowedOrigins)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(googleOptions =>
{
    var googleAuth = builder.Configuration.GetSection("GoogleAuth");
    googleOptions.ClientId = googleAuth["ClientId"] ?? "";
    googleOptions.ClientSecret = googleAuth["ClientSecret"] ?? "";
    googleOptions.CallbackPath = "/api/auth/google-signin";
    // Fix "Correlation failed" derriere nginx (TLS termine par le proxy)
    // Chrome rejette SameSite=None sans Secure → forcer Secure=Always
    googleOptions.CorrelationCookie.SameSite = SameSiteMode.None;
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    googleOptions.CorrelationCookie.HttpOnly = true;
    // Fix TaskCanceledException : ExchangeCodeAsync passe Context.RequestAborted au HttpClient.
    // Si nginx ferme la connexion ou le browser deconnecte, RequestAborted annule l'echange.
    // DetachedCancellationHandler ignore ce token → seul le BackchannelTimeout (30s) s'applique.
    googleOptions.BackchannelTimeout = TimeSpan.FromSeconds(30);
    googleOptions.BackchannelHttpHandler = new DetachedCancellationHandler(new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
})
.AddMicrosoftAccount(msOptions =>
{
    var msAuth = builder.Configuration.GetSection("MicrosoftAuth");
    msOptions.ClientId = msAuth["ClientId"] ?? "";
    msOptions.ClientSecret = msAuth["ClientSecret"] ?? "";
    msOptions.CallbackPath = "/api/auth/microsoft-signin";
    msOptions.CorrelationCookie.SameSite = SameSiteMode.None;
    msOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    msOptions.CorrelationCookie.HttpOnly = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseNpgsql(connectionString));

// Persister les clés DataProtection sur disque (nécessaire pour les cookies OAuth sur Linux)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/var/www/serapi/dataprotection-keys"))
    .SetApplicationName("serapi");

// Rate limiting : protège les endpoints d'auth contre le brute force
// 5 tentatives par 15 minutes par IP sur login/register
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = 429;
});

// 2. Services
builder.Services.AddScoped<ScolaireScraper>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ForwardedHeaders : faire confiance a tous les proxies (nginx, etc.)
// KnownNetworks/KnownProxies vides = accepter les headers de n'importe quel proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ForwardedHeaders doit etre le premier middleware
app.UseForwardedHeaders();
app.UseRateLimiter();

// app.UseHttpsRedirection(); // Desactive - le serveur tourne en HTTP
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/update-scolaire", async (ScolaireScraper scraper) =>
{
    await scraper.DemarrerScraping();
    return Results.Ok("Scraping terminé !");
}).RequireAuthorization("AdminOnly");

app.MapFallbackToFile("index.html");

app.Run();

// Empeche que Context.RequestAborted annule l'echange de code OAuth (ExchangeCodeAsync).
// Le BackchannelTimeout (30s) reste le seul timeout actif.
public class DetachedCancellationHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => base.SendAsync(request, CancellationToken.None);
}