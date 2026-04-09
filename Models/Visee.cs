// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un objectif d'apprentissage spécifique (visée) dans le programme scolaire.
// Une Visee est classifiée par :
//   - Son nom/type (NomVisee) — la catégorie d'objectif (ex. : "disciplinaire", "transversale")
//   - Son domaine (Domaine) — le large domaine curriculaire dans lequel elle s'inscrit
//   - Son sous-domaine optionnel (Sousdomaine) — une catégorie plus étroite au sein du domaine
//   - Sa compétence (Competence) — la compétence globale qu'elle développe
// Les objectifs d'apprentissage peuvent être ciblés dans des séances de cours planifiées (SeanceObjectif)
// et sont liés aux visées à maîtriser (ViseesMaitriser) via une table de jointure plusieurs-à-plusieurs.
public partial class Visee
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdVisee { get; set; }

    // Clé étrangère vers l'enregistrement NomVisee (le type/l'étiquette de cet objectif d'apprentissage)
    public int IdNomViseeFk { get; set; }

    // Clé étrangère vers le Domaine (domaine curriculaire) auquel appartient cet objectif
    public int IdDomaineFk { get; set; }

    // Clé étrangère vers le Sousdomaine (sous-domaine curriculaire) pour une classification plus fine
    // Optionnel — peut être null si l'objectif est classifié uniquement au niveau du domaine
    public int? IdSousDomaineFk { get; set; }

    // Clé étrangère vers la Competence (catégorie de savoir-faire générale) dans laquelle s'inscrit cet objectif
    public int IdCompFk { get; set; }

    // Propriété de navigation : l'enregistrement Competence complet
    public virtual Competence IdCompFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Domaine complet
    public virtual Domaine IdDomaineFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement NomVisee complet (étiquette de type d'objectif)
    public virtual NomVisee IdNomViseeFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Sousdomaine optionnel (nullable — peut ne pas être défini)
    public virtual Sousdomaine? IdSousDomaineFkNavigation { get; set; }

    // Propriété de navigation : tous les enregistrements de séance qui prévoient de traiter cet objectif
    public virtual ICollection<SeanceObjectif> SeanceObjectifs { get; set; } = new List<SeanceObjectif>();

    // Propriété de navigation : toutes les visées à maîtriser (ViseesMaitriser) auxquelles cet objectif contribue.
    // Cette relation plusieurs-à-plusieurs est réalisée via la table de jointure "lien_visee_maitrise" en base de données.
    public virtual ICollection<ViseesMaitriser> IdViseesMaitriserFks { get; set; } = new List<ViseesMaitriser>();
}
