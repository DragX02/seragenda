using System.Collections.Generic;

namespace seragenda.Models;

// Représente une catégorie de niveau supérieur qui regroupe des matières apparentées.
// Par exemple : "Sciences", "Langues", "Humanités".
// Chaque Cour appartient à exactement une CategorieCours via la clé étrangère id_cat_fk.
public partial class CategorieCours
{
    // Clé primaire — entier auto-incrémenté attribué par la base de données
    public int IdCat { get; set; }

    // Nom d'affichage de la catégorie (ex. : "Sciences et techniques", "Langues modernes")
    public string NomCat { get; set; } = null!;

    // Ordre de tri utilisé pour contrôler la séquence d'affichage dans les listes déroulantes
    public int Ordre { get; set; }

    // Propriété de navigation : toutes les matières appartenant à cette catégorie
    public virtual ICollection<Cour> Cours { get; set; } = new List<Cour>();
}
