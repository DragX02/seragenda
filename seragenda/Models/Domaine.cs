// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un domaine pédagogique au sein d'une combinaison matière-niveau spécifique (CoursNiveau).
// Un domaine est un large domaine curriculaire utilisé pour organiser les objectifs d'apprentissage.
// Par exemple, "Algèbre" ou "Géométrie" pourraient être des domaines dans "Mathématiques en 3e année".
// Les domaines peuvent contenir des sous-domaines (Sousdomaine) et sont liés aux objectifs d'apprentissage (Visee).
public partial class Domaine
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdDom { get; set; }

    // Nom d'affichage du domaine (ex. : "Algèbre", "Géométrie", "Lecture")
    public string Nom { get; set; } = null!;

    // Clé étrangère vers l'enregistrement CoursNiveau auquel ce domaine appartient
    // (la combinaison matière + niveau spécifique qui contient ce domaine)
    public int IdCoursNiveauFk { get; set; }

    // Propriété de navigation : l'enregistrement CoursNiveau complet dont ce domaine fait partie
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Propriété de navigation : tous les enregistrements de sous-domaine qui affinent ce domaine davantage
    public virtual ICollection<Sousdomaine> Sousdomaines { get; set; } = new List<Sousdomaine>();

    // Propriété de navigation : tous les objectifs d'apprentissage (visées) qui appartiennent à ce domaine
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
