using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Competence
{
    public int IdCompetence { get; set; }

    public string NomCompetence { get; set; } = null!;

    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
