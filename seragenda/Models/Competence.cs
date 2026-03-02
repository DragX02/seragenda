// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a broad competency in the curriculum framework.
/// A Competence is a high-level skill area that groups related learning objectives (Visee)
/// and mastery target/aptitude links (AppartenirViseeAptitude).
/// Example competencies: "Communiquer", "Résoudre des problèmes", "Analyser".
/// Maximum name length: 50 characters (database constraint).
/// </summary>
public partial class Competence
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdCompetence { get; set; }

    // Display name of the competency (e.g., "Communiquer", "Résoudre", "Analyser")
    // Maximum length: 50 characters
    public string NomCompetence { get; set; } = null!;

    // Navigation property: all mastery-target/aptitude links that involve this competency
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    // Navigation property: all learning objectives (visées) classified under this competency
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
