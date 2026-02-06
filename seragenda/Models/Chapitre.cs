using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class Chapitre
{
    public int IdChapitre { get; set; }

    public int IdLivreFk { get; set; }

    public int NumeroChapitre { get; set; }

    public string TitreChapitre { get; set; } = null!;

    public int? PageDebut { get; set; }

    public virtual Livre IdLivreFkNavigation { get; set; } = null!;

    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();

    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();
}
