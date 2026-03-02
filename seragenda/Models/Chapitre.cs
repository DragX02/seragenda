// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a single chapter within a textbook (Livre).
/// Teachers can reference specific chapters when recording which book sections
/// are used during a lesson session (via SeanceRessource).
/// Chapters can also appear in course-level chapter usage records (UtilisationChapitre).
/// </summary>
public partial class Chapitre
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdChapitre { get; set; }

    // Foreign key to the Livre (book) this chapter belongs to
    public int IdLivreFk { get; set; }

    // Sequential chapter number within the book (e.g., 1, 2, 3, ...)
    public int NumeroChapitre { get; set; }

    // Full title of the chapter (e.g., "Les équations du premier degré")
    // Maximum length: 255 characters
    public string TitreChapitre { get; set; } = null!;

    // Optional starting page number of the chapter in the physical book
    // Null when the page number is unknown or not applicable
    public int? PageDebut { get; set; }

    // Navigation property: the full Livre record this chapter belongs to
    public virtual Livre IdLivreFkNavigation { get; set; } = null!;

    // Navigation property: all lesson session resource records that reference this chapter
    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();

    // Navigation property: all course-level chapter usage records that reference this chapter
    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();
}
