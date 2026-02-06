using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Planification
{
    public int IdPlanning { get; set; }

    public DateOnly DateSeance { get; set; }

    public TimeOnly? HeureDebut { get; set; }

    public TimeOnly? HeureFin { get; set; }

    public int IdCoursNiveauFk { get; set; }

    public int? IdCalendrierFk { get; set; }

    public string? NoteProf { get; set; }

    public string? Statut { get; set; }

    public virtual CalendrierScolaire? IdCalendrierFkNavigation { get; set; }

    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();
}
