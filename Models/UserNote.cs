// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une entrée de note personnelle horodatée dans l'agenda quotidien d'un enseignant.
// Chaque note est attachée à une date calendaire spécifique et occupe un ou plusieurs
// créneaux horaires consécutifs dans la grille de l'agenda (qui s'étend de 06h00 à 23h00).
// Le contenu est stocké sous forme de texte brut assaini (toutes les balises HTML supprimées côté serveur).
public class UserNote
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int Id { get; set; }

    // Clé étrangère vers la table Utilisateur ; identifie l'enseignant propriétaire de cette note
    public int IdUserFk { get; set; }

    // La date calendaire à laquelle cette note appartient (la composante heure est toujours minuit / ignorée)
    public DateTime Date { get; set; }

    // L'heure à laquelle cette note commence dans la grille de l'agenda.
    // Plage valide : 8 (8h00) à 17 (17h00)
    public int Hour { get; set; }

    // L'heure à laquelle cette note se termine dans la grille de l'agenda (borne supérieure exclusive).
    // Plage valide : 9 à 18 ; doit être strictement supérieure à Hour.
    public int EndHour { get; set; }

    // La minute de début associée à Hour (0–55, par pas de 5).
    // Vaut 0 pour les notes créées avant l'introduction des minutes.
    public int Minute { get; set; }

    // La minute de fin associée à EndHour (0–55, par pas de 5).
    // La fin (EndHour:EndMinute) doit être strictement postérieure au début (Hour:Minute).
    public int EndMinute { get; set; }

    // Le contenu textuel de la note ; les balises HTML sont supprimées côté serveur avant le stockage.
    // Longueur maximale imposée côté serveur : 2000 caractères.
    public string Content { get; set; } = string.Empty;

    // Horodatage UTC de la première création de cette note
    public DateTime CreatedAt { get; set; }

    // Horodatage UTC de la dernière modification du contenu ou du créneau horaire de cette note
    public DateTime ModifiedAt { get; set; }

    // Clé étrangère optionnelle vers la visée (objectif du référentiel) associée à cette note
    // via la cascade de sélection. Null si aucune visée n'a été rattachée.
    public int? IdViseeFk { get; set; }

    // Contexte complet de la sélection en cascade, composé côté client au moment de
    // l'enregistrement (Année, Catégorie, Cours, Domaine, [Sous-domaine], Compétence,
    // Visée, Visée à maîtriser), une ligne par niveau. Affiché tel quel dans le calendrier.
    // Fige le libellé choisi (la VM n'est pas dérivable de l'id de visée seul). Null si non renseigné.
    public string? ViseeContexte { get; set; }

    // Propriété de navigation vers l'enregistrement Utilisateur propriétaire
    // Marquée comme nullable car elle n'est pas toujours chargée en mode eager
    public virtual Utilisateur? User { get; set; }
}
