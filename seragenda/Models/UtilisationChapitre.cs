// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Records which book chapters are used (or recommended) for a specific course-level combination.
/// A single UtilisationChapitre entry links a Chapitre (chapter) to a CoursNiveau (subject + level)
/// and captures the usage status (e.g., "Recommandé", "Obligatoire").
/// This allows teachers to see which chapters of their textbooks are relevant for each class.
/// The database default status is "Recommandé" when a new record is created.
/// </summary>
public partial class UtilisationChapitre
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdUtilisation { get; set; }

    // Foreign key to the Chapitre being referenced
    public int IdChapitreFk { get; set; }

    // Foreign key to the CoursNiveau (subject + level combination) this chapter is used for
    public int IdCoursNiveauFk { get; set; }

    // Usage status descriptor; defaults to "Recommandé" in the database
    // Possible values: "Recommandé", "Obligatoire", "Optionnel", etc.
    // May be null if no status has been explicitly set
    public string? Statut { get; set; }

    // Navigation property: the full Chapitre record being referenced
    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    // Navigation property: the full CoursNiveau record (subject + level + teacher) for this usage
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;
}
