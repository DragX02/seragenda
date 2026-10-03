using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace seragenda.Models;

public partial class Utilisateur
{
    public int IdUser { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? NomComplet { get; set; }

    [Column("nom")]
    public string? Nom { get; set; }

    [Column("prenom")]
    public string? Prenom { get; set; }

    [Column("date_naissance")]
    public DateOnly? DateNaissance { get; set; }

    public string RoleSysteme { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? AuthProvider { get; set; }

    public bool IsConfirmed { get; set; }

    public string? ConfirmationToken { get; set; }

    public DateTime? ConfirmationTokenExpiresAt { get; set; }

    public string? ResetToken { get; set; }

    public DateTime? ResetTokenExpiresAt { get; set; }

    public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();

    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
