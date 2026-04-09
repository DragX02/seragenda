// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Relie un objectif d'apprentissage spécifique (Visee) à une séance de cours planifiée (Planification).
// Chaque enregistrement SeanceObjectif signifie : "Lors de cette séance, l'enseignant prévoit de traiter cet objectif."
// Un indicateur optionnel indique si une évaluation formelle (interrogation, quiz, etc.) est prévue
// pour cet objectif pendant la séance.
// Une même séance peut cibler plusieurs objectifs, et le même objectif peut apparaître
// dans plusieurs séances.
public partial class SeanceObjectif
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdSeanceObj { get; set; }

    // Clé étrangère vers la Planification (séance de cours) à laquelle cet objectif est associé
    public int IdPlanningFk { get; set; }

    // Clé étrangère vers la Visee (objectif d'apprentissage) abordée dans cette séance
    public int IdViseeFk { get; set; }

    // Indique si une évaluation formelle de cet objectif est prévue pour cette séance.
    // Null par défaut équivaut à false en base de données.
    // True = l'enseignant prévoit d'évaluer ou de tester formellement cet objectif pendant la séance.
    public bool? EvaluationPrevue { get; set; }

    // Propriété de navigation : l'enregistrement Planification (séance de cours) complet
    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Visee (objectif d'apprentissage) complet
    public virtual Visee IdViseeFkNavigation { get; set; } = null!;
}
