namespace seragenda.Models;

public class Lecon
{
    public int Id { get; set; }

    public int IdUserFk { get; set; }

    public string Titre { get; set; } = string.Empty;

    public string Enseignant { get; set; } = string.Empty;

    public string Duree { get; set; } = string.Empty;

    public int NombreSeances { get; set; } = 1;

    public string Niveaux { get; set; } = string.Empty;

    public string Competences { get; set; } = string.Empty;

    public int? IdViseeFk { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public virtual ICollection<LeconPhase> Phases { get; set; } = new List<LeconPhase>();

    public virtual Utilisateur? User { get; set; }
}
