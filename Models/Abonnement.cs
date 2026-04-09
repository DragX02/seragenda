// Importation des types .NET de base (DateOnly, etc.)
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente une période d'abonnement pour un compte utilisateur.
// Un Abonnement suit le type, les dates de début/fin et le statut actif de l'accès d'un utilisateur.
// Un utilisateur peut avoir plusieurs enregistrements d'abonnement au fil du temps (ex. : essai → premium → renouvellement).
// Le statut par défaut en base de données est "Actif" lors de la création d'un nouvel abonnement.
public partial class Abonnement
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdAbonnement { get; set; }

    // Clé étrangère vers la table Utilisateur ; identifie l'utilisateur auquel cet abonnement appartient
    public int IdUserFk { get; set; }

    // Premier jour d'activité de cet abonnement (inclus)
    public DateOnly DateDebut { get; set; }

    // Dernier jour d'activité de cet abonnement (inclus)
    public DateOnly DateFin { get; set; }

    // Niveau ou type d'abonnement (ex. : "Premium", "Essai", "Annuel")
    // Peut être null si le type n'a pas encore été assigné
    public string? TypeAbo { get; set; }

    // Statut actuel de l'abonnement (ex. : "Actif", "Expiré", "Annulé")
    // Par défaut "Actif" en base de données lors de l'insertion d'un nouvel enregistrement
    public string? Statut { get; set; }

    // Propriété de navigation : l'enregistrement Utilisateur complet du propriétaire de l'abonnement
    public virtual Utilisateur IdUserFkNavigation { get; set; } = null!;
}
