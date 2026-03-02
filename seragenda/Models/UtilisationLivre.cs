// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Records which textbooks are used (or recommended) for a specific course-level combination.
/// A single UtilisationLivre entry links a Livre (book) to a CoursNiveau (subject + level)
/// and captures the usage status (e.g., "Recommandé", "Obligatoire").
/// This allows curriculum managers to track which books each teacher uses for each class.
/// The database default status is "Recommandé" when a new record is inserted.
/// </summary>
public partial class UtilisationLivre
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdUtilisation { get; set; }

    // Foreign key to the Livre (textbook) being referenced
    public int IdLivreFk { get; set; }

    // Foreign key to the CoursNiveau (subject + level combination) this book is used for
    public int IdCoursNiveauFk { get; set; }

    // Usage status descriptor; defaults to "Recommandé" in the database
    // Possible values: "Recommandé", "Obligatoire", "Optionnel", etc.
    // May be null if no status has been explicitly set
    public string? Statut { get; set; }

    // Navigation property: the full CoursNiveau record (subject + level + teacher) for this usage
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Navigation property: the full Livre (book) record being referenced
    public virtual Livre IdLivreFkNavigation { get; set; } = null!;
}
