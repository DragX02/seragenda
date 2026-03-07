// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un sous-domaine au sein d'un domaine pédagogique (Domaine).
// Les sous-domaines offrent un niveau d'organisation curriculaire plus fin en dessous des domaines.
// Par exemple, le domaine "Algèbre" pourrait contenir les sous-domaines
// "Équations du premier degré" et "Équations du second degré".
// Les objectifs d'apprentissage (Visee) peuvent être liés à un sous-domaine pour une classification précise.
// Longueur maximale du nom : 50 caractères (contrainte de base de données, colonne "nom_comp").
public partial class Sousdomaine
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdSousDomaine { get; set; }

    // Nom d'affichage / courte description du sous-domaine (stocké dans la colonne "nom_comp")
    // Longueur maximale : 50 caractères
    public string NomComp { get; set; } = null!;

    // Clé étrangère vers l'enregistrement Domaine parent
    public int IdDomFk { get; set; }

    // Propriété de navigation : l'enregistrement Domaine complet qui contient ce sous-domaine
    public virtual Domaine IdDomFkNavigation { get; set; } = null!;

    // Propriété de navigation : tous les objectifs d'apprentissage (visées) qui appartiennent à ce sous-domaine
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
