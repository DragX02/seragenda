using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class UtilisationLivre
{
    public int IdUtilisation { get; set; }

    public int IdLivreFk { get; set; }

    public int IdCoursNiveauFk { get; set; }

    public string? Statut { get; set; }

    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    public virtual Livre IdLivreFkNavigation { get; set; } = null!;
}
