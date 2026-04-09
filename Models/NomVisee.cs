// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente le nom ou l'intitulé d'un type d'objectif d'apprentissage (visée).
// NomVisee joue le rôle de dictionnaire d'étiquettes pour la table Visee :
// au lieu de répéter la même longue chaîne d'étiquette dans chaque ligne Visee,
// l'étiquette est stockée une seule fois ici et référencée par clé étrangère.
// Exemples d'étiquettes : "Visée disciplinaire", "Visée transversale".
// Longueur maximale de l'étiquette : 150 caractères (contrainte de base de données).
public partial class NomVisee
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdNomVisee { get; set; }

    // Texte de l'étiquette pour ce type d'objectif d'apprentissage
    // Nommé avec le suffixe "1" (NomVisee1) pour éviter une collision de nommage avec la classe elle-même
    // Longueur maximale : 150 caractères
    public string NomVisee1 { get; set; } = null!;

    // Propriété de navigation : tous les enregistrements d'objectif d'apprentissage qui utilisent cette étiquette
    public virtual ICollection<Visee> Visees { get; set; } = new List<Visee>();
}
