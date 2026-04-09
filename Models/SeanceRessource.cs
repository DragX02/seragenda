// Importation des types .NET de base
using System;
// Importation des interfaces de collection pour les propriétés de navigation
using System.Collections.Generic;

// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Relie un chapitre de manuel (Chapitre) à une séance de cours planifiée (Planification),
// enregistrant quel(s) chapitre(s) sera(seront) utilisé(s) comme ressources pédagogiques durant la séance.
// Une même séance peut référencer plusieurs chapitres, et le même chapitre peut apparaître
// dans plusieurs séances.
// Il s'agit d'une table de jointure pure avec sa propre clé primaire de substitution pour une gestion facilitée.
public partial class SeanceRessource
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int IdSeanceRes { get; set; }

    // Clé étrangère vers la Planification (séance de cours) qui utilise cette ressource
    public int IdPlanningFk { get; set; }

    // Clé étrangère vers le Chapitre (chapitre de manuel) référencé comme ressource
    public int IdChapitreFk { get; set; }

    // Propriété de navigation : l'enregistrement Chapitre complet pour le chapitre de manuel référencé
    public virtual Chapitre IdChapitreFkNavigation { get; set; } = null!;

    // Propriété de navigation : l'enregistrement Planification (séance de cours) complet
    public virtual Planification IdPlanningFkNavigation { get; set; } = null!;
}
