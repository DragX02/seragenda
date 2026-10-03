using System.Collections.Generic;

namespace seragenda.Models;

public partial class CategorieCours
{
    public int IdCat { get; set; }

    public string NomCat { get; set; } = null!;

    public int Ordre { get; set; }

    public virtual ICollection<Cour> Cours { get; set; } = new List<Cour>();
}
