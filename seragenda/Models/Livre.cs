// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un manuel scolaire physique ou numérique dans le catalogue de ressources.
// Les Livres (manuels) sont liés aux combinaisons cours-niveau via la table UtilisationLivre
// (indiquant quels manuels sont recommandés ou obligatoires pour une matière et un niveau donnés).
// Les manuels contiennent des chapitres (Chapitre) pouvant être référencés individuellement dans les séances de cours.
// L'ISBN possède un index unique en base de données pour éviter les doublons d'entrées de manuels.
public partial class Livre
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdLivre { get; set; }

    // Titre complet du manuel (ex. : "Mathématiques 4e — Livre de l'élève")
    // Longueur maximale : 255 caractères ; obligatoire (non null)
    public string TitreLivre { get; set; } = null!;

    // Nom(s) du ou des auteur(s) du manuel ; peut être null si inconnu
    // Longueur maximale : 150 caractères
    public string? Auteur { get; set; }

    // Numéro ISBN (International Standard Book Number, 13 chiffres) ; doit être unique parmi tous les manuels
    // Peut être null pour les manuels sans ISBN formel
    public string? Isbn { get; set; }

    // Nom de la maison d'édition (ex. : "Nathan", "Hachette Éducation")
    // Longueur maximale : 100 caractères ; peut être null
    public string? MaisonEdition { get; set; }

    // Propriété de navigation : tous les chapitres qui appartiennent à ce manuel
    public virtual ICollection<Chapitre> Chapitres { get; set; } = new List<Chapitre>();

    // Propriété de navigation : tous les enregistrements d'utilisation cours-niveau qui référencent ce manuel
    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
