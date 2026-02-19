using System.Security.Cryptography;
using System.Text;

namespace seragenda.Services
{
    public static class LicenseHelper
    {
        /// <summary>
        /// Retourne le hash SHA-256 (hex lowercase) du code normalisé en majuscules.
        /// Utilisé pour stocker et comparer les codes de licence sans jamais persister le plaintext.
        /// </summary>
        public static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpper()));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
