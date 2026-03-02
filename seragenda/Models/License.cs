// File-scoped namespace nested inside the seragenda.Models namespace
namespace seragenda.Models
{
    /// <summary>
    /// Represents a license key that grants access to the application.
    /// License codes are never stored in plaintext; the Code property holds
    /// the SHA-256 hash of the original code (see <see cref="Services.LicenseHelper"/>).
    /// An admin creates licenses and distributes the plain codes to end users,
    /// who then validate them via the access endpoint to link the license to their account.
    /// </summary>
    public class License
    {
        // Primary key — auto-incremented integer assigned by the database
        public int Id { get; set; }

        // SHA-256 hash (lowercase hex, 64 chars) of the normalised plain license code.
        // The plain code is NEVER stored — only this hash is persisted.
        public string Code { get; set; } = string.Empty;

        // Indicates whether the license can currently be used.
        // false = manually revoked by an admin; the license is rejected during validation.
        public bool IsActive { get; set; } = true;

        // UTC timestamp of when this license record was created by an admin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional UTC expiry date/time; null means the license never expires.
        // If set, the license is rejected once this timestamp has passed.
        public DateTime? ExpiresAt { get; set; }

        // Optional human-readable label to help the admin identify the license purpose
        // (e.g., "PROF-DUPONT" or "Premium-2025")
        public string? Label { get; set; }

        // Foreign key to the Utilisateur who activated this license; null if the license is unused
        public int? AssignedUserId { get; set; }

        // UTC timestamp of when the license was first activated by a user; null if unused
        public DateTime? AssignedAt { get; set; }

        // Navigation property to the user who activated this license
        // Loaded via Include() in the AdminController.GetLicenses query
        public Utilisateur? AssignedUser { get; set; }
    }
}
