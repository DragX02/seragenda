// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Join table that links a mastery target (ViseesMaitriser) to both
/// a specific competency (Competence) and an optional aptitude (Aptitude).
/// This three-way association models the relationship:
/// "This mastery target involves a particular competency and, optionally, a specific aptitude."
/// Used in the curriculum planning layer to map educational goals to skills.
/// </summary>
public partial class AppartenirViseeAptitude
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdAppartenirViseeAptitude { get; set; }

    // Foreign key to the Aptitude table (optional — a mastery target may have no aptitude)
    public int? IdAptitudeFk { get; set; }

    // Foreign key to the ViseesMaitriser table; links this record to a mastery target
    public int IdViseesMaitriserFk { get; set; }

    // Foreign key to the Competence table; links this record to a required competency
    public int IdCompetenceFk { get; set; }

    // Navigation property: the optional aptitude associated with this mastery-target link
    // Nullable because IdAptitudeFk is optional
    public virtual Aptitude? IdAptitudeFkNavigation { get; set; }

    // Navigation property: the competency associated with this mastery-target link
    public virtual Competence IdCompetenceFkNavigation { get; set; } = null!;

    // Navigation property: the mastery target (visée à maîtriser) that this record describes
    public virtual ViseesMaitriser IdViseesMaitriserFkNavigation { get; set; } = null!;
}
