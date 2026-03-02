// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a pedagogical domain within a specific subject-level combination (CoursNiveau).
/// A domain is a broad curriculum area used to organise learning objectives.
/// For example, "Algebra" or "Geometry" might be domains within "Mathematics at 3rd year level".
/// Domains can contain sub-domains (Sousdomaine) and are linked to learning objectives (Visee).
/// </summary>
public partial class Domaine
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdDom { get; set; }

    // Display name of the domain (e.g., "Algèbre", "Géométrie", "Lecture")
    public string Nom { get; set; } = null!;

    // Foreign key to the CoursNiveau record this domain belongs to
    // (the specific subject + level combination that contains this domain)
    public int IdCoursNiveauFk { get; set; }

    // Navigation property: the full CoursNiveau record this domain is part of
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Navigation property: all sub-domain records that refine this domain further
    public virtual ICollection<Sousdomaine> Sousdomaines { get; set; } = new List<Sousdomaine>();

    // Navigation property: all learning objectives (visées) that belong to this domain
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
