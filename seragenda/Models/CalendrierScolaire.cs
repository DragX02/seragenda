using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class CalendrierScolaire
{
    public int IdCalendrier { get; set; }

    public string NomEvenement { get; set; } = null!;

    public DateOnly DateDebut { get; set; }

    public DateOnly DateFin { get; set; }

    public string TypeEvenement { get; set; } = null!;

    public string? AnneeScolaire { get; set; }

    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();
}
