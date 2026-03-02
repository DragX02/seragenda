// Import ASP.NET Core Data Protection for encrypting cookies (OAuth correlation, auth_pending)
using Microsoft.AspNetCore.DataProtection;
// Import rate limiting middleware and policies
using Microsoft.AspNetCore.RateLimiting;
// Import Entity Framework Core for database context registration
using Microsoft.EntityFrameworkCore;
// Import the project's own namespace for AgendaContext
using seragenda;
// Import the project's service implementations
using seragenda.Services;
// Import JWT Bearer authentication handler
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Import Cookie authentication handler (used as the sign-in scheme for OAuth)
using Microsoft.AspNetCore.Authentication.Cookies;
// Import JWT token validation parameters
using Microsoft.IdentityModel.Tokens;
// Import ForwardedHeaders middleware for running behind nginx/reverse proxy
using Microsoft.AspNetCore.HttpOverrides;
// Import text encoding for converting the JWT secret key to bytes
using System.Text;
// Import rate limiting options and partitions
using System.Threading.RateLimiting;

// Enable legacy timestamp behaviour for Npgsql so that DateTime values are treated as
// "timestamp without time zone" rather than the newer UTC-aware type.
// This is required to maintain compatibility with the existing PostgreSQL schema
// where timestamp columns are stored without timezone information.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Create the WebApplication builder — this is the starting point for all configuration
var builder = WebApplication.CreateBuilder(args);

// Read the JWT secret key from configuration (appsettings.json or environment variable)
var secretkey = builder.Configuration["JwtSettings:SecretKey"];
// Fail fast at startup if the secret key is not configured — a missing key is a security error
if (string.IsNullOrEmpty(secretkey))
{
    throw new Exception("Pas de clef");
}
// Convert the string secret key to a byte array for use in the JWT signing key
var key = Encoding.ASCII.GetBytes(secretkey);

// --- CORS Configuration ---
// Read the list of allowed frontend origins from configuration (Cors:AllowedOrigins section)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

// Register the CORS middleware with a default policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Only allow requests from the configured origins (e.g., the Blazor WASM frontend)
        policy.WithOrigins(allowedOrigins)
              // Allow any HTTP method (GET, POST, PUT, DELETE, etc.)
              .AllowAnyMethod()
              // Allow any request header
              .AllowAnyHeader()
              // Allow cookies to be sent with cross-origin requests (needed for OAuth cookie exchange)
              .AllowCredentials();
    });
});

// --- Authentication Configuration ---
// Configure the authentication pipeline with three schemes:
//   1. JWT Bearer — the primary scheme for API requests (validates the Authorization: Bearer <token> header)
//   2. Cookie — the sign-in scheme used to persist OAuth identity between the callback and the exchange
//   3. Google OAuth 2.0 — external provider for social login
//   4. Microsoft Account OAuth — external provider for social login
builder.Services.AddAuthentication(x =>
{
    // Use JWT Bearer as the default scheme for verifying API request tokens
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // Use JWT Bearer for issuing 401 challenge responses
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // Use Cookie as the sign-in scheme so the OAuth middleware can persist the authenticated
    // identity in a cookie until the Blazor client calls /api/auth/exchange
    x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
// Register the Cookie authentication handler (needed by OAuth to store the temporary identity)
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
// Register JWT Bearer token validation
.AddJwtBearer(x =>
{
    // Allow HTTP in development (set to true in production to require HTTPS)
    x.RequireHttpsMetadata = false;
    // Store the validated token in the HTTP context for downstream use
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        // Verify the token's signature against our secret key
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        // Skip issuer and audience validation (single-service API, no federation)
        ValidateIssuer   = false,
        ValidateAudience = false,
        // No clock skew tolerance — tokens expire exactly at the Expires claim time
        ClockSkew = TimeSpan.Zero
    };
})
// Register Google OAuth 2.0 provider
.AddGoogle(googleOptions =>
{
    var googleAuth = builder.Configuration.GetSection("GoogleAuth");
    // OAuth client credentials registered in the Google Cloud Console
    googleOptions.ClientId     = googleAuth["ClientId"]     ?? "";
    googleOptions.ClientSecret = googleAuth["ClientSecret"] ?? "";
    // The path Google redirects to after the user has authenticated on their consent screen
    googleOptions.CallbackPath = "/api/auth/google-signin";

    // Fix "Correlation failed" errors behind nginx where TLS is terminated by the proxy.
    // The correlation cookie must be SameSite=None so it survives the external redirect,
    // and Secure=Always because Chrome rejects SameSite=None without HTTPS.
    googleOptions.CorrelationCookie.SameSite     = SameSiteMode.None;
    googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    googleOptions.CorrelationCookie.HttpOnly     = true;

    // Fix TaskCanceledException: ExchangeCodeAsync passes Context.RequestAborted to the backchannel
    // HttpClient. If nginx closes the connection or the browser disconnects, RequestAborted fires
    // and cancels the token exchange before Google can respond.
    // DetachedCancellationHandler (defined below) wraps the inner handler and replaces
    // RequestAborted with CancellationToken.None, leaving only the BackchannelTimeout (30s) active.
    googleOptions.BackchannelTimeout = TimeSpan.FromSeconds(30);
    googleOptions.BackchannelHttpHandler = new DetachedCancellationHandler(new SocketsHttpHandler
    {
        // Maximum time to wait for an initial TCP connection to Google's servers
        ConnectTimeout           = TimeSpan.FromSeconds(10),
        // Recycle pooled connections after 2 minutes to avoid stale connections
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
})
// Register Microsoft Account / Outlook OAuth provider
.AddMicrosoftAccount(msOptions =>
{
    var msAuth = builder.Configuration.GetSection("MicrosoftAuth");
    // OAuth client credentials registered in the Azure portal
    msOptions.ClientId     = msAuth["ClientId"]     ?? "";
    msOptions.ClientSecret = msAuth["ClientSecret"] ?? "";
    // The path Microsoft redirects to after authentication
    msOptions.CallbackPath = "/api/auth/microsoft-signin";
    // Same SameSite/Secure fixes as Google (see above)
    msOptions.CorrelationCookie.SameSite     = SameSiteMode.None;
    msOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    msOptions.CorrelationCookie.HttpOnly     = true;
});

// --- Database Configuration ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Register AgendaContext as a scoped service using the Npgsql provider
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseNpgsql(connectionString));

// --- Data Protection Configuration ---
// Data Protection keys are used to encrypt the OAuth correlation cookie and the auth_pending cookie.
// On Linux deployments, the keys must be persisted to disk so they survive application restarts.
// Without this, all OAuth flows will fail after a process restart because the correlation cookie
// cannot be decrypted by a new key set.
builder.Services.AddDataProtection()
    // Store keys in a fixed directory on the server filesystem
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/var/www/serapi/dataprotection-keys"))
    // Associate keys with this specific application name to prevent key sharing with other apps
    .SetApplicationName("serapi");

// --- Authorization Policies ---
// Register the "AdminOnly" policy that requires the ADMIN role (used by the /api/update-scolaire route)
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));

// --- Rate Limiting Configuration ---
// Protect the login and register endpoints against brute-force and account-farming attacks.
// Policy "auth": fixed window of 5 requests per 15 minutes, keyed by client IP address.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Use the remote IP address as the partition key; fall back to "unknown" if not available
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Maximum 5 requests are permitted within a single window
                PermitLimit            = 5,
                // Window duration: 15 minutes
                Window                 = TimeSpan.FromMinutes(15),
                // Process the oldest requests first if any are queued (queue is 0 here, so no queuing)
                QueueProcessingOrder   = QueueProcessingOrder.OldestFirst,
                // No request queuing — excess requests are immediately rejected with 429
                QueueLimit             = 0
            }));
    // HTTP 429 Too Many Requests is returned when the rate limit is exceeded
    options.RejectionStatusCode = 429;
});

// --- Application Services ---
// Register ScolaireScraper as a scoped service (created once per HTTP request)
builder.Services.AddScoped<ScolaireScraper>();
// Register the email service using its interface for testability / dependency inversion
builder.Services.AddScoped<seragenda.Services.IEmailService, seragenda.Services.EmailService>();
// Register all MVC controllers
builder.Services.AddControllers();
// Register the API explorer (used by Swagger to generate the OpenAPI specification)
builder.Services.AddEndpointsApiExplorer();
// Register Swagger/OpenAPI generation
builder.Services.AddSwaggerGen();

// --- Forwarded Headers Configuration ---
// The API runs behind an nginx reverse proxy that terminates TLS.
// UseForwardedHeaders rewrites the request's Host, Scheme, and RemoteIP using the
// X-Forwarded-For, X-Forwarded-Host, and X-Forwarded-Proto headers sent by nginx.
// KnownNetworks and KnownProxies are cleared to accept headers from any proxy
// (suitable when nginx and the API are on the same trusted network segment).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor    |  // Restores the real client IP
        ForwardedHeaders.XForwardedHost   |  // Restores the original Host header
        ForwardedHeaders.XForwardedProto;    // Restores "https" as the request scheme
    // Trust forwarded headers from any network (since nginx is on the same host/subnet)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Build the application from the configured services
var app = builder.Build();

// --- Middleware Pipeline (order matters in ASP.NET Core) ---

// Expose Swagger UI only in the development environment to avoid leaking API docs in production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ForwardedHeaders must be the FIRST middleware so that all subsequent middleware
// (CORS, authentication, rate limiter) sees the real client IP and HTTPS scheme
app.UseForwardedHeaders();

// Apply rate limiting before any authentication — rejected requests should not waste auth resources
app.UseRateLimiter();

// HTTPS redirection is disabled because the application runs on HTTP behind nginx,
// which handles TLS termination. Re-enabling this would cause redirect loops.
// app.UseHttpsRedirection();

// Serve the Blazor WASM static files from wwwroot (index.html, .js, .wasm bundles, etc.)
app.UseDefaultFiles();
app.UseStaticFiles();

// Apply the CORS policy — must come before authentication to handle preflight OPTIONS requests
app.UseCors();

// Authenticate the JWT token on each request (populates User.Identity / User.Claims)
app.UseAuthentication();
// Enforce [Authorize] attributes and policy requirements
app.UseAuthorization();

// Map all controller routes (e.g., /api/auth, /api/courses, etc.)
app.MapControllers();

// Admin-only minimal-API endpoint that triggers the school calendar scraper on demand.
// Protected by the "AdminOnly" policy (requires ADMIN role).
app.MapGet("/api/update-scolaire", async (ScolaireScraper scraper) =>
{
    // Run the scraper and wait for it to complete
    await scraper.DemarrerScraping();
    return Results.Ok("Scraping terminé !");
}).RequireAuthorization("AdminOnly");

// Fallback route: any unmatched path is served the Blazor WASM entry point (index.html).
// This enables client-side routing in the Blazor app (deep links work on page refresh).
app.MapFallbackToFile("index.html");

// Start the Kestrel server and begin processing requests
app.Run();

/// <summary>
/// Custom <see cref="DelegatingHandler"/> that replaces the CancellationToken passed to
/// <see cref="HttpMessageHandler.SendAsync"/> with <see cref="CancellationToken.None"/>.
/// This prevents the OAuth backchannel token exchange from being cancelled when the
/// browser's HTTP connection is closed (which fires Context.RequestAborted).
/// Only the BackchannelTimeout (30 seconds) remains as a cancellation source.
/// </summary>
public class DetachedCancellationHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    /// <summary>
    /// Forwards the HTTP request to the inner handler, ignoring the provided cancellation token.
    /// </summary>
    /// <param name="request">The outgoing HTTP request message</param>
    /// <param name="cancellationToken">Ignored — replaced with CancellationToken.None</param>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        // Pass CancellationToken.None so that browser disconnect cannot abort the code exchange
        => base.SendAsync(request, CancellationToken.None);
}
