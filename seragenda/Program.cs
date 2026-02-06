using Microsoft.EntityFrameworkCore;
using seragenda;
using seragenda.Services;

var builder = WebApplication.CreateBuilder(args);

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
app.MapControllers(); 

app.MapGet("/", () => "Serveur API en ligne ! Va sur /api/values pour voir les données.");

app.MapGet("/api/update-scolaire", async (ScolaireScraper scraper) =>
{
    await scraper.DemarrerScraping();
    return Results.Ok("Scraping terminé ! La base de données est à jour.");
});

app.Run();