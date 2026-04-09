// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une entrée de cours récurrente dans l'emploi du temps personnel d'un enseignant.
// Un enregistrement UserCourse décrit un cours qui se répète certains jours de la semaine
// sur une plage de dates (ex. : tout un semestre).
// Les jours de la semaine sont encodés sous forme de masque de bits entier :
//   Lundi = 1, Mardi = 2, Mercredi = 4, Jeudi = 8, Vendredi = 16, Samedi = 32, Dimanche = 64
// Plusieurs jours sont combinés avec un OU binaire (ex. : Lun+Mer = 1|4 = 5).
public class UserCourse
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int Id { get; set; }

    // Clé étrangère vers la table Utilisateur ; identifie l'enseignant propriétaire de cette entrée de cours
    public int IdUserFk { get; set; }

    // Nom d'affichage du cours (ex. : "Mathématiques 3B", "Littérature française")
    public string Name { get; set; } = string.Empty;

    // Chaîne de couleur CSS utilisée pour afficher ce cours dans l'agenda (ex. : "#3B82F6", "#FF0000")
    public string Color { get; set; } = "#FF0000";

    // Première date à laquelle ce cours récurrent a lieu (généralement le début du semestre)
    public DateTime StartDate { get; set; }

    // Dernière date à laquelle ce cours récurrent a lieu (généralement la fin du semestre)
    public DateTime EndDate { get; set; }

    // Heure de début du cours dans la journée (ex. : 08:00:00)
    public TimeSpan StartTime { get; set; }

    // Heure de fin du cours dans la journée (ex. : 09:30:00)
    public TimeSpan EndTime { get; set; }

    // Masque de bits encodant les jours de la semaine auxquels ce cours se répète.
    // Lun=1, Mar=2, Mer=4, Jeu=8, Ven=16, Sam=32, Dim=64
    // Exemple : un cours le lundi et le mercredi a DaysOfWeek = 1 | 4 = 5
    public int DaysOfWeek { get; set; }

    // Propriété de navigation vers l'enregistrement Utilisateur propriétaire
    // Marquée comme nullable car elle n'est pas toujours chargée en mode eager
    public virtual Utilisateur? User { get; set; }
}
