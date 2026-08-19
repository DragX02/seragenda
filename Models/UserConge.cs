// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Correction personnelle du calendrier des congés scolaires.
//
// La table calendrier_scolaire est alimentée automatiquement par le scraper et
// contient parfois des dates inexactes ou incomplètes. Plutôt que de modifier
// cette table commune, chaque enseignant enregistre ici ses propres corrections,
// appliquées uniquement à son calendrier :
//   - IdCalendrierFk renseigné + Masque = false → remplace les dates/le nom du congé officiel
//   - IdCalendrierFk renseigné + Masque = true  → masque complètement le congé officiel
//   - IdCalendrierFk null                       → congé ajouté de toutes pièces par l'utilisateur
public class UserConge
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int Id { get; set; }

    // Clé étrangère vers la table Utilisateur ; identifie l'enseignant propriétaire de la correction
    public int IdUserFk { get; set; }

    // Identifiant du congé officiel corrigé, ou null lorsque l'utilisateur ajoute un congé
    // qui n'existe pas dans le calendrier officiel.
    // Volontairement sans contrainte de clé étrangère : le scraper peut reconstruire la table
    // calendrier_scolaire, et une correction orpheline est ignorée à l'affichage plutôt que
    // de faire échouer la suppression de la ligne officielle.
    public int? IdCalendrierFk { get; set; }

    // Nom affiché du congé (ex. : "Conge d'automne (Toussaint)").
    // Longueur maximale imposée côté serveur : 100 caractères.
    public string Nom { get; set; } = string.Empty;

    // Premier jour de la période (inclus)
    public DateTime DateDebut { get; set; }

    // Dernier jour de la période (inclus) ; doit être postérieur ou égal à DateDebut
    public DateTime DateFin { get; set; }

    // Vrai lorsque l'utilisateur veut simplement faire disparaître un congé officiel
    // de son calendrier ; les dates et le nom sont alors sans effet.
    public bool Masque { get; set; }

    // Propriété de navigation vers l'enregistrement Utilisateur propriétaire
    public virtual Utilisateur? User { get; set; }
}
