// Import regular expression support for pattern-based validation
using System.Text.RegularExpressions;

namespace seragenda.Validators
{
    /// <summary>
    /// Provides static validation and sanitization methods for user-supplied input.
    /// These methods are used throughout the authentication and registration controllers
    /// to defend against injection attacks, XSS, and to enforce data quality rules.
    /// All methods are stateless and thread-safe.
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Checks whether the given string is a syntactically valid email address.
        /// Enforces a maximum length of 100 characters to prevent abuse.
        /// Uses a permissive regex that accepts most real-world email formats:
        /// "anything@anything.anything" (no whitespace, exactly one @).
        /// </summary>
        /// <param name="email">The email address string to validate</param>
        /// <returns>True if the email is non-empty, within the length limit, and matches the pattern</returns>
        public static bool IsValidEmail(string email)
        {
            // Reject null/whitespace and emails that exceed the maximum allowed length
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100)
                return false;

            try
            {
                // Regex pattern: at least one non-whitespace, non-@ character on each side of the @,
                // and a dot-containing domain part
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                // Treat any regex exception as a validation failure
                return false;
            }
        }

        /// <summary>
        /// Detects whether the input string contains patterns associated with
        /// SQL injection or cross-site scripting (XSS) attacks.
        /// Used as a secondary defence layer after format validation.
        /// An empty or whitespace-only string is considered safe (returns false).
        /// </summary>
        /// <param name="input">The user-supplied string to scan for dangerous patterns</param>
        /// <returns>True if a dangerous pattern is detected; false if the string is safe</returns>
        public static bool ContainsDangerousCharacters(string input)
        {
            // Empty inputs are inherently safe
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // An array of regex patterns corresponding to common injection and XSS payloads
            var dangerousPatterns = new[]
            {
                @"<script",          // Inline JavaScript via script tag
                @"javascript:",      // JavaScript URI scheme (e.g., in href attributes)
                @"onerror=",         // XSS event handler attribute
                @"onload=",          // XSS event handler attribute
                @"';--",             // SQL injection: string terminator + comment
                @""";--",           // SQL injection: double-quote string terminator + comment
                @"DROP\s+TABLE",     // SQL DDL: table deletion
                @"INSERT\s+INTO",    // SQL DML: data insertion
                @"DELETE\s+FROM",    // SQL DML: data deletion
                @"UPDATE\s+.*\s+SET",// SQL DML: data update
                @"EXEC\s*\(",        // SQL stored procedure execution
                @"<iframe",          // Embedded frame injection
                @"SELECT\s+.*\s+FROM",// SQL DQL: data exfiltration
                @"UNION\s+SELECT",   // SQL injection: result set union
                @"--",               // SQL inline comment (used to truncate queries)
                @"/\*",              // SQL block comment opening
                @"\*/",              // SQL block comment closing
                @"xp_",              // SQL Server extended stored procedure prefix
                @"sp_"               // SQL Server system stored procedure prefix
            };

            // Check each pattern against the input (case-insensitive to catch mixed-case variants)
            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    return true; // A dangerous pattern was found
            }

            // No dangerous patterns detected
            return false;
        }

        /// <summary>
        /// Validates a person's name (first name or last name).
        /// Allows only letters (including accented Latin characters), spaces, hyphens, and apostrophes.
        /// Enforces a maximum length of 50 characters.
        /// </summary>
        /// <param name="name">The name string to validate</param>
        /// <returns>True if the name is non-empty, within the length limit, and contains only allowed characters</returns>
        public static bool IsValidName(string name)
        {
            // Reject null/whitespace and names exceeding 50 characters
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                return false;

            // Allow uppercase and lowercase ASCII letters, accented Latin characters (À-ÿ),
            // spaces, hyphens, and apostrophes — common in French and other European names
            var nameRegex = new Regex(@"^[a-zA-ZÀ-ÿ\s\-']+$");
            return nameRegex.IsMatch(name);
        }

        /// <summary>
        /// Validates that a text value falls within a specified length range.
        /// Returns false if the string is null or whitespace.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <param name="minLength">The minimum required number of characters (inclusive)</param>
        /// <param name="maxLength">The maximum allowed number of characters (inclusive)</param>
        /// <returns>True if the text length is within [minLength, maxLength]; false otherwise</returns>
        public static bool IsValidLength(string text, int minLength, int maxLength)
        {
            // Null or whitespace-only strings are considered invalid regardless of the range
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Check that the length falls within the inclusive range
            return text.Length >= minLength && text.Length <= maxLength;
        }

        /// <summary>
        /// Validates a password string.
        /// Requires between 6 and 100 characters (inclusive).
        /// Does not enforce complexity rules (uppercase, digits, symbols) — only length.
        /// </summary>
        /// <param name="password">The plaintext password to validate (never stored)</param>
        /// <returns>True if the password is non-empty and within the allowed length range</returns>
        public static bool IsValidPassword(string password)
        {
            // Reject null or whitespace-only passwords
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Enforce the minimum length of 6 and maximum length of 100
            return password.Length >= 6 && password.Length <= 100;
        }

        /// <summary>
        /// Sanitizes a user-supplied string by HTML-encoding the five special HTML characters
        /// (&lt;, &gt;, ", ', /) and trimming surrounding whitespace.
        /// Used for names and other display strings before storing them in the database,
        /// so that if the value is ever rendered in HTML, it will not be interpreted as markup.
        /// </summary>
        /// <param name="input">The raw user input to sanitize</param>
        /// <returns>
        /// The sanitized string with dangerous HTML characters replaced by their entity equivalents,
        /// or an empty string if the input is null or whitespace.
        /// </returns>
        public static string SanitizeInput(string input)
        {
            // Return an empty string for null or whitespace-only input
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input
                .Replace("<",  "&lt;")    // Prevents opening of HTML tags
                .Replace(">",  "&gt;")    // Prevents closing of HTML tags
                .Replace("\"", "&quot;")  // Prevents breaking out of HTML attribute double quotes
                .Replace("'",  "&#x27;")  // Prevents breaking out of HTML attribute single quotes
                .Replace("/",  "&#x2F;")  // Prevents self-closing tag patterns (e.g., />)
                .Trim();                  // Remove surrounding whitespace after encoding
        }
    }
}
