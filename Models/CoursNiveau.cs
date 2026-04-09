// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Table de liaison qui associe une matière (Cour) à un niveau d'enseignement (Niveau)
// tel qu'enseigné par un enseignant spécifique (Utilisateur).
// Cette relation trilatérale est le pivot central du modèle de planification des leçons :
// tous les domaines curriculaires, les usages de livres, les usages de chapitres et les séances de cours en dépendent.
// Une contrainte unique en base de données empêche le même enseignant d'être enregistré deux fois
// pour la même combinaison matière-niveau.
public partial class CoursNiveau
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdCoursNiveau { get; set; }

    // Clé étrangère vers l'enregistrement Cour (matière)
    public int IdCoursFk { get; set; }

    // Clé étrangère vers l'enregistrement Niveau (niveau d'enseignement)
    public int IdNiveauFk { get; set; }

    // Clé étrangère vers l'Utilisateur (enseignant) qui enseigne cette matière à ce niveau
    public int IdProfFk { get; set; }

    // Propriété de navigation : l'ensemble des domaines pédagogiques définis pour cette combinaison matière-niveau
    public virtual ICollection<Domaine> Domaines { get; set; } = new List<Domaine>();

    // Propriété de navigation : l'enregistrement Cour complet pour la matière associée
    public virtual Cour IdCoursFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Niveau complet pour le niveau d'enseignement associé
    public virtual Niveau IdNiveauFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Utilisateur complet pour l'enseignant associé
    public virtual Utilisateur IdProfFkNavigation { get; set; } = null!;

    // Propriété de navigation : tous les enregistrements de planification de leçons (séances) pour cette combinaison cours-niveau
    public virtual ICollection<Planification> Planifications { get; set; } = new List<Planification>();

    // Propriété de navigation : tous les enregistrements d'utilisation de chapitre pour cette combinaison cours-niveau
    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();

    // Propriété de navigation : tous les enregistrements d'utilisation de livre pour cette combinaison cours-niveau
    public virtual ICollection<UtilisationLivre> UtilisationLivres { get; set; } = new List<UtilisationLivre>();
}
