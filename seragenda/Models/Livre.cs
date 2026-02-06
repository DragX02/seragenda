using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Livre
{
    public int IdLivre { get; set; }

    public string TitreLivre { get; set; } = null!;

    public string? Auteur { get; set; }

    public string? Isbn { get; set; }

    public string? MaisonEdition { get; set; }

    public virtual ICollection<Chapitre> Chapitres { get; set; } = new List<Chapitre>();

    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
