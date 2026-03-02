// Import the SHA-256 cryptographic hash algorithm
using System.Security.Cryptography;
// Import UTF-8 text encoding for converting strings to bytes before hashing
using System.Text;

namespace seragenda.Services
{
    /// <summary>
    /// Utility class for securely handling license key codes.
    /// License codes are never stored in plaintext in the database.
    /// Instead, their SHA-256 hash (hex lowercase) is stored, and every comparison
    /// is done by hashing the submitted code and comparing it against the stored hash.
    /// This prevents license codes from being exposed if the database is compromised.
    /// </summary>
    public static class LicenseHelper
    {
        /// <summary>
        /// Computes the SHA-256 hash of a license code and returns it as a lowercase hex string.
        /// The input code is trimmed and uppercased before hashing to ensure that
        /// "abc123", "ABC123", and " ABC123 " all produce the same hash.
        /// </summary>
        /// <param name="code">The plaintext license code to hash (e.g., "PROF-DUPONT")</param>
        /// <returns>
        /// A 64-character lowercase hex string representing the SHA-256 digest of the normalised code.
        /// This value is safe to store in the database.
        /// </returns>
        public static string HashCode(string code)
        {
            // Normalise: remove surrounding whitespace and force uppercase before hashing.
            // This guarantees that case and padding differences do not produce different hashes.
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpper()));

            // Convert the 32-byte hash to a 64-character lowercase hex string.
            // ToLower() ensures consistent storage regardless of the platform's default casing.
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
