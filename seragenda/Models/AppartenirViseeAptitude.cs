using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class AppartenirViseeAptitude
{
    public int IdAppartenirViseeAptitude { get; set; }

    public int? IdAptitudeFk { get; set; }

    public int IdViseesMaitriserFk { get; set; }

    public int IdCompetenceFk { get; set; }

    public virtual Aptitude? IdAptitudeFkNavigation { get; set; }

    public virtual Competence IdCompetenceFkNavigation { get; set; } = null!;

    public virtual ViseesMaitriser IdViseesMaitriserFkNavigation { get; set; } = null!;
}
