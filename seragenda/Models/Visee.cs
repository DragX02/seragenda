using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Visee
{
    public int IdVisee { get; set; }

    public int IdNomViseeFk { get; set; }

    public int IdDomaineFk { get; set; }

    public int? IdSousDomaineFk { get; set; }

    public int IdCompFk { get; set; }

    public virtual Competence IdCompFkNavigation { get; set; } = null!;

    public virtual Domaine IdDomaineFkNavigation { get; set; } = null!;

    public virtual NomVisee IdNomViseeFkNavigation { get; set; } = null!;

    public virtual Sousdomaine? IdSousDomaineFkNavigation { get; set; }

    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    public virtual ICollection<ViseesMaitriser> IdViseesMaitriserFks { get; set; } = new List<ViseesMaitriser>();
}
