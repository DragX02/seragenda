// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents an educational level (year group or grade) in the curriculum structure.
/// A Niveau groups students of the same academic year (e.g., "1A", "3B", "6TH").
/// It is linked to subjects via the CoursNiveau joining table.
/// Each level has a unique short code and a display name.
/// </summary>
public partial class Niveau
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdNiveau { get; set; }

    // Short unique code for the level, used in API parameters and internal lookups (e.g., "1A", "3B")
    // Has a unique database index to enforce code uniqueness
    public string CodeNiveau { get; set; } = null!;

    // Full human-readable name of the educational level (e.g., "Première A", "Troisième B")
    public string NomNiveau { get; set; } = null!;

    // Navigation property: all course-level combinations that reference this education level
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
