// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a specific cognitive or pedagogical aptitude in the curriculum model.
/// An Aptitude is a fine-grained observable skill or behaviour
/// (e.g., "to summarise a text", "to solve a linear equation").
/// Aptitudes are linked to mastery targets (ViseesMaitriser) and competencies
/// through the AppartenirViseeAptitude join table.
/// </summary>
public partial class Aptitude
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdAptitude { get; set; }

    // Descriptive name of the aptitude (e.g., "Lire à voix haute", "Résoudre une équation")
    // Maximum length is 50 characters as defined in the database schema
    public string NomAptitude { get; set; } = null!;

    // Navigation property: all mastery-target/competency links that reference this aptitude
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();
}
