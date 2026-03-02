// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Linking table that associates a subject (Cour) with an education level (Niveau)
/// as taught by a specific teacher (Utilisateur).
/// This three-way relationship is the central pivot of the lesson planning model:
/// all curriculum domains, book usage, chapter usage, and lesson sessions hang off it.
/// A unique database constraint prevents the same teacher from being listed twice
/// for the same subject-level combination.
/// </summary>
public partial class CoursNiveau
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdCoursNiveau { get; set; }

    // Foreign key pointing to the Cour (subject) record
    public int IdCoursFk { get; set; }

    // Foreign key pointing to the Niveau (education level) record
    public int IdNiveauFk { get; set; }

    // Foreign key pointing to the Utilisateur (teacher) who teaches this subject at this level
    public int IdProfFk { get; set; }

    // Navigation property: the set of pedagogical domains defined for this subject-level combination
    public virtual ICollection<Domaine> Domaines { get; set; } = new List<Domaine>();

    // Navigation property: the full Cour record for the associated subject
    public virtual Cour IdCoursFkNavigation { get; set; } = null!;

    // Navigation property: the full Niveau record for the associated education level
    public virtual Niveau IdNiveauFkNavigation { get; set; } = null!;

    // Navigation property: the full Utilisateur record for the associated teacher
    public virtual Utilisateur IdProfFkNavigation { get; set; } = null!;

    // Navigation property: all lesson planning records (sessions) for this course-level combination
    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();

    // Navigation property: all chapter usage records for this course-level combination
    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();

    // Navigation property: all book usage records for this course-level combination
    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
