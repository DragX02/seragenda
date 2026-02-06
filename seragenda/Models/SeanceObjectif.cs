using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class SeanceObjectif
{
    public int IdSeanceObj { get; set; }

    public int IdPlanningFk { get; set; }

    public int IdViseeFk { get; set; }

    public bool? EvaluationPrevue { get; set; }

    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;

    public virtual Visee IdViseeFkNavigation { get; set; } = null!;
}
