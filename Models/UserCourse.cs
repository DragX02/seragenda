namespace seragenda.Models;

public class UserCourse
{
    public int Id { get; set; }

    public int IdUserFk { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#FF0000";

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int DaysOfWeek { get; set; }

    public virtual Utilisateur? User { get; set; }
}
