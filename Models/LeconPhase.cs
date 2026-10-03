namespace seragenda.Models;

public class LeconPhase
{
    public int Id { get; set; }

    public int IdLeconFk { get; set; }

    public int Ordre { get; set; }

    public string Intitule { get; set; } = string.Empty;

    public string Temps { get; set; } = string.Empty;

    public virtual Lecon? Lecon { get; set; }
}
