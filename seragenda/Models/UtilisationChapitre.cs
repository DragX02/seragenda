using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class UtilisationChapitre
{
    public int IdUtilisation { get; set; }

    public int IdChapitreFk { get; set; }

    public int IdCoursNiveauFk { get; set; }

    public string? Statut { get; set; }

    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;
}
