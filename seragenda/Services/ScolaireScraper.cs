// Import de l'analyse de dates tenant compte de la culture
using System.Globalization;
// Import du support des expressions régulières pour la correspondance de plages de dates
using System.Text.RegularExpressions;
// Import de HtmlAgilityPack pour analyser le HTML brut de la page récupérée
using HtmlAgilityPack;
// Import d'Entity Framework Core pour les opérations de base de données asynchrones
using Microsoft.EntityFrameworkCore;
// Import des modèles du projet
using seragenda.Models;
// Import de l'espace de noms racine pour l'accès à AgendaContext
using seragenda;

// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Services;

// Récupère le calendrier scolaire officiel belge depuis le site enseignement.be
// et persiste les nouveaux événements dans la table CalendrierScolaire de la base de données.
// Chaque événement est soit une période de vacances avec une date de début et de fin,
// soit un événement d'un jour tel qu'un jour férié ou une rentrée scolaire.
// La détection des doublons empêche d'insérer deux fois le même nom d'événement + date de début.
public class ScolaireScraper
{
    // Contexte de base de données Entity Framework pour vérifier les enregistrements existants et persister les nouveaux
    private readonly AgendaContext _context;

    // Constructeur — reçoit le contexte de base de données par injection de dépendances.
    // context : le contexte de base de données EF Core
    public ScolaireScraper(AgendaContext context)
    {
        _context = context;
    }

    // Point d'entrée du processus de scraping.
    // Itère sur toutes les URLs sources configurées, télécharge et analyse chaque page HTML,
    // extrait les événements du calendrier à partir des lignes de tableau formatées,
    // et sauvegarde les nouvelles entrées dans la base de données.
    // Affiche la progression dans la console et signale le nombre total d'événements insérés/mis à jour.
    public async Task DemarrerScraping()
    {
        // Liste des URLs à scraper ; ajouter ici des numéros de page supplémentaires pour les années scolaires futures
        var urls = new List<string>
        {
            "http://www.enseignement.be/index.php?page=23953",
            // Des URLs supplémentaires pour d'autres années scolaires peuvent être ajoutées ici
        };

        // Utilisation de la locale française pour l'analyse des dates (ex. format "lundi 3 janvier 2025")
        var culture = new CultureInfo("fr-FR");

        // Utilisation d'un seul HttpClient pour toutes les requêtes de la session (mise en pool des connexions)
        using (HttpClient client = new HttpClient())
        {
            // Compteur des événements du calendrier nouvellement insérés ou mis à jour
            int totalCompteur = 0;

            // Traitement de chaque URL dans l'ordre
            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Traitement de l'URL : {url}");
                    // Téléchargement du contenu HTML complet de la page
                    var html = await client.GetStringAsync(url);

                    // Analyse du HTML dans un modèle objet de document interrogeable
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // Sélection de toutes les lignes de tableau utilisant le style de bordure spécifique aux événements du calendrier
                    // sur la page enseignement.be (ce XPath est étroitement couplé à la structure de la page)
                    var nodes = doc.DocumentNode.SelectNodes("//tr[contains(@style, 'border:1pt solid black')]");

                    // Si aucune ligne correspondante n'est trouvée, la structure de la page a peut-être changé
                    if (nodes == null)
                    {
                        Console.WriteLine("Aucun tableau trouvé sur cette page.");
                        continue; // Passer à l'URL suivante
                    }

                    // Traitement de chaque ligne de tableau correspondante
                    foreach (var node in nodes)
                    {
                        // Chaque ligne est censée avoir au moins 2 cellules : [titre de l'événement, chaîne de date]
                        var cells = node.SelectNodes("td");
                        if (cells != null && cells.Count >= 2)
                        {
                            // Décodage des entités HTML (ex. &nbsp; → espace) et suppression des espaces
                            string titre   = System.Net.WebUtility.HtmlDecode(cells[0].InnerText).Trim();
                            string dateTxt = System.Net.WebUtility.HtmlDecode(cells[1].InnerText).Trim();

                            // Ignorer les lignes d'en-tête ou de note de bas de page commençant par "NOTE" ou vides
                            if (titre.StartsWith("NOTE") || string.IsNullOrWhiteSpace(titre)) continue;

                            // Tentative d'analyse du titre et du texte de date en un enregistrement CalendrierScolaire
                            CalendrierScolaire? eventData = ParseLigne(titre, dateTxt, culture);
                            if (eventData != null)
                            {
                                // Persistance uniquement si aucun événement identique (même nom + date de début) n'existe déjà
                                await SauvegarderEnDB(eventData);
                                totalCompteur++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Journalisation de l'erreur mais poursuite vers l'URL suivante plutôt qu'abandon total
                    Console.WriteLine($"Erreur sur {url} : {ex.Message}");
                }
            }

            Console.WriteLine($"TOTAL TERMINÉ : {totalCompteur} événements ajoutés ou mis à jour.");
        }
    }

    // Analyse le titre et le texte de date d'une ligne de tableau en un enregistrement CalendrierScolaire.
    // Deux formats de date sont reconnus :
    // 1. Plage de dates : "du [début] au [fin]" → typé "Vacances"
    // 2. Date unique : "le [date]" ou simplement "[date]" → typé "Jour Férié/Rentrée"
    // Retourne null si le texte de date ne peut pas être analysé.
    // titre : nom de l'événement extrait de la première cellule du tableau
    // dateTxt : texte de date brut extrait de la deuxième cellule du tableau
    // culture : locale française utilisée pour l'analyse des dates
    // Retourne un CalendrierScolaire renseigné ou null en cas d'échec d'analyse
    private CalendrierScolaire? ParseLigne(string titre, string dateTxt, CultureInfo culture)
    {
        try
        {
            // Normalisation des variantes courantes dans le texte de date avant tentative d'analyse :
            // "1er" est le suffixe ordinal français pour "1st" — remplacé par un simple "1"
            // &nbsp; et les sauts de ligne peuvent apparaître dans le contenu HTML brut
            dateTxt = dateTxt.Replace("1er", "1")
                             .Replace("&nbsp;", " ")
                             .Replace("\n", " ")
                             .Replace("\r", " ")
                             .Trim();

            DateTime debutDt, finDt;

            // Tentative de correspondance avec un motif de plage de dates : "du <début> au <fin>"
            var matchRange = Regex.Match(dateTxt, @"du\s+(.+?)\s+au\s+(.+)", RegexOptions.IgnoreCase);

            if (matchRange.Success)
            {
                // Extraction des chaînes de date de début et de fin depuis les groupes de capture regex
                string debutStr = matchRange.Groups[1].Value.Trim();
                string finStr   = matchRange.Groups[2].Value.Trim();

                // Analyse toujours la date de fin en premier car elle contient généralement l'année
                if (TrySmartParse(finStr, culture, out finDt))
                {
                    // Tentative d'analyse de la date de début telle quelle
                    if (TrySmartParse(debutStr, culture, out debutDt))
                    {
                        // Gestion du cas limite où la date de fin a une année mais pas la date de début
                        // (ex. "du 28 décembre au 5 janvier 2025" — l'année de début doit être déduite)
                        if (finDt.Year > debutDt.Year && !debutStr.Any(char.IsDigit))
                        {
                            // La déduction de l'année irait ici si nécessaire ; actuellement laissée comme espace réservé
                        }
                    }
                    else
                    {
                        // La chaîne de date de début n'a pas d'année — ajout de l'année de la date de fin et nouvelle analyse
                        string debutAvecAnnee = debutStr + " " + finDt.Year;
                        TrySmartParse(debutAvecAnnee, culture, out debutDt);
                    }

                    // Construction de l'enregistrement d'événement calendrier pour cette période de vacances
                    return new CalendrierScolaire
                    {
                        NomEvenement  = titre,
                        DateDebut     = DateOnly.FromDateTime(debutDt),
                        DateFin       = DateOnly.FromDateTime(finDt),
                        TypeEvenement = "Vacances"  // Les événements en plage de dates sont catégorisés comme vacances scolaires
                    };
                }
            }
            else
            {
                // Aucun motif de plage trouvé — traitement du texte comme un événement d'un seul jour
                // Suppression des mots préfixes courants comme "le " qui apparaissent avant les dates simples
                string dateClean = dateTxt.ToLower().Replace("le ", "").Trim();
                if (TrySmartParse(dateClean, culture, out debutDt))
                {
                    // Pour les événements d'un seul jour, les dates de début et de fin sont identiques
                    return new CalendrierScolaire
                    {
                        NomEvenement  = titre,
                        DateDebut     = DateOnly.FromDateTime(debutDt),
                        DateFin       = DateOnly.FromDateTime(debutDt),
                        TypeEvenement = "Jour Férié/Rentrée" // Jour férié ou jour de rentrée scolaire
                    };
                }
            }
        }
        catch { return null; } // Absorption des exceptions d'analyse et traitement de la ligne comme non analysable

        // Retourne null si aucun motif ne correspond ou si l'analyse a échoué
        return null;
    }

    // Tente d'analyser une chaîne de date en utilisant la culture française fournie.
    // Encapsule DateTime.TryParse avec les indicateurs de culture et de style appropriés.
    // input : la chaîne de date à analyser (ex. "3 janvier 2025")
    // culture : la locale française à utiliser pour la reconnaissance des noms de mois
    // dateValue : le DateTime analysé si l'opération a réussi
    // Retourne true si l'analyse a réussi, false sinon
    private bool TrySmartParse(string input, CultureInfo culture, out DateTime dateValue)
    {
        // Utilisation de l'analyse tenant compte de la culture sans ajustement de DateTimeStyles particulier
        return DateTime.TryParse(input, culture, DateTimeStyles.None, out dateValue);
    }

    // Sauvegarde un événement calendrier dans la base de données s'il n'existe pas déjà.
    // La détection des doublons est basée sur la combinaison nom de l'événement et date de début.
    // Si un enregistrement avec le même nom et la même date de début existe déjà, il est ignoré.
    // evt : l'événement calendrier à insérer conditionnellement
    private async Task SauvegarderEnDB(CalendrierScolaire evt)
    {
        // Vérification si un événement identique (même nom + même date de début) existe déjà
        bool existe = await _context.CalendrierScolaires
            .AnyAsync(e => e.NomEvenement == evt.NomEvenement
                        && e.DateDebut    == evt.DateDebut);

        // Insertion uniquement si l'événement n'est pas déjà présent pour éviter les doublons
        if (!existe)
        {
            _context.CalendrierScolaires.Add(evt);
            // SaveChanges est appelé par événement pour ne pas perdre toute la progression en cas d'échec unique
            await _context.SaveChangesAsync();
        }
    }
}
