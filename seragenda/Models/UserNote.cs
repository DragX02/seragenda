namespace seragenda.Models;

public class UserNote
{
    public int Id { get; set; }
    public int IdUserFk { get; set; }
    public DateTime Date { get; set; }
    public int Hour { get; set; }    // début (6-22)
    public int EndHour { get; set; } // fin exclusive (7-23)
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    public virtual Utilisateur? User { get; set; }
}
