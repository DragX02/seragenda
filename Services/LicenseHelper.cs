// Import de l'algorithme de hachage cryptographique SHA-256
using System.Security.Cryptography;
// Import de l'encodage UTF-8 pour convertir les chaînes en octets avant le hachage
using System.Text;

namespace seragenda.Services
{
    // Classe utilitaire pour la gestion sécurisée des codes de licence.
    // Les codes de licence ne sont jamais stockés en clair dans la base de données.
    // À la place, leur hachage SHA-256 (hex minuscule) est stocké, et toute comparaison
    // s'effectue en hachant le code soumis et en le comparant au hachage stocké.
    // Cela protège les codes de licence en cas de compromission de la base de données.
    public static class LicenseHelper
    {
        // Calcule le hachage SHA-256 d'un code de licence et le retourne sous forme de chaîne hex minuscule.
        // Le code est rogné et mis en majuscules avant le hachage afin que
        // "abc123", "ABC123" et " ABC123 " produisent tous le même hachage.
        // code : le code de licence en clair à hacher (ex. "PROF-DUPONT")
        // Retourne une chaîne hex minuscule de 64 caractères représentant le condensat SHA-256 du code normalisé.
        // Cette valeur peut être stockée en toute sécurité dans la base de données.
        public static string HashCode(string code)
        {
            // Normalisation : suppression des espaces et mise en majuscules avant le hachage.
            // Garantit que les différences de casse et d'espacement ne produisent pas des hachages différents.
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpper()));

            // Conversion des 32 octets du hachage en une chaîne hex minuscule de 64 caractères.
            // ToLower() assure un stockage cohérent quel que soit la casse par défaut de la plateforme.
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
