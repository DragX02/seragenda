// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Préparation de leçon, telle que la remplit l'enseignant.
//
// Reprend le formulaire papier « Préparation de leçon type » : un en-tête
// (titre, enseignant, durée, nombre de séances, niveaux), les compétences
// visées, puis le déroulement découpé en phases.
//
// Le déroulement vit dans une table fille (LeconPhase) plutôt que dans quatre
// paires de colonnes : le modèle papier en compte quatre, mais une leçon qui en
// demande trois ou six ne doit pas exiger une migration de schéma.
public class Lecon
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int Id { get; set; }

    // Clé étrangère vers la table Utilisateur ; identifie l'enseignant propriétaire
    public int IdUserFk { get; set; }

    // Titre de la leçon. Seul champ obligatoire : c'est lui qui identifie la
    // préparation dans la liste. Longueur maximale imposée côté serveur : 200.
    public string Titre { get; set; } = string.Empty;

    // Nom de l'enseignant tel qu'il doit apparaître sur la feuille imprimée.
    // Distinct du compte connecté : une préparation peut être écrite pour un
    // collègue ou un stagiaire. Longueur maximale : 150.
    public string Enseignant { get; set; } = string.Empty;

    // Durée de la leçon, en texte libre : le formulaire accepte aussi bien
    // « 50 min » que « 2 x 50 min » ou « une matinée ». Longueur maximale : 100.
    public string Duree { get; set; } = string.Empty;

    // Nombre de séances que couvre la préparation (1 par défaut)
    public int NombreSeances { get; set; } = 1;

    // Niveaux concernés, saisis librement : une même leçon peut en viser
    // plusieurs. Longueur maximale : 200.
    public string Niveaux { get; set; } = string.Empty;

    // Compétences visées, en texte libre sur plusieurs lignes.
    // Longueur maximale imposée côté serveur : 4000 caractères.
    public string Competences { get; set; } = string.Empty;

    // Visée du référentiel choisie dans la cascade, ou null quand la section
    // Compétences n'est pas rattachée au référentiel.
    //
    // Le détail complet vit dans Competences, figé au moment de l'enregistrement :
    // c'est lui qui s'imprime, et il reste lisible même si le référentiel change
    // ensuite. Cette clé garde en plus le lien réel, comme UserNote.IdViseeFk.
    public int? IdViseeFk { get; set; }

    // Horodatage UTC de la première création de cette préparation
    public DateTime CreatedAt { get; set; }

    // Horodatage UTC de la dernière modification
    public DateTime ModifiedAt { get; set; }

    // Les phases du déroulement, dans leur ordre d'affichage
    public virtual ICollection<LeconPhase> Phases { get; set; } = new List<LeconPhase>();

    // Propriété de navigation vers l'enregistrement Utilisateur propriétaire
    // Marquée comme nullable car elle n'est pas toujours chargée en mode eager
    public virtual Utilisateur? User { get; set; }
}
