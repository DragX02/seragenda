// Espace de noms délimité au fichier (style C# 10+)
namespace seragenda.Models;

// Une phase du déroulement d'une leçon : « Phase 1 : … Temps : … » sur le
// formulaire papier.
//
// Une phase n'existe que par sa leçon : supprimer la préparation emporte ses
// phases (ON DELETE CASCADE). L'enregistrement d'une leçon réécrit entièrement
// ses phases, ce qui évite d'avoir à distinguer ajout, modification et
// suppression pour des lignes que l'utilisateur manipule comme un bloc.
public class LeconPhase
{
    // Clé primaire — entier auto-incrémenté assigné par la base de données
    public int Id { get; set; }

    // Clé étrangère vers la préparation à laquelle cette phase appartient
    public int IdLeconFk { get; set; }

    // Rang d'affichage, à partir de 1 : c'est le numéro montré à l'écran et
    // sur la feuille (« Phase 1 », « Phase 2 »…). Unique au sein d'une leçon.
    public int Ordre { get; set; }

    // Ce qui se passe pendant la phase, en texte libre sur plusieurs lignes.
    // Longueur maximale imposée côté serveur : 1000 caractères.
    public string Intitule { get; set; } = string.Empty;

    // Temps imparti, en texte libre (« 10 min », « 1/4 h »).
    // Longueur maximale : 50.
    public string Temps { get; set; } = string.Empty;

    // Propriété de navigation vers la préparation propriétaire
    public virtual Lecon? Lecon { get; set; }
}
