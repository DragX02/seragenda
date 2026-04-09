// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une aptitude cognitive ou pédagogique spécifique dans le modèle curriculaire.
// Une Aptitude est un savoir-faire ou comportement observable à grain fin
// (ex. : "résumer un texte", "résoudre une équation du premier degré").
// Les aptitudes sont liées aux visées à maîtriser (ViseesMaitriser) et aux compétences
// via la table de jointure AppartenirViseeAptitude.
public partial class Aptitude
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdAptitude { get; set; }

    // Nom descriptif de l'aptitude (ex. : "Lire à voix haute", "Résoudre une équation")
    // Longueur maximale : 50 caractères, telle que définie dans le schéma de base de données
    public string NomAptitude { get; set; } = null!;

    // Propriété de navigation : tous les liens visée à maîtriser / compétence qui référencent cette aptitude
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();
}
