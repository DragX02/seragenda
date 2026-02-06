using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Niveau
{
    public int IdNiveau { get; set; }

    public string CodeNiveau { get; set; } = null!;

    public string NomNiveau { get; set; } = null!;

    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
