// Import du support des expressions régulières pour la validation par motif
using System.Text.RegularExpressions;

namespace seragenda.Validators
{
    // Fournit des méthodes statiques de validation et de désinfection pour les entrées utilisateur.
    // Ces méthodes sont utilisées dans les contrôleurs d'authentification et d'inscription
    // pour se défendre contre les attaques par injection, XSS, et pour garantir la qualité des données.
    // Toutes les méthodes sont sans état et thread-safe.
    public static class InputValidator
    {
        // Vérifie si la chaîne donnée est une adresse e-mail syntaxiquement valide.
        // Impose une longueur maximale de 100 caractères pour prévenir les abus.
        // Utilise une expression régulière permissive acceptant la plupart des formats réels :
        // "quelquechose@quelquechose.quelquechose" (pas d'espace, exactement un @).
        // email : la chaîne d'adresse e-mail à valider
        // Retourne true si l'e-mail est non vide, dans la limite de longueur et correspond au motif
        public static bool IsValidEmail(string email)
        {
            // Rejeter les valeurs nulles/vides et les e-mails dépassant la longueur maximale autorisée
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100)
                return false;

            try
            {
                // Motif regex : au moins un caractère non espace/non @ de chaque côté du @,
                // et une partie domaine contenant un point
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                // Toute exception regex est traitée comme un échec de validation
                return false;
            }
        }

        // Détecte si la chaîne d'entrée contient des motifs associés à des attaques
        // par injection SQL ou cross-site scripting (XSS).
        // Utilisé comme couche de défense secondaire après la validation du format.
        // Une chaîne vide ou composée uniquement d'espaces est considérée comme sûre (retourne false).
        // input : la chaîne fournie par l'utilisateur à analyser pour détecter des motifs dangereux
        // Retourne true si un motif dangereux est détecté ; false si la chaîne est sûre
        public static bool ContainsDangerousCharacters(string input)
        {
            // Les entrées vides sont intrinsèquement sûres
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Tableau de motifs regex correspondant aux charges utiles d'injection et XSS courantes
            var dangerousPatterns = new[]
            {
                @"<script",          // JavaScript en ligne via balise script
                @"javascript:",      // Schéma URI JavaScript (ex. dans les attributs href)
                @"onerror=",         // Attribut de gestionnaire d'événement XSS
                @"onload=",          // Attribut de gestionnaire d'événement XSS
                @"';--",             // Injection SQL : terminateur de chaîne + commentaire
                @""";--",           // Injection SQL : terminateur de chaîne guillemet double + commentaire
                @"DROP\s+TABLE",     // DDL SQL : suppression de table
                @"INSERT\s+INTO",    // DML SQL : insertion de données
                @"DELETE\s+FROM",    // DML SQL : suppression de données
                @"UPDATE\s+.*\s+SET",// DML SQL : mise à jour de données
                @"EXEC\s*\(",        // Exécution de procédure stockée SQL
                @"<iframe",          // Injection de frame intégrée
                @"SELECT\s+.*\s+FROM",// DQL SQL : exfiltration de données
                @"UNION\s+SELECT",   // Injection SQL : union d'ensembles de résultats
                @"--",               // Commentaire en ligne SQL (utilisé pour tronquer les requêtes)
                @"/\*",              // Ouverture de commentaire de bloc SQL
                @"\*/",              // Fermeture de commentaire de bloc SQL
                @"xp_",              // Préfixe de procédure stockée étendue SQL Server
                @"sp_"               // Préfixe de procédure stockée système SQL Server
            };

            // Vérification de chaque motif sur l'entrée (insensible à la casse pour détecter les variantes mixtes)
            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    return true; // Un motif dangereux a été trouvé
            }

            // Aucun motif dangereux détecté
            return false;
        }

        // Valide le nom d'une personne (prénom ou nom de famille).
        // Autorise uniquement les lettres (y compris les caractères latins accentués), les espaces, les traits d'union et les apostrophes.
        // Impose une longueur maximale de 50 caractères.
        // name : la chaîne de nom à valider
        // Retourne true si le nom est non vide, dans la limite de longueur et contient uniquement les caractères autorisés
        public static bool IsValidName(string name)
        {
            // Rejeter les valeurs nulles/vides et les noms dépassant 50 caractères
            if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
                return false;

            // Autoriser les lettres ASCII majuscules et minuscules, les caractères latins accentués (À-ÿ),
            // les espaces, les traits d'union et les apostrophes — courants dans les noms français et européens
            var nameRegex = new Regex(@"^[a-zA-ZÀ-ÿ\s\-']+$");
            return nameRegex.IsMatch(name);
        }

        // Valide qu'une valeur textuelle se situe dans une plage de longueur spécifiée.
        // Retourne false si la chaîne est null ou composée uniquement d'espaces.
        // text : le texte à vérifier
        // minLength : le nombre minimal de caractères requis (inclus)
        // maxLength : le nombre maximal de caractères autorisés (inclus)
        // Retourne true si la longueur du texte est dans [minLength, maxLength] ; false sinon
        public static bool IsValidLength(string text, int minLength, int maxLength)
        {
            // Les chaînes nulles ou composées uniquement d'espaces sont considérées invalides quelle que soit la plage
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Vérification que la longueur se situe dans la plage inclusive
            return text.Length >= minLength && text.Length <= maxLength;
        }

        // Valide une chaîne de mot de passe.
        // Requiert entre 6 et 100 caractères (inclus).
        // N'impose pas de règles de complexité (majuscules, chiffres, symboles) — uniquement la longueur.
        // password : le mot de passe en clair à valider (jamais stocké)
        // Retourne true si le mot de passe est non vide et dans la plage de longueur autorisée
        public static bool IsValidPassword(string password)
        {
            // Rejeter les mots de passe nuls ou composés uniquement d'espaces
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Vérification de la longueur minimale de 6 et maximale de 100
            return password.Length >= 6 && password.Length <= 100;
        }

        // Désinfecte une chaîne fournie par l'utilisateur en encodant en HTML les cinq caractères HTML spéciaux
        // (<, >, ", ', /) et en supprimant les espaces environnants.
        // Utilisé pour les noms et autres chaînes d'affichage avant leur stockage en base de données,
        // afin que si la valeur est un jour rendue en HTML, elle ne soit pas interprétée comme du balisage.
        // input : la saisie brute de l'utilisateur à désinfecter
        // Retourne la chaîne désinfectée avec les caractères HTML dangereux remplacés par leurs équivalents entité,
        // ou une chaîne vide si l'entrée est null ou composée uniquement d'espaces.
        public static string SanitizeInput(string input)
        {
            // Retourner une chaîne vide pour une entrée null ou composée uniquement d'espaces
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input
                .Replace("<",  "&lt;")    // Empêche l'ouverture de balises HTML
                .Replace(">",  "&gt;")    // Empêche la fermeture de balises HTML
                .Replace("\"", "&quot;")  // Empêche la sortie des guillemets doubles d'attribut HTML
                .Replace("'",  "&#x27;")  // Empêche la sortie des guillemets simples d'attribut HTML
                .Replace("/",  "&#x2F;")  // Empêche les motifs de balise auto-fermante (ex. />)
                .Trim();                  // Suppression des espaces environnants après encodage
        }
    }
}
