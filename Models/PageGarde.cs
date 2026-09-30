namespace seragenda.Models;

public class PageGarde
{
    public int Id { get; set; }

    public int IdUserFk { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Contenu { get; set; } = string.Empty;

    public bool PourTous { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }

    public virtual Utilisateur? User { get; set; }
}
