using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class ViseesMaitriser
{
    public int IdViseesMaitriser { get; set; }

    public string NomViseesMaitriser { get; set; } = null!;

    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    public virtual ICollection<Visee> IdViseeFks { get; set; } = new List<Visee>();
}
