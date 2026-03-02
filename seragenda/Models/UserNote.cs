// File-scoped namespace (C# 10+ style)
namespace seragenda.Models;

/// <summary>
/// Represents a personal timed note entry in a teacher's daily agenda.
/// Each note is attached to a specific calendar date and occupies one or more
/// consecutive hour slots within the agenda grid (which spans from 06:00 to 23:00).
/// Content is stored as sanitized plain text (all HTML stripped server-side).
/// </summary>
public class UserNote
{
    // Primary key — auto-incremented integer assigned by the database
    public int Id { get; set; }

    // Foreign key to the Utilisateur table; identifies the teacher who owns this note
    public int IdUserFk { get; set; }

    // The calendar date this note belongs to (time component is always midnight / ignored)
    public DateTime Date { get; set; }

    // The hour at which this note starts on the agenda grid.
    // Valid range: 6 (6:00 AM) to 22 (10:00 PM)
    public int Hour { get; set; }

    // The hour at which this note ends on the agenda grid (exclusive upper bound).
    // Valid range: 7 to 23; must be strictly greater than Hour.
    public int EndHour { get; set; }

    // The text content of the note; HTML tags are stripped server-side before storage.
    // Maximum length enforced server-side: 2000 characters.
    public string Content { get; set; } = string.Empty;

    // UTC timestamp of when this note was first created
    public DateTime CreatedAt { get; set; }

    // UTC timestamp of the most recent modification to this note's content or time slot
    public DateTime ModifiedAt { get; set; }

    // Navigation property back to the owning Utilisateur record
    // Marked as nullable because it is not always eagerly loaded
    public virtual Utilisateur? User { get; set; }
}
