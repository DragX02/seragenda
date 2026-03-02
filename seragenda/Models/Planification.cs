// Import base .NET types (DateOnly, TimeOnly, etc.)
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a planned lesson session in the teacher's agenda.
/// A Planification links a specific date and time slot to a course-level combination (CoursNiveau)
/// and optionally to a school calendar event (CalendrierScolaire) such as a holiday.
/// Each session can have multiple associated learning objectives (SeanceObjectif)
/// and multiple resource references (SeanceRessource — chapters used during the session).
/// The session status defaults to "Prévue" (planned) and can progress to other states
/// (e.g., "Réalisée", "Annulée").
/// </summary>
public partial class Planification
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdPlanning { get; set; }

    // The calendar date on which this lesson session is scheduled
    public DateOnly DateSeance { get; set; }

    // Optional start time of the lesson session (e.g., 08:30)
    // Null when the time is not yet determined
    public TimeOnly? HeureDebut { get; set; }

    // Optional end time of the lesson session (e.g., 10:00)
    // Null when the time is not yet determined
    public TimeOnly? HeureFin { get; set; }

    // Foreign key to the CoursNiveau record; identifies which subject-level this session belongs to
    public int IdCoursNiveauFk { get; set; }

    // Optional foreign key to a CalendrierScolaire record (e.g., if the session falls on a noted calendar day)
    // Null when the session is not associated with a specific school calendar event
    public int? IdCalendrierFk { get; set; }

    // Optional private note written by the teacher about this session (free text)
    public string? NoteProf { get; set; }

    // Current status of the session; defaults to "Prévue" in the database
    // Possible values include: "Prévue", "Réalisée", "Annulée"
    public string? Statut { get; set; }

    // Navigation property: the optional school calendar event associated with this session
    public virtual CalendrierScolaire? IdCalendrierFkNavigation { get; set; }

    // Navigation property: the full CoursNiveau record (subject + level + teacher) for this session
    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    // Navigation property: all learning objectives planned for this session
    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    // Navigation property: all book chapter resources planned for use during this session
    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();
}
