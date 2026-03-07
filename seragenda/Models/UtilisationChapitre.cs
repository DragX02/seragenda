// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Enregistre quels chapitres de manuels sont utilisés (ou recommandés) pour une combinaison cours-niveau spécifique.
// Un enregistrement UtilisationChapitre relie un Chapitre (chapitre) à un CoursNiveau (matière + niveau)
// et capture le statut d'utilisation (ex. : "Recommandé", "Obligatoire").
// Cela permet aux enseignants de voir quels chapitres de leurs manuels sont pertinents pour chaque classe.
// Le statut par défaut en base de données est "Recommandé" lors de la création d'un nouvel enregistrement.
public partial class UtilisationChapitre
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdUtilisation { get; set; }

    // Clé étrangère vers le Chapitre référencé
    public int IdChapitreFk { get; set; }

    // Clé étrangère vers le CoursNiveau (combinaison matière + niveau) pour lequel ce chapitre est utilisé
    public int IdCoursNiveauFk { get; set; }

    // Descripteur du statut d'utilisation ; par défaut "Recommandé" en base de données
    // Valeurs possibles : "Recommandé", "Obligatoire", "Optionnel", etc.
    // Peut être null si aucun statut n'a été explicitement défini
    public string? Statut { get; set; }

    // Propriété de navigation : l'enregistrement Chapitre complet référencé
    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement CoursNiveau complet (matière + niveau + enseignant) pour cette utilisation
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;
}
