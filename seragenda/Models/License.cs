namespace seragenda.Models
{
    public class License
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public string? Label { get; set; }           // Ex : "PROF-DUPONT", "Premium-2025"

        // Utilisateur qui a activé cette licence
        public int? AssignedUserId { get; set; }
        public DateTime? AssignedAt { get; set; }
        public Utilisateur? AssignedUser { get; set; }
    }
}
