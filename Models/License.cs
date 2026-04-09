// Espace de noms imbriqué dans le namespace seragenda.Models
namespace seragenda.Models
{
    // Représente une clé de licence accordant l'accès à l'application.
    // Les codes de licence ne sont jamais stockés en clair ; la propriété Code contient
    // le hachage SHA-256 du code d'origine (voir le service LicenseHelper).
    // Un administrateur crée des licences et distribue les codes en clair aux utilisateurs finaux,
    // qui les valident ensuite via l'endpoint d'accès pour lier la licence à leur compte.
    public class License
    {
        // Clé primaire — entier auto-incrémenté assigné par la base de données
        public int Id { get; set; }

        // Hachage SHA-256 (hexadécimal en minuscules, 64 caractères) du code de licence normalisé en clair.
        // Le code en clair n'est JAMAIS stocké — seul ce hachage est persisté.
        public string Code { get; set; } = string.Empty;

        // Indique si la licence peut actuellement être utilisée.
        // false = révoquée manuellement par un administrateur ; la licence est rejetée lors de la validation.
        public bool IsActive { get; set; } = true;

        // Horodatage UTC de la création de cet enregistrement de licence par un administrateur
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Date/heure d'expiration UTC optionnelle ; null signifie que la licence n'expire jamais.
        // Si défini, la licence est rejetée une fois cet horodatage dépassé.
        public DateTime? ExpiresAt { get; set; }

        // Étiquette lisible optionnelle pour aider l'administrateur à identifier l'objet de la licence
        // (ex. : "PROF-DUPONT" ou "Premium-2025")
        public string? Label { get; set; }

        // Clé étrangère vers l'Utilisateur qui a activé cette licence ; null si la licence n'est pas utilisée
        public int? AssignedUserId { get; set; }

        // Horodatage UTC de la première activation de la licence par un utilisateur ; null si non utilisée
        public DateTime? AssignedAt { get; set; }

        // Propriété de navigation vers l'utilisateur qui a activé cette licence
        // Chargée via Include() dans la requête AdminController.GetLicenses
        public Utilisateur? AssignedUser { get; set; }
    }
}
