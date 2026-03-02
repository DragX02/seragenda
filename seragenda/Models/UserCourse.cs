// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a recurring course (class) entry in a teacher's personal schedule.
/// A single UserCourse record describes a class that repeats on specific days of the week
/// throughout a date range (e.g., a full semester).
/// Days of week are encoded as a bitmask integer:
///   Monday = 1, Tuesday = 2, Wednesday = 4, Thursday = 8, Friday = 16, Saturday = 32, Sunday = 64
/// Multiple days are combined with bitwise OR (e.g., Mon+Wed = 1|4 = 5).
/// </summary>
public class UserCourse
{
    // Primary key — auto-incremented integer assigned by the database
    public int Id { get; set; }

    // Foreign key to the Utilisateur table; identifies the teacher who owns this course entry
    public int IdUserFk { get; set; }

    // Display name of the course (e.g., "Mathematics 3B", "French Literature")
    public string Name { get; set; } = string.Empty;

    // CSS colour string used to render this course on the agenda (e.g., "#3B82F6", "#FF0000")
    public string Color { get; set; } = "#FF0000";

    // First date on which this recurring course occurs (typically the start of the semester)
    public DateTime StartDate { get; set; }

    // Last date on which this recurring course occurs (typically the end of the semester)
    public DateTime EndDate { get; set; }

    // Time of day when the course starts (e.g., 08:00:00)
    public TimeSpan StartTime { get; set; }

    // Time of day when the course ends (e.g., 09:30:00)
    public TimeSpan EndTime { get; set; }

    // Bitmask encoding which days of the week this course recurs on.
    // Mon=1, Tue=2, Wed=4, Thu=8, Fri=16, Sat=32, Sun=64
    // Example: a course on Monday and Wednesday has DaysOfWeek = 1 | 4 = 5
    public int DaysOfWeek { get; set; }

    // Navigation property back to the owning Utilisateur record
    // Marked as nullable because it is not always eagerly loaded
    public virtual Utilisateur? User { get; set; }
}
