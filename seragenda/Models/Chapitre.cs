// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un chapitre unique au sein d'un manuel scolaire (Livre).
// Les enseignants peuvent référencer des chapitres spécifiques pour enregistrer quelles sections du livre
// sont utilisées lors d'une séance de cours (via SeanceRessource).
// Les chapitres peuvent également apparaître dans les enregistrements d'utilisation de chapitres au niveau du cours (UtilisationChapitre).
public partial class Chapitre
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdChapitre { get; set; }

    // Clé étrangère vers le Livre (manuel) auquel appartient ce chapitre
    public int IdLivreFk { get; set; }

    // Numéro séquentiel du chapitre dans le livre (ex. : 1, 2, 3, ...)
    public int NumeroChapitre { get; set; }

    // Titre complet du chapitre (ex. : "Les équations du premier degré")
    // Longueur maximale : 255 caractères
    public string TitreChapitre { get; set; } = null!;

    // Numéro de page de début optionnel du chapitre dans le livre physique
    // Null si le numéro de page est inconnu ou non applicable
    public int? PageDebut { get; set; }

    // Propriété de navigation : l'enregistrement Livre complet auquel appartient ce chapitre
    public virtual Livre IdLivreFkNavigation { get; set; } = null!;

    // Propriété de navigation : tous les enregistrements de ressources de séance qui référencent ce chapitre
    public virtual ICollection<SeanceRessource> SeanceRessources { get; set; } = new List<SeanceRessource>();

    // Propriété de navigation : tous les enregistrements d'utilisation de chapitre au niveau du cours qui référencent ce chapitre
    public virtual ICollection<UtilisationChapitre> UtilisationChapitres { get; set; } = new List<UtilisationChapitre>();
}
