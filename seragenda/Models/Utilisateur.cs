using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Utilisateur
{
    public int IdUser { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? NomComplet { get; set; }

    public string RoleSysteme { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();

    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
