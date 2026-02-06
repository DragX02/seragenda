using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class SeanceRessource
{
    public int IdSeanceRes { get; set; }

    public int IdPlanningFk { get; set; }

    public int IdChapitreFk { get; set; }

    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;
}
