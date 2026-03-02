// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a mastery target — a high-level educational outcome that students are expected to achieve.
/// ViseesMaitriser records are part of the official curriculum framework (e.g., Belgian CPC or similar).
/// A mastery target can be linked to:
///   - Multiple learning objectives (Visee) via the "lien_visee_maitrise" many-to-many join table
///   - Multiple competency/aptitude combinations via AppartenirViseeAptitude
/// These records are reference data managed by curriculum administrators,
/// not created by individual teachers.
/// The ViseesMaitriserController exposes this table with pagination support.
/// </summary>
public partial class ViseesMaitriser
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdViseesMaitriser { get; set; }

    // Full name/description of the mastery target (text column, no explicit max length in the model)
    // Example: "Lire, comprendre et exploiter des textes variés"
    public string NomViseesMaitriser { get; set; } = null!;

    // Navigation property: all mastery-target/competency/aptitude links associated with this record
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    // Navigation property: all learning objectives (Visee) linked to this mastery target
    // via the "lien_visee_maitrise" many-to-many join table
    public virtual ICollection<Visee> IdViseeFks { get; set; } = new List<Visee>();
}
