namespace seragenda.Models;

public class UserConge
{
    public int Id { get; set; }

    public int IdUserFk { get; set; }

    public int? IdCalendrierFk { get; set; }

    public string Nom { get; set; } = string.Empty;

    public DateTime DateDebut { get; set; }

    public DateTime DateFin { get; set; }

    public bool Masque { get; set; }

    public virtual Utilisateur? User { get; set; }
}
