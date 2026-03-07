// Importation des types .NET de base (DateOnly, etc.)
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un événement du calendrier scolaire, tel qu'une période de vacances, un jour férié
// ou une journée de rentrée scolaire.
// Les enregistrements sont alimentés par le service ScolaireScraper à partir du
// site officiel de l'enseignement belge (enseignement.be).
// Le champ AnneeScolaire est une colonne calculée en base de données, dérivée de DateDebut :
//   - Si le mois >= 8 (août) → "AAAA-(AAAA+1)" (ex. : "2024-2025")
//   - Sinon                  → "(AAAA-1)-AAAA"  (ex. : "2024-2025" pour janvier 2025)
public partial class CalendrierScolaire
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdCalendrier { get; set; }

    // Nom de l'événement (ex. : "Vacances de Noël", "Jour de l'An", "Rentrée scolaire")
    // Longueur maximale : 100 caractères
    public string NomEvenement { get; set; } = null!;

    // Premier jour de la période de l'événement (inclus)
    public DateOnly DateDebut { get; set; }

    // Dernier jour de la période de l'événement (inclus) ; identique à DateDebut pour les événements d'un seul jour
    public DateOnly DateFin { get; set; }

    // Catégorie de l'événement : "Vacances" pour les périodes de congé, "Jour Férié/Rentrée" pour les jours uniques
    // Longueur maximale : 50 caractères
    public string TypeEvenement { get; set; } = null!;

    // Colonne calculée en base de données représentant l'année scolaire à laquelle appartient cet événement
    // (ex. : "2024-2025") ; calculée côté serveur par une expression SQL CASE — ne pas définir manuellement
    public string? AnneeScolaire { get; set; }

    // Propriété de navigation : toutes les séances de planification qui référencent cet événement du calendrier
    // (utilisée lorsqu'une séance est liée à un jour ou jour férié spécifique du calendrier)
    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();
}
