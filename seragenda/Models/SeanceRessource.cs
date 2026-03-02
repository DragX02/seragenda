// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Links a book chapter (Chapitre) to a planned lesson session (Planification),
/// recording which chapter(s) will be used as instructional resources during the session.
/// A single session can reference multiple chapters, and the same chapter can appear
/// across multiple sessions.
/// This is a pure join table with its own surrogate primary key for easier management.
/// </summary>
public partial class SeanceRessource
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdSeanceRes { get; set; }

    // Foreign key to the Planification (lesson session) that uses this resource
    public int IdPlanningFk { get; set; }

    // Foreign key to the Chapitre (book chapter) being referenced as a resource
    public int IdChapitreFk { get; set; }

    // Navigation property: the full Chapitre record for the referenced book chapter
    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    // Navigation property: the full Planification (lesson session) record
    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;
}
