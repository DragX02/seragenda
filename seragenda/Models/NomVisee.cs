// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents the name or label of a learning objective type (visée).
/// NomVisee acts as a label dictionary for the Visee table:
/// instead of repeating the same long label string in every Visee row,
/// the label is stored once here and referenced by foreign key.
/// Example labels: "Visée disciplinaire", "Visée transversale".
/// Maximum label length: 150 characters (database constraint).
/// </summary>
public partial class NomVisee
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdNomVisee { get; set; }

    // The label text for this learning objective type
    // Named with a "1" suffix (NomVisee1) to avoid naming collision with the class itself
    // Maximum length: 150 characters
    public string NomVisee1 { get; set; } = null!;

    // Navigation property: all learning objective records that use this label
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
