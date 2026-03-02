// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Links a specific learning objective (Visee) to a planned lesson session (Planification).
/// Each SeanceObjectif record means: "During this session, the teacher plans to address this objective."
/// An optional flag indicates whether a formal evaluation (test, quiz, etc.) is planned
/// for this objective during the session.
/// A single session can target multiple objectives, and the same objective can appear
/// across multiple sessions.
/// </summary>
public partial class SeanceObjectif
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdSeanceObj { get; set; }

    // Foreign key to the Planification (lesson session) this objective is associated with
    public int IdPlanningFk { get; set; }

    // Foreign key to the Visee (learning objective) being addressed in this session
    public int IdViseeFk { get; set; }

    // Indicates whether a formal evaluation of this objective is planned for this session.
    // Null defaults to false in the database.
    // True = teacher plans to test or formally evaluate this objective during the session.
    public bool? EvaluationPrevue { get; set; }

    // Navigation property: the full Planification (lesson session) record
    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;

    // Navigation property: the full Visee (learning objective) record
    public virtual Visee IdViseeFkNavigation { get; set; } = null!;
}
