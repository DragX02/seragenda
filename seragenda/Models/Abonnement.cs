// Import base .NET types (DateOnly, etc.)
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a subscription period for a user account.
/// An Abonnement tracks the type, start/end dates, and active status of a user's access subscription.
/// A user may have multiple subscription records over time (e.g., trial → premium → renewal).
/// The database default status is "Actif" when a new subscription is created.
/// </summary>
public partial class Abonnement
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdAbonnement { get; set; }

    // Foreign key to the Utilisateur table; identifies which user this subscription belongs to
    public int IdUserFk { get; set; }

    // The first day on which this subscription is active (inclusive)
    public DateOnly DateDebut { get; set; }

    // The last day on which this subscription is active (inclusive)
    public DateOnly DateFin { get; set; }

    // Subscription tier or type descriptor (e.g., "Premium", "Trial", "Annuel")
    // May be null if the type has not been assigned
    public string? TypeAbo { get; set; }

    // Current status of the subscription (e.g., "Actif", "Expiré", "Annulé")
    // Defaults to "Actif" in the database when a new record is inserted
    public string? Statut { get; set; }

    // Navigation property: the full Utilisateur record of the subscription's owner
    public virtual Utilisateur IdUserFkNavigation { get; set; } = null!;
}
