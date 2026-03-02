// Import base .NET types
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a specific learning objective (visée) within the curriculum.
/// A Visee is classified by:
///   - Its name/type (NomVisee) — the category of objective (e.g., "disciplinaire", "transversale")
///   - Its domain (Domaine) — the broad curriculum area it falls under
///   - Its optional sub-domain (Sousdomaine) — a narrower category within the domain
///   - Its competency (Competence) — the overarching skill it develops
/// Learning objectives can be targeted in planned lesson sessions (SeanceObjectif)
/// and are linked to mastery targets (ViseesMaitriser) via a many-to-many join table.
/// </summary>
public partial class Visee
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdVisee { get; set; }

    // Foreign key to the NomVisee record (the type/label of this learning objective)
    public int IdNomViseeFk { get; set; }

    // Foreign key to the Domaine (curriculum domain) this objective belongs to
    public int IdDomaineFk { get; set; }

    // Foreign key to the Sousdomaine (curriculum sub-domain) for finer classification
    // Optional — may be null if the objective is classified at the domain level only
    public int? IdSousDomaineFk { get; set; }

    // Foreign key to the Competence (broad skill category) this objective falls under
    public int IdCompFk { get; set; }

    // Navigation property: the full Competence record
    public virtual Competence IdCompFkNavigation { get; set; } = null!;

    // Navigation property: the full Domaine record
    public virtual Domaine IdDomaineFkNavigation { get; set; } = null!;

    // Navigation property: the full NomVisee record (objective type label)
    public virtual NomVisee IdNomViseeFkNavigation { get; set; } = null!;

    // Navigation property: the optional Sousdomaine record (nullable — may not be set)
    public virtual Sousdomaine? IdSousDomaineFkNavigation { get; set; }

    // Navigation property: all lesson session records that plan to address this objective
    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    // Navigation property: all mastery targets (ViseesMaitriser) that this objective contributes to.
    // This many-to-many relationship is realised through the "lien_visee_maitrise" join table in the database.
    public virtual ICollection<ViseesMaitriser> IdViseesMaitriserFks { get; set; } = new List<ViseesMaitriser>();
}
