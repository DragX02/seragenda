// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une compétence générale dans le référentiel curriculaire.
// Une Competence est un domaine de savoir-faire de haut niveau qui regroupe des objectifs d'apprentissage (Visee)
// et des liens visée à maîtriser / aptitude (AppartenirViseeAptitude).
// Exemples de compétences : "Communiquer", "Résoudre des problèmes", "Analyser".
// Longueur maximale du nom : 50 caractères (contrainte de base de données).
public partial class Competence
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdCompetence { get; set; }

    // Nom d'affichage de la compétence (ex. : "Communiquer", "Résoudre", "Analyser")
    // Longueur maximale : 50 caractères
    public string NomCompetence { get; set; } = null!;

    // Propriété de navigation : tous les liens visée à maîtriser / aptitude qui impliquent cette compétence
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    // Propriété de navigation : tous les objectifs d'apprentissage (visées) classifiés sous cette compétence
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
