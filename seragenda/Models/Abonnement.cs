using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Abonnement
{
    public int IdAbonnement { get; set; }

    public int IdUserFk { get; set; }

    public DateOnly DateDebut { get; set; }

    public DateOnly DateFin { get; set; }

    public string? TypeAbo { get; set; }

    public string? Statut { get; set; }

    public virtual Utilisateur IdUserFkNavigation { get; set; } = null!;
}
