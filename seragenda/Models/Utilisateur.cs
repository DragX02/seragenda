// Import base .NET types (DateTime, DateOnly, etc.)
using System;
// Import collection interfaces for navigation properties
using System.Collections.Generic;
// Import DataAnnotations for column mapping overrides
using System.ComponentModel.DataAnnotations.Schema;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a registered user account in the application.
/// Users are typically teachers ("PROF") or administrators ("ADMIN").
/// Accounts can be created locally (email + password) or via OAuth (Google, Microsoft).
/// Local accounts require email confirmation before login is permitted.
/// </summary>
public partial class Utilisateur
{
    // Primary key — auto-incremented integer assigned by the database
    public int IdUser { get; set; }

    // The user's email address; used as the login identifier and must be unique across all accounts
    public string Email { get; set; } = null!;

    // BCrypt hash of the user's password; the plaintext password is never stored
    // For OAuth accounts, this contains a random unusable hash
    public string PasswordHash { get; set; } = null!;

    // Computed or legacy full-name field stored in the "nom_complet" database column
    // Prefer using Nom + Prenom separately; this field may be null for newer accounts
    public string? NomComplet { get; set; }

    // User's family name (last name), mapped to the "nom" column
    [Column("nom")]
    public string? Nom { get; set; }

    // User's given name (first name), mapped to the "prenom" column
    [Column("prenom")]
    public string? Prenom { get; set; }

    // Optional date of birth, mapped to the "date_naissance" column
    [Column("date_naissance")]
    public DateOnly? DateNaissance { get; set; }

    // System role controlling access level; valid values are "PROF" (default) and "ADMIN"
    public string RoleSysteme { get; set; } = null!;

    // UTC timestamp of when the account was created; defaults to CURRENT_TIMESTAMP in the database
    public DateTime? CreatedAt { get; set; }

    // Name of the OAuth provider used to create the account ("Google", "Microsoft"), or null for local accounts
    // Note: columns auth_provider, is_confirmed, nom, prenom, date_naissance must be added via ALTER TABLE
    // if migrating from an older schema version (see migration notes)
    public string? AuthProvider { get; set; }

    // Indicates whether the user has confirmed their email address.
    // Local accounts start as false and are set to true after clicking the confirmation link.
    // OAuth accounts are confirmed immediately (the provider has already verified the email).
    public bool IsConfirmed { get; set; }

    // One-time token sent via email to confirm account ownership; cleared after use
    public string? ConfirmationToken { get; set; }

    // UTC expiry date/time for the confirmation token; the token is invalid after this point
    public DateTime? ConfirmationTokenExpiresAt { get; set; }

    // Navigation property: all subscription records associated with this user
    public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();

    // Navigation property: all course-level teaching assignments for this user (as a teacher)
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
