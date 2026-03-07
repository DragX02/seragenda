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
    // Plage valide : 6 (6h00) à 22 (22h00)
    public int Hour { get; set; }

    // L'heure à laquelle cette note se termine dans la grille de l'agenda (borne supérieure exclusive).
    // Plage valide : 7 à 23 ; doit être strictement supérieure à Hour.
    public int EndHour { get; set; }

    // Le contenu textuel de la note ; les balises HTML sont supprimées côté serveur avant le stockage.
    // Longueur maximale imposée côté serveur : 2000 caractères.
    public string Content { get; set; } = string.Empty;

    // Horodatage UTC de la première création de cette note
    public DateTime CreatedAt { get; set; }

    // Horodatage UTC de la dernière modification du contenu ou du créneau horaire de cette note
    public DateTime ModifiedAt { get; set; }

    // Propriété de navigation vers l'enregistrement Utilisateur propriétaire
    // Marquée comme nullable car elle n'est pas toujours chargée en mode eager
    public virtual Utilisateur? User { get; set; }
}
