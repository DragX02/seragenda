// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a physical or digital textbook in the resource catalogue.
/// Livres (books) are linked to course-level combinations via the UtilisationLivre table
/// (indicating which books are recommended or required for a given subject and level).
/// Books contain chapters (Chapitre) that can be individually referenced in lesson sessions.
/// ISBN has a unique database index to prevent duplicate book entries.
/// </summary>
public partial class Livre
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdLivre { get; set; }

    // Full title of the book (e.g., "Mathématiques 4e — Livre de l'élève")
    // Maximum length: 255 characters; required (not null)
    public string TitreLivre { get; set; } = null!;

    // Name(s) of the book's author(s); may be null if unknown
    // Maximum length: 150 characters
    public string? Auteur { get; set; }

    // International Standard Book Number (13-digit ISBN); must be unique across all books
    // May be null for books without a formal ISBN
    public string? Isbn { get; set; }

    // Name of the publishing house (e.g., "Nathan", "Hachette Éducation")
    // Maximum length: 100 characters; may be null
    public string? MaisonEdition { get; set; }

    // Navigation property: all chapters that belong to this book
    public virtual ICollection<Chapitre> Chapitres { get; set; } = new List<Chapitre>();

    // Navigation property: all course-level usage records that reference this book
    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
