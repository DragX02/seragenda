// Importation des types .NET de base (DateTime, DateOnly, etc.)
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;
// Importation des DataAnnotations pour les substitutions de mappage de colonnes
using System.ComponentModel.DataAnnotations.Schema;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Représente un compte utilisateur enregistré dans l'application.
// Les utilisateurs sont généralement des enseignants ("PROF") ou des administrateurs ("ADMIN").
// Les comptes peuvent être créés localement (email + mot de passe) ou via OAuth (Google, Microsoft).
// Les comptes locaux nécessitent une confirmation d'email avant que la connexion ne soit autorisée.
public partial class Utilisateur
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdUser { get; set; }

    // Adresse email de l'utilisateur ; utilisée comme identifiant de connexion et doit être unique parmi tous les comptes
    public string Email { get; set; } = null!;

    // Hachage BCrypt du mot de passe de l'utilisateur ; le mot de passe en clair n'est jamais stocké
    // Pour les comptes OAuth, ce champ contient un hachage aléatoire inutilisable
    public string PasswordHash { get; set; } = null!;

    // Champ de nom complet calculé ou hérité, stocké dans la colonne "nom_complet" de la base de données
    // Préférer l'utilisation séparée de Nom + Prenom ; ce champ peut être null pour les comptes plus récents
    public string? NomComplet { get; set; }

    // Nom de famille de l'utilisateur, mappé sur la colonne "nom"
    [Column("nom")]
    public string? Nom { get; set; }

    // Prénom de l'utilisateur, mappé sur la colonne "prenom"
    [Column("prenom")]
    public string? Prenom { get; set; }

    // Date de naissance optionnelle, mappée sur la colonne "date_naissance"
    [Column("date_naissance")]
    public DateOnly? DateNaissance { get; set; }

    // Rôle système contrôlant le niveau d'accès ; les valeurs valides sont "PROF" (par défaut) et "ADMIN"
    public string RoleSysteme { get; set; } = null!;

    // Horodatage UTC de la création du compte ; par défaut CURRENT_TIMESTAMP en base de données
    public DateTime? CreatedAt { get; set; }

    // Nom du fournisseur OAuth utilisé pour créer le compte ("Google", "Microsoft"), ou null pour les comptes locaux
    // Remarque : les colonnes auth_provider, is_confirmed, nom, prenom, date_naissance doivent être ajoutées via ALTER TABLE
    // lors d'une migration depuis une version plus ancienne du schéma (voir les notes de migration)
    public string? AuthProvider { get; set; }

    // Indique si l'utilisateur a confirmé son adresse email.
    // Les comptes locaux démarrent à false et passent à true après avoir cliqué sur le lien de confirmation.
    // Les comptes OAuth sont confirmés immédiatement (le fournisseur a déjà vérifié l'email).
    public bool IsConfirmed { get; set; }

    // Jeton unique envoyé par email pour confirmer la propriété du compte ; effacé après utilisation
    public string? ConfirmationToken { get; set; }

    // Date/heure d'expiration UTC du jeton de confirmation ; le jeton est invalide après ce point
    public DateTime? ConfirmationTokenExpiresAt { get; set; }

    // Propriété de navigation : tous les enregistrements d'abonnement associés à cet utilisateur
    public virtual ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();

    // Propriété de navigation : toutes les affectations d'enseignement cours-niveau pour cet utilisateur (en tant qu'enseignant)
    public virtual ICollection<CoursNiveau> CoursNiveaus { get; set; } = new List<CoursNiveau>();
}
