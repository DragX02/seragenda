using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var secretkey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(secretkey)) 
{
    throw new Exception("Pas de clef");
}
var key = Encoding.ASCII.GetBytes(secretkey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Services
builder.Services.AddScoped<ScolaireScraper>();
builder.Services.AddControllers(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Serveur I en ligne !");

app.MapGet("/api/update-scolaire", async (ScolaireScraper scraper) =>
{
    await scraper.DemarrerScraping();
    return Results.Ok("Scraping terminé !");
}).RequireAuthorization();

app.Run();