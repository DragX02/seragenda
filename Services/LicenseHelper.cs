using System.Security.Cryptography;
using System.Text;

namespace seragenda.Services
{
    public static class LicenseHelper
    {
        public static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpper()));

            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
