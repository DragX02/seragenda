using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class NomVisee
{
    public int IdNomVisee { get; set; }

    public string NomVisee1 { get; set; } = null!;

    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
