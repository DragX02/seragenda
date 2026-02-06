using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using seragenda;

namespace seragenda.Services;

public class ScolaireScraper
{
    private readonly AgendaContext _context;

    public ScolaireScraper(AgendaContext context)
    {
        _context = context;
    }

    public async Task DemarrerScraping()
    {
        var urls = new List<string>
        {
            "http://www.enseignement.be/index.php?page=23953", 
            // "http://www.enseignement.be/index.php?page=XXXXX" 
        };

        var culture = new CultureInfo("fr-FR");

        using (HttpClient client = new HttpClient())
        {
            int totalCompteur = 0;

            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Traitement de l'URL : {url}");
                    var html = await client.GetStringAsync(url);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var nodes = doc.DocumentNode.SelectNodes("//tr[contains(@style, 'border:1pt solid black')]");

                    if (nodes == null)
                    {
                        Console.WriteLine("Aucun tableau trouvé sur cette page.");
                        continue;
                    }

                    foreach (var node in nodes)
                    {
                        var cells = node.SelectNodes("td");
                        if (cells != null && cells.Count >= 2)
                        {
                            string titre = System.Net.WebUtility.HtmlDecode(cells[0].InnerText).Trim();
                            string dateTxt = System.Net.WebUtility.HtmlDecode(cells[1].InnerText).Trim();

                            if (titre.StartsWith("NOTE") || string.IsNullOrWhiteSpace(titre)) continue;

                            CalendrierScolaire? eventData = ParseLigne(titre, dateTxt, culture);
                            if (eventData != null)
                            {
                                await SauvegarderEnDB(eventData);
                                totalCompteur++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur sur {url} : {ex.Message}");
                }
            }
            Console.WriteLine($"TOTAL TERMINÉ : {totalCompteur} événements ajoutés ou mis à jour.");
        }
    }

    private CalendrierScolaire? ParseLigne(string titre, string dateTxt, CultureInfo culture)
    {
        try
        {
            dateTxt = dateTxt.Replace("1er", "1")
                             .Replace("&nbsp;", " ")
                             .Replace("\n", " ")
                             .Replace("\r", " ")
                             .Trim();

            DateTime debutDt, finDt;

            var matchRange = Regex.Match(dateTxt, @"du\s+(.+?)\s+au\s+(.+)", RegexOptions.IgnoreCase);

            if (matchRange.Success)
            {
                string debutStr = matchRange.Groups[1].Value.Trim();
                string finStr = matchRange.Groups[2].Value.Trim();

                if (TrySmartParse(finStr, culture, out finDt))
                {
                    if (TrySmartParse(debutStr, culture, out debutDt))
                    {
                        if (finDt.Year > debutDt.Year && !debutStr.Any(char.IsDigit))
                        {
                        }
                    }
                    else
                    {
                        string debutAvecAnnee = debutStr + " " + finDt.Year;
                        TrySmartParse(debutAvecAnnee, culture, out debutDt);
                    }

                    return new CalendrierScolaire
                    {
                        NomEvenement = titre,
                        DateDebut = DateOnly.FromDateTime(debutDt),
                        DateFin = DateOnly.FromDateTime(finDt),
                        TypeEvenement = "Vacances"
                    };
                }
            }
            else
            {
                string dateClean = dateTxt.ToLower().Replace("le ", "").Trim();
                if (TrySmartParse(dateClean, culture, out debutDt))
                {
                    return new CalendrierScolaire
                    {
                        NomEvenement = titre,
                        DateDebut = DateOnly.FromDateTime(debutDt),
                        DateFin = DateOnly.FromDateTime(debutDt),
                        TypeEvenement = "Jour Férié/Rentrée"
                    };
                }
            }
        }
        catch { return null; }
        return null;
    }

    private bool TrySmartParse(string input, CultureInfo culture, out DateTime dateValue)
    {
        return DateTime.TryParse(input, culture, DateTimeStyles.None, out dateValue);
    }

    private async Task SauvegarderEnDB(CalendrierScolaire evt)
    {
        bool existe = await _context.CalendrierScolaires
            .AnyAsync(e => e.NomEvenement == evt.NomEvenement
                        && e.DateDebut == evt.DateDebut);

        if (!existe)
        {
            _context.CalendrierScolaires.Add(evt);
            await _context.SaveChangesAsync();
        }
    }
}