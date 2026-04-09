// Importation des types .NET de base (DateOnly, TimeOnly, etc.)
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une séance de cours planifiée dans l'agenda de l'enseignant.
// Une Planification relie une date et un créneau horaire spécifiques à une combinaison cours-niveau (CoursNiveau)
// et optionnellement à un événement du calendrier scolaire (CalendrierScolaire) tel qu'un jour férié.
// Chaque séance peut avoir plusieurs objectifs d'apprentissage associés (SeanceObjectif)
// et plusieurs références de ressources (SeanceRessource — chapitres utilisés pendant la séance).
// Le statut de la séance est par défaut "Prévue" (planifiée) et peut évoluer vers d'autres états
// (ex. : "Réalisée", "Annulée").
public partial class Planification
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdPlanning { get; set; }

    // La date calendaire à laquelle cette séance de cours est planifiée
    public DateOnly DateSeance { get; set; }

    // Heure de début optionnelle de la séance de cours (ex. : 08:30)
    // Null si l'heure n'est pas encore déterminée
    public TimeOnly? HeureDebut { get; set; }

    // Heure de fin optionnelle de la séance de cours (ex. : 10:00)
    // Null si l'heure n'est pas encore déterminée
    public TimeOnly? HeureFin { get; set; }

    // Clé étrangère vers l'enregistrement CoursNiveau ; identifie la matière-niveau à laquelle cette séance appartient
    public int IdCoursNiveauFk { get; set; }

    // Clé étrangère optionnelle vers un enregistrement CalendrierScolaire (ex. : si la séance tombe un jour noté au calendrier)
    // Null si la séance n'est pas associée à un événement spécifique du calendrier scolaire
    public int? IdCalendrierFk { get; set; }

    // Note privée optionnelle rédigée par l'enseignant à propos de cette séance (texte libre)
    public string? NoteProf { get; set; }

    // Statut actuel de la séance ; par défaut "Prévue" en base de données
    // Valeurs possibles : "Prévue", "Réalisée", "Annulée"
    public string? Statut { get; set; }

    // Propriété de navigation : l'événement optionnel du calendrier scolaire associé à cette séance
    public virtual CalendrierScolaire? IdCalendrierFkNavigation { get; set; }

    // Propriété de navigation : l'enregistrement CoursNiveau complet (matière + niveau + enseignant) pour cette séance
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Propriété de navigation : tous les objectifs d'apprentissage planifiés pour cette séance
    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    // Propriété de navigation : toutes les ressources de chapitres de manuels prévues pour cette séance
    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();
}
