// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a sub-domain within a pedagogical domain (Domaine).
/// Sub-domains provide a finer level of curriculum organisation below domains.
/// For example, the domain "Algebra" might contain sub-domains like
/// "Linear equations" and "Quadratic equations".
/// Learning objectives (Visee) can be linked to a sub-domain for precise classification.
/// Maximum name length: 50 characters (database constraint, column "nom_comp").
/// </summary>
public partial class Sousdomaine
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdSousDomaine { get; set; }

    // Display name / short description of the sub-domain (stored in column "nom_comp")
    // Maximum length: 50 characters
    public string NomComp { get; set; } = null!;

    // Foreign key to the parent Domaine record
    public int IdDomFk { get; set; }

    // Navigation property: the full Domaine record that contains this sub-domain
    public virtual Domaine IdDomFkNavigation { get; set; } = null!;

    // Navigation property: all learning objectives (visées) that belong to this sub-domain
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
