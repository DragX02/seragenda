using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Domaine
{
    public int IdDom { get; set; }

    public string Nom { get; set; } = null!;

    public int IdCoursNiveauFk { get; set; }

    public virtual CoursNiveau IdCoursNiveauFkNavigation { get; set; } = null!;

    public virtual ICollection<Sousdomaine> Sousdomaines { get; set; } = new List<Sousdomaine>();

    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
