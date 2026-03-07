// Importation des types de base .NET
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une matière (cours) dans le catalogue du programme scolaire.
// Un enregistrement Cour identifie une matière enseignable telle que les Mathématiques, le Français ou la Biologie.
// Chaque matière possède un code court unique utilisé dans les paramètres d'URL et les recherches de données.
// Les matières peuvent être enseignées à plusieurs niveaux scolaires, représentés via la table de liaison CoursNiveau.
public partial class Cour
{
    // Clé primaire — entier auto-incrémenté attribué par la base de données
    public int IdCours { get; set; }

    // Nom d'affichage complet de la matière (ex. : "Mathématiques", "Français")
    public string NomCours { get; set; } = null!;

    // Code court unique de la matière utilisé dans les routes d'API et les recherches internes (ex. : "MATH", "FR")
    // Possède un index unique en base de données pour garantir l'unicité du code
    public string CodeCours { get; set; } = null!;

    // Chaîne de préfixe courte utilisée pour générer des identifiants de leçons ou de chapitres (ex. : "MAT", "FRA")
    public string PrefixCours { get; set; } = null!;

    // Chaîne de couleur hexadécimale compatible CSS utilisée pour afficher cette matière dans l'agenda (ex. : "#3B82F6")
    public string CouleurAgenda { get; set; } = null!;

    // Clé étrangère reliant cette matière à sa catégorie de niveau supérieur (colonne id_cat_fk en base de données)
    public int? IdCatFk { get; set; }

    // Propriété de navigation : la catégorie à laquelle appartient cette matière
    public virtual CategorieCours? IdCatFkNavigation { get; set; }

    // Propriété de navigation : toutes les combinaisons cours-niveau où cette matière est enseignée
    // (chaque entrée CoursNiveau relie cette matière à un niveau scolaire et à un enseignant spécifiques)
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
