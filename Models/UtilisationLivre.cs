// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Enregistre quels manuels scolaires sont utilisés (ou recommandés) pour une combinaison cours-niveau spécifique.
// Un enregistrement UtilisationLivre relie un Livre (manuel) à un CoursNiveau (matière + niveau)
// et capture le statut d'utilisation (ex. : "Recommandé", "Obligatoire").
// Cela permet aux responsables du programme de suivre quels manuels chaque enseignant utilise pour chaque classe.
// Le statut par défaut en base de données est "Recommandé" lors de l'insertion d'un nouvel enregistrement.
public partial class UtilisationLivre
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdUtilisation { get; set; }

    // Clé étrangère vers le Livre (manuel scolaire) référencé
    public int IdLivreFk { get; set; }

    // Clé étrangère vers le CoursNiveau (combinaison matière + niveau) pour lequel ce manuel est utilisé
    public int IdCoursNiveauFk { get; set; }

    // Descripteur du statut d'utilisation ; par défaut "Recommandé" en base de données
    // Valeurs possibles : "Recommandé", "Obligatoire", "Optionnel", etc.
    // Peut être null si aucun statut n'a été explicitement défini
    public string? Statut { get; set; }

    // Propriété de navigation : l'enregistrement CoursNiveau complet (matière + niveau + enseignant) pour cette utilisation
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Livre (manuel scolaire) complet référencé
    public virtual Livre IdLivreFkNavigation { get; set; } = null!;
}
