using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Sousdomaine
{
    public int IdSousDomaine { get; set; }

    public string NomComp { get; set; } = null!;

    public int IdDomFk { get; set; }

    public virtual Domaine IdDomFkNavigation { get; set; } = null!;

    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
