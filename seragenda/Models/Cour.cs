// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a subject (course) in the curriculum catalogue.
/// A Cour record identifies a teachable subject such as Mathematics, French, or Biology.
/// Each subject has a unique short code used in URL parameters and data lookups.
/// Subjects can be taught at multiple education levels, represented via the CoursNiveau linking table.
/// </summary>
public partial class Cour
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdCours { get; set; }

    // Full display name of the subject (e.g., "Mathématiques", "Français")
    public string NomCours { get; set; } = null!;

    // Short unique code for the subject used in API routes and internal lookups (e.g., "MATH", "FR")
    // Has a unique database index to enforce code uniqueness
    public string CodeCours { get; set; } = null!;

    // Short prefix string used for generating lesson or chapter identifiers (e.g., "MAT", "FRA")
    public string PrefixCours { get; set; } = null!;

    // CSS-compatible hex colour string used to render this subject on the agenda (e.g., "#3B82F6")
    public string CouleurAgenda { get; set; } = null!;

    // Navigation property: all course-level combinations where this subject is taught
    // (each CoursNiveau entry links this subject to a specific education level and teacher)
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
