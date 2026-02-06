using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Cour
{
    public int IdCours { get; set; }

    public string NomCours { get; set; } = null!;

    public string CodeCours { get; set; } = null!;

    public string PrefixCours { get; set; } = null!;

    public string CouleurAgenda { get; set; } = null!;

    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
