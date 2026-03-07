// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un niveau d'enseignement (groupe d'année ou classe) dans la structure curriculaire.
// Un Niveau regroupe les élèves d'une même année académique (ex. : "1A", "3B", "6TH").
// Il est lié aux matières via la table de jointure CoursNiveau.
// Chaque niveau possède un code court unique et un nom d'affichage.
public partial class Niveau
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdNiveau { get; set; }

    // Code court unique du niveau, utilisé dans les paramètres d'API et les recherches internes (ex. : "1A", "3B")
    // Possède un index unique en base de données pour garantir l'unicité du code
    public string CodeNiveau { get; set; } = null!;

    // Nom complet lisible du niveau d'enseignement (ex. : "Première A", "Troisième B")
    public string NomNiveau { get; set; } = null!;

    // Propriété de navigation : toutes les combinaisons cours-niveau qui référencent ce niveau d'enseignement
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
