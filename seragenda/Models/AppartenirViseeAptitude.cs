// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Table de jointure reliant une visée à maîtriser (ViseesMaitriser) à la fois
// à une compétence spécifique (Competence) et à une aptitude optionnelle (Aptitude).
// Cette association trilatérale modélise la relation :
// "Cette visée à maîtriser implique une compétence particulière et, optionnellement, une aptitude spécifique."
// Utilisée dans la couche de planification pédagogique pour associer les objectifs éducatifs aux savoir-faire.
public partial class AppartenirViseeAptitude
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdAppartenirViseeAptitude { get; set; }

    // Clé étrangère vers la table Aptitude (optionnelle — une visée à maîtriser peut ne pas avoir d'aptitude)
    public int? IdAptitudeFk { get; set; }

    // Clé étrangère vers la table ViseesMaitriser ; relie cet enregistrement à une visée à maîtriser
    public int IdViseesMaitriserFk { get; set; }

    // Clé étrangère vers la table Competence ; relie cet enregistrement à une compétence requise
    public int IdCompetenceFk { get; set; }

    // Propriété de navigation : l'aptitude optionnelle associée à ce lien de visée à maîtriser
    // Nullable car IdAptitudeFk est optionnel
    public virtual Aptitude? IdAptitudeFkNavigation { get; set; }

    // Propriété de navigation : la compétence associée à ce lien de visée à maîtriser
    public virtual Competence IdCompetenceFkNavigation { get; set; } = null!;

    // Propriété de navigation : la visée à maîtriser que cet enregistrement décrit
    public virtual ViseesMaitriser IdViseesMaitriserFkNavigation { get; set; } = null!;
}
