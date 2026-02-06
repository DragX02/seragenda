using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Aptitude
{
    public int IdAptitude { get; set; }

    public string NomAptitude { get; set; } = null!;

    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();
}
