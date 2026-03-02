// Import base .NET types (DateOnly, etc.)
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a school calendar event, such as a holiday period, a public holiday,
/// or a back-to-school day.
/// Records are populated by the <see cref="Services.ScolaireScraper"/> from the
/// official Belgian education website (enseignement.be).
/// The AnneeScolaire field is a database-computed column derived from the DateDebut:
///   - If month >= 8 (August) → "YYYY-(YYYY+1)" (e.g., "2024-2025")
///   - Otherwise              → "(YYYY-1)-YYYY"  (e.g., "2024-2025" for January 2025)
/// </summary>
public partial class CalendrierScolaire
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdCalendrier { get; set; }

    // Name of the event (e.g., "Vacances de Noël", "Jour de l'An", "Rentrée scolaire")
    // Maximum length: 100 characters
    public string NomEvenement { get; set; } = null!;

    // First day of the event period (inclusive)
    public DateOnly DateDebut { get; set; }

    // Last day of the event period (inclusive); same as DateDebut for single-day events
    public DateOnly DateFin { get; set; }

    // Category of the event: "Vacances" for holiday periods, "Jour Férié/Rentrée" for single days
    // Maximum length: 50 characters
    public string TypeEvenement { get; set; } = null!;

    // Database-computed column representing the school year this event belongs to
    // (e.g., "2024-2025"); computed server-side by a SQL CASE expression — do not set manually
    public string? AnneeScolaire { get; set; }

    // Navigation property: all lesson planning sessions that reference this calendar event
    // (used when a session is linked to a specific calendar day/holiday)
    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();
}
