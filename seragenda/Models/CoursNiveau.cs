using System;
using System.Collections.Generic;

namespace seragenda.Models;

public partial class CoursNiveau
{
    public int IdCoursNiveau { get; set; }

    public int IdCoursFk { get; set; }

    public int IdNiveauFk { get; set; }

    public int IdProfFk { get; set; }

    public virtual ICollection<Domaine> Domaines { get; set; } = new List<Domaine>();

    public virtual Cour IdCoursFkNavigation { get; set; } = null!;

    public virtual Niveau IdNiveauFkNavigation { get; set; } = null!;

    public virtual Utilisateur IdProfFkNavigation { get; set; } = null!;

    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();

    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();

    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
