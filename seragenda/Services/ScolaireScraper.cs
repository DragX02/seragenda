// Import culture-aware date parsing
using System.Globalization;
// Import regular expression support for date range pattern matching
using System.Text.RegularExpressions;
// Import HtmlAgilityPack for parsing raw HTML from the scraped page
using HtmlAgilityPack;
// Import Entity Framework Core for async database operations
using Microsoft.EntityFrameworkCore;
// Import project models
using seragenda.Models;
// Import the root namespace for AgendaContext access
using seragenda;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Services;

/// <summary>
/// Scrapes the official Belgian school calendar from the enseignement.be website
/// and persists any new events to the CalendrierScolaire database table.
/// Each event is either a holiday period ("Vacances") with a start and end date,
/// or a single-day event such as a public holiday or back-to-school day ("Jour Férié/Rentrée").
/// Duplicate detection prevents inserting the same event name + start date twice.
/// </summary>
public class ScolaireScraper
{
    // Entity Framework database context for checking existing records and persisting new ones
    private readonly AgendaContext _context;

    /// <summary>
    /// Constructor — receives the database context via dependency injection.
    /// </summary>
    /// <param name="context">The EF Core database context</param>
    public ScolaireScraper(AgendaContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Entry point for the scraping process.
    /// Iterates over all configured source URLs, downloads and parses each HTML page,
    /// extracts calendar events from the formatted table rows, and saves new entries to the database.
    /// Prints progress to the console and reports the total number of events inserted/updated.
    /// </summary>
    public async Task DemarrerScraping()
    {
        // List of URLs to scrape; add additional page numbers here for future school years
        var urls = new List<string>
        {
            "http://www.enseignement.be/index.php?page=23953",
            // Additional URLs for other school years can be added here
        };

        // Use French locale for date parsing (e.g., "lundi 3 janvier 2025" format)
        var culture = new CultureInfo("fr-FR");

        // Use a single HttpClient for all requests in this session (connection pooling)
        using (HttpClient client = new HttpClient())
        {
            // Running counter of newly inserted or updated calendar events
            int totalCompteur = 0;

            // Process each URL in order
            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Traitement de l'URL : {url}");
                    // Download the full HTML content of the page
                    var html = await client.GetStringAsync(url);

                    // Parse the HTML into a queryable document object model
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // Select all table rows that use the specific border style used for calendar events
                    // on the enseignement.be page (this XPath is tightly coupled to the page structure)
                    var nodes = doc.DocumentNode.SelectNodes("//tr[contains(@style, 'border:1pt solid black')]");

                    // If no matching rows are found, the page structure may have changed
                    if (nodes == null)
                    {
                        Console.WriteLine("Aucun tableau trouvé sur cette page.");
                        continue; // Skip to the next URL
                    }

                    // Process each matching table row
                    foreach (var node in nodes)
                    {
                        // Each row is expected to have at least 2 cells: [event title, date string]
                        var cells = node.SelectNodes("td");
                        if (cells != null && cells.Count >= 2)
                        {
                            // Decode HTML entities (e.g., &nbsp; → space) and trim whitespace
                            string titre   = System.Net.WebUtility.HtmlDecode(cells[0].InnerText).Trim();
                            string dateTxt = System.Net.WebUtility.HtmlDecode(cells[1].InnerText).Trim();

                            // Skip header or footnote rows that start with "NOTE" or are empty
                            if (titre.StartsWith("NOTE") || string.IsNullOrWhiteSpace(titre)) continue;

                            // Attempt to parse the event title and date text into a CalendrierScolaire record
                            CalendrierScolaire? eventData = ParseLigne(titre, dateTxt, culture);
                            if (eventData != null)
                            {
                                // Persist only if no identical event (same name + start date) already exists
                                await SauvegarderEnDB(eventData);
                                totalCompteur++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue to the next URL rather than aborting the entire run
                    Console.WriteLine($"Erreur sur {url} : {ex.Message}");
                }
            }

            Console.WriteLine($"TOTAL TERMINÉ : {totalCompteur} événements ajoutés ou mis à jour.");
        }
    }

    /// <summary>
    /// Parses a single table row's title and date text into a <see cref="CalendrierScolaire"/> record.
    /// Two date formats are recognised:
    /// 1. Date range: "du [start] au [end]" → typed as "Vacances"
    /// 2. Single date: "le [date]" or just "[date]" → typed as "Jour Férié/Rentrée"
    /// Returns null if the date text cannot be parsed.
    /// </summary>
    /// <param name="titre">The event name extracted from the first table cell</param>
    /// <param name="dateTxt">The raw date text extracted from the second table cell</param>
    /// <param name="culture">The French locale used for date parsing</param>
    /// <returns>A populated <see cref="CalendrierScolaire"/> or null on parse failure</returns>
    private CalendrierScolaire? ParseLigne(string titre, string dateTxt, CultureInfo culture)
    {
        try
        {
            // Normalise common variants in the date text before attempting to parse:
            // "1er" is the French ordinal suffix for "1st" — replace with plain "1"
            // &nbsp; and line breaks may appear in the raw HTML content
            dateTxt = dateTxt.Replace("1er", "1")
                             .Replace("&nbsp;", " ")
                             .Replace("\n", " ")
                             .Replace("\r", " ")
                             .Trim();

            DateTime debutDt, finDt;

            // Try to match a date range pattern: "du <start> au <end>"
            var matchRange = Regex.Match(dateTxt, @"du\s+(.+?)\s+au\s+(.+)", RegexOptions.IgnoreCase);

            if (matchRange.Success)
            {
                // Extract the raw start and end date strings from the regex capture groups
                string debutStr = matchRange.Groups[1].Value.Trim();
                string finStr   = matchRange.Groups[2].Value.Trim();

                // Always parse the end date first because it typically contains the year
                if (TrySmartParse(finStr, culture, out finDt))
                {
                    // Try to parse the start date as-is
                    if (TrySmartParse(debutStr, culture, out debutDt))
                    {
                        // Handle the edge case where the end date has a year but the start date does not
                        // (e.g., "du 28 décembre au 5 janvier 2025" — start year must be inferred)
                        if (finDt.Year > debutDt.Year && !debutStr.Any(char.IsDigit))
                        {
                            // Year inference would go here if needed; currently left as a no-op placeholder
                        }
                    }
                    else
                    {
                        // The start date string has no year — append the end date's year and re-parse
                        string debutAvecAnnee = debutStr + " " + finDt.Year;
                        TrySmartParse(debutAvecAnnee, culture, out debutDt);
                    }

                    // Build the calendar event record for this holiday period
                    return new CalendrierScolaire
                    {
                        NomEvenement  = titre,
                        DateDebut     = DateOnly.FromDateTime(debutDt),
                        DateFin       = DateOnly.FromDateTime(finDt),
                        TypeEvenement = "Vacances"  // Date range events are categorised as school holidays
                    };
                }
            }
            else
            {
                // No range pattern matched — treat the text as a single-day event
                // Strip common prefix words like "le " that appear before single dates
                string dateClean = dateTxt.ToLower().Replace("le ", "").Trim();
                if (TrySmartParse(dateClean, culture, out debutDt))
                {
                    // For single-day events, the start and end dates are the same
                    return new CalendrierScolaire
                    {
                        NomEvenement  = titre,
                        DateDebut     = DateOnly.FromDateTime(debutDt),
                        DateFin       = DateOnly.FromDateTime(debutDt),
                        TypeEvenement = "Jour Férié/Rentrée" // Public holiday or back-to-school day
                    };
                }
            }
        }
        catch { return null; } // Swallow parse exceptions and treat the row as unparseable

        // Return null if no pattern matched or parsing failed
        return null;
    }

    /// <summary>
    /// Attempts to parse a date string using the provided French culture.
    /// Wraps <see cref="DateTime.TryParse"/> with the appropriate culture and style flags.
    /// </summary>
    /// <param name="input">The date string to parse (e.g., "3 janvier 2025")</param>
    /// <param name="culture">The French locale to use for month name recognition</param>
    /// <param name="dateValue">The parsed <see cref="DateTime"/> if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    private bool TrySmartParse(string input, CultureInfo culture, out DateTime dateValue)
    {
        // Use culture-aware parsing without adjusting for any specific DateTimeStyles
        return DateTime.TryParse(input, culture, DateTimeStyles.None, out dateValue);
    }

    /// <summary>
    /// Saves a single calendar event to the database if it does not already exist.
    /// Duplicate detection is based on the event name and start date combination.
    /// If a record with the same name and start date already exists, it is skipped.
    /// </summary>
    /// <param name="evt">The calendar event to conditionally insert</param>
    private async Task SauvegarderEnDB(CalendrierScolaire evt)
    {
        // Check whether an identical event (same name + same start date) already exists
        bool existe = await _context.CalendrierScolaires
            .AnyAsync(e => e.NomEvenement == evt.NomEvenement
                        && e.DateDebut    == evt.DateDebut);

        // Only insert if the event is not already present to avoid duplicates
        if (!existe)
        {
            _context.CalendrierScolaires.Add(evt);
            // SaveChanges is called per-event to avoid losing all progress on a single failure
            await _context.SaveChangesAsync();
        }
    }
}
