// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une visée à maîtriser — un résultat éducatif de haut niveau que les élèves sont censés atteindre.
// Les enregistrements ViseesMaitriser font partie du référentiel officiel du programme scolaire (ex. : CPC belge ou similaire).
// Une visée à maîtriser peut être liée à :
//   - Plusieurs objectifs d'apprentissage (Visee) via la table de jointure plusieurs-à-plusieurs "lien_visee_maitrise"
//   - Plusieurs combinaisons compétence/aptitude via AppartenirViseeAptitude
// Ces enregistrements sont des données de référence gérées par les administrateurs du programme scolaire,
// et non créées par des enseignants individuels.
// Le contrôleur ViseesMaitriserController expose cette table avec support de pagination.
public partial class ViseesMaitriser
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdViseesMaitriser { get; set; }

    // Nom/description complet(e) de la visée à maîtriser (colonne texte, sans longueur maximale explicite dans le modèle)
    // Exemple : "Lire, comprendre et exploiter des textes variés"
    public string NomViseesMaitriser { get; set; } = null!;

    // Propriété de navigation : tous les liens visée à maîtriser / compétence / aptitude associés à cet enregistrement
    public virtual ICollection<AppartenirViseeAptitude> AppartenirViseeAptitudes { get; set; } = new List<AppartenirViseeAptitude>();

    // Propriété de navigation : tous les objectifs d'apprentissage (Visee) liés à cette visée à maîtriser
    // via la table de jointure plusieurs-à-plusieurs "lien_visee_maitrise"
    public virtual ICollection<Visee> IdViseeFks { get; set; } = new List<Visee>();
}
