using System.Text.RegularExpressions;

namespace seragenda.Validators
{
    public static class InputValidator
    {
        // Validation de l'email
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100)
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public static bool ContainsDangerousCharacters(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var dangerousPatterns = new[]
            {
                @"<script",
                @"javascript:",
                @"onerror=",
                @"onload=",
                @"';--",
                @""";--",
                @"DROP\s+TABLE",
                @"INSERT\s+INTO",
                @"DELETE\s+FROM",
                @"UPDATE\s+.*\s+SET",
                @"EXEC\s*\(",
                @"<iframe",
                @"SELECT\s+.*\s+FROM",
                @"UNION\s+SELECT",
                @"--",
                @"/\*",
                @"\*/",
                @"xp_",
                @"sp_"
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                return false;

            var nameRegex = new Regex(@"^[a-zA-ZÀ-ÿ\s\-']+$");
            return nameRegex.IsMatch(name);
        }
        public static bool IsValidLength(string text, int minLength, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Length >= minLength && text.Length <= maxLength;
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 6 && password.Length <= 100;
        }
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;")
                .Replace("/", "&#x2F;")
                .Trim();
        }
    }
}
