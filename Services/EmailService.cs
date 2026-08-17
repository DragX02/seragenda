// Client SMTP MailKit pour l'envoi d'e-mails via une vraie connexion SMTP
using MailKit.Net.Smtp;
// Options de sécurité MailKit (StartTLS, SSL, etc.)
using MailKit.Security;
// Types MimeKit pour construire le message et le corps du mail
using MimeKit;

// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Services;

// Implémentation concrète de IEmailService qui envoie des e-mails transactionnels
// via MailKit sur SMTP (configuré dans appsettings.json sous "EmailSettings").
// Prend en charge deux types d'e-mails : confirmation de compte et bienvenue (inscription OAuth).
public class EmailService : IEmailService
{
    // Fournisseur de configuration pour lire les identifiants SMTP et les URLs
    private readonly IConfiguration _config;
    // Logger pour enregistrer les erreurs SMTP sans faire planter le code appelant
    private readonly ILogger<EmailService> _logger;

    // Constructeur — reçoit la configuration et le logger par injection de dépendances.
    // config : configuration de l'application (fournit la section EmailSettings)
    // logger : logger pour enregistrer les erreurs d'envoi d'e-mail
    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // Envoie un e-mail HTML demandant à l'utilisateur de cliquer sur un lien pour confirmer son adresse.
    // Le lien est valable 24 heures. En cas d'échec, l'exception est journalisée et relancée
    // afin que l'appelant décide de l'ignorer ou de la propager.
    // toEmail : adresse e-mail du destinataire
    // prenom : prénom du destinataire, utilisé pour personnaliser le corps du mail
    // confirmationUrl : URL complète que le destinataire doit cliquer pour activer son compte
    public async Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl)
    {
        // Lecture des paramètres SMTP depuis la section "EmailSettings"
        var smtp      = _config.GetSection("EmailSettings");
        // Adresse expéditeur par défaut si le paramètre est absent
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        // Lecture de l'URL de base du frontend pour construire l'URL du logo
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        // Construction de l'URL absolue du logo utilisé dans l'en-tête de l'e-mail
        var logoUrl   = $"{frontUrl}/icon-192.png";

        // Construction de l'enveloppe MIME
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        // Le nom affiché dans l'en-tête To utilise le prénom du destinataire
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Confirmez votre inscription";

        // Construction du corps HTML et du texte brut de secours
        var bodyBuilder = new BodyBuilder
        {
            // Corps HTML riche construit par la méthode privée helper
            HtmlBody = BuildHtml(prenom, confirmationUrl, logoUrl),
            // Texte brut de secours pour les clients e-mail qui n'affichent pas le HTML
            TextBody = $"Bonjour {prenom},\n\nConfirmez votre compte ObriGénie en visitant ce lien :\n{confirmationUrl}\n\nLien valable 24 heures."
        };
        message.Body = bodyBuilder.ToMessageBody();

        // Envoi via SMTP ; l'exception est journalisée puis relancée pour que l'appelant décide
        await SendViaSmtpAsync(message, toEmail, "confirmation");
    }

    // Envoie un e-mail HTML contenant un lien permettant de choisir un nouveau mot de passe.
    // Le lien est valable 1 heure et ne peut être utilisé qu'une seule fois (le jeton est effacé après usage).
    // toEmail : adresse e-mail du destinataire (l'adresse enregistrée sur le compte)
    // prenom : prénom du destinataire, utilisé pour personnaliser le corps du mail
    // resetUrl : URL complète que le destinataire doit cliquer pour définir un nouveau mot de passe
    public async Task SendPasswordResetEmailAsync(string toEmail, string prenom, string resetUrl)
    {
        // Lecture des paramètres SMTP depuis la section "EmailSettings"
        var smtp      = _config.GetSection("EmailSettings");
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";

        // Construction de l'enveloppe MIME
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Réinitialisation de votre mot de passe";

        var bodyBuilder = new BodyBuilder
        {
            // Corps HTML riche construit par la méthode privée helper
            HtmlBody = BuildResetHtml(prenom, resetUrl),
            // Texte brut de secours pour les clients e-mail qui n'affichent pas le HTML
            TextBody = $"Bonjour {prenom},\n\nVous avez demandé la réinitialisation de votre mot de passe ObriGénie.\nChoisissez un nouveau mot de passe via ce lien :\n{resetUrl}\n\nLien valable 1 heure. Si vous n'êtes pas à l'origine de cette demande, ignorez cet e-mail : votre mot de passe reste inchangé."
        };
        message.Body = bodyBuilder.ToMessageBody();

        await SendViaSmtpAsync(message, toEmail, "réinitialisation de mot de passe");
    }

    // Envoie un e-mail de bienvenue à un utilisateur qui vient de s'inscrire via OAuth (Google ou Microsoft).
    // Contrairement à l'e-mail de confirmation, celui-ci ne contient pas de lien d'activation —
    // les comptes OAuth sont considérés comme confirmés immédiatement par le fournisseur.
    // toEmail : adresse e-mail du destinataire
    // prenom : prénom du destinataire, utilisé pour personnaliser le message de bienvenue
    public async Task SendWelcomeEmailAsync(string toEmail, string prenom)
    {
        // Lecture des paramètres SMTP depuis la configuration
        var smtp      = _config.GetSection("EmailSettings");
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        var logoUrl   = $"{frontUrl}/icon-192.png";

        // Construction du message MIME
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Bienvenue !";

        var bodyBuilder = new BodyBuilder
        {
            // Corps HTML de bienvenue
            HtmlBody = BuildWelcomeHtml(prenom, frontUrl, logoUrl),
            // Texte brut de secours
            TextBody = $"Bonjour {prenom},\n\nBienvenue sur ObriGénie ! Votre compte est activé.\n\nAccédez à l'application : {frontUrl}"
        };
        message.Body = bodyBuilder.ToMessageBody();

        await SendViaSmtpAsync(message, toEmail, "bienvenue");
    }

    // Ouvre une connexion SMTP jetable, s'authentifie et envoie le message construit par l'appelant.
    // Centralise la connexion/authentification/déconnexion commune à tous les e-mails transactionnels.
    // message : le message MIME complet (expéditeur, destinataire, sujet, corps) prêt à être envoyé
    // toEmail : adresse du destinataire, uniquement utilisée pour le message de journalisation
    // typeMail : libellé du type d'e-mail (confirmation, bienvenue, ...) inclus dans le log d'erreur
    private async Task SendViaSmtpAsync(MimeMessage message, string toEmail, string typeMail)
    {
        // Lecture des paramètres SMTP depuis la section "EmailSettings"
        var smtp = _config.GetSection("EmailSettings");

        try
        {
            // Ouverture d'une connexion SMTP jetable
            using var client = new SmtpClient();
            // Connexion au serveur SMTP avec StartTLS sur le port configuré (généralement 587)
            await client.ConnectAsync(
                smtp["Host"] ?? "smtp.gmail.com",
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            // Authentification avec le nom d'utilisateur et le mot de passe SMTP issus de la configuration
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            // Envoi du message
            await client.SendAsync(message);
            // Fermeture propre de la session SMTP
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Journalisation de l'erreur avec l'adresse du destinataire pour le diagnostic
            _logger.LogError(ex, "Échec d'envoi du mail de {TypeMail} à {Email}", typeMail, toEmail);
            // Relance de l'exception pour que l'appelant décide de l'ignorer ou de la propager
            throw;
        }
    }

    // Construit le corps HTML de l'e-mail de confirmation de compte.
    // Utilise une mise en page tabulaire avec styles inline pour une compatibilité maximale.
    // Le bouton de confirmation renvoie vers l'URL fournie ; un lien texte de secours est également affiché.
    // prenom : prénom du destinataire pour la personnalisation
    // confirmUrl : URL complète de confirmation à intégrer dans le bouton et le lien de secours
    // logoUrl : URL absolue du logo de l'application (non affichée dans le design actuel)
    // Retourne une chaîne HTML complète prête à être définie comme corps HTML de l'e-mail
    private static string BuildHtml(string prenom, string confirmUrl, string logoUrl) => $"""
        <!DOCTYPE html>
        <html lang="fr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Confirmation ObriGénie</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f0f2f5;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.12);">

                  <!-- En-tête blanc avec logo texte -->
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>

                  <!-- Corps blanc avec salutation, explication, bouton et lien de secours -->
                  <tr>
                    <td style="background:#ffffff;padding:40px 40px 40px;">
                      <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:22px;">
                        Bienvenue, {prenom}&nbsp;! 🎉
                      </h2>
                      <p style="margin:0 0 20px;color:#444;font-size:16px;line-height:1.6;">
                        Merci de vous être inscrit(e) à <strong>Obrigenie</strong>.<br />
                        Pour activer votre compte et commencer à organiser votre agenda,
                        cliquez sur le bouton ci-dessous.
                      </p>

                      <!-- Bouton d'appel à l'action -->
                      <table cellpadding="0" cellspacing="0" style="margin:32px auto;">
                        <tr>
                          <td style="border-radius:10px;background:#1a1a2e;">
                            <a href="{confirmUrl}"
                               style="display:inline-block;padding:16px 36px;color:#ffffff;font-size:16px;font-weight:700;text-decoration:none;letter-spacing:0.3px;">
                              ✅&nbsp;&nbsp;Confirmer mon compte
                            </a>
                          </td>
                        </tr>
                      </table>

                      <!-- Lien texte de secours pour les clients e-mail qui bloquent les clics sur bouton -->
                      <p style="margin:0 0 8px;color:#666;font-size:13px;text-align:center;">
                        Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :
                      </p>
                      <p style="margin:0 0 24px;text-align:center;">
                        <a href="{confirmUrl}" style="color:#4f46e5;font-size:12px;word-break:break-all;">
                          {confirmUrl}
                        </a>
                      </p>

                      <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                      <p style="margin:0;color:#999;font-size:12px;line-height:1.6;text-align:center;">
                        Ce lien est valable <strong>24 heures</strong>.<br />
                        Si vous n'avez pas créé de compte, ignorez cet email.
                      </p>
                    </td>
                  </tr>

                  <!-- Pied de page avec mention de copyright -->
                  <tr>
                    <td style="background:#f8f9fa;padding:20px 30px;text-align:center;border-top:1px solid #eee;">
                      <p style="margin:0;color:#bbb;font-size:12px;">
                        © {DateTime.UtcNow.Year} Obrigenie – Votre assistant agenda scolaire
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    // Construit le corps HTML de l'e-mail de réinitialisation de mot de passe.
    // Même mise en page tabulaire à styles inline que les autres e-mails, avec un bouton
    // pointant vers la page frontend /reset-password et un lien texte de secours.
    // prenom : prénom du destinataire pour la personnalisation
    // resetUrl : URL complète de réinitialisation à intégrer dans le bouton et le lien de secours
    // Retourne une chaîne HTML complète prête à être définie comme corps HTML de l'e-mail
    private static string BuildResetHtml(string prenom, string resetUrl) => $"""
        <!DOCTYPE html>
        <html lang="fr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Réinitialisation du mot de passe Obrigenie</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f0f2f5;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.12);">

                  <!-- En-tête blanc avec logo texte -->
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>

                  <!-- Corps blanc avec explication, bouton et lien de secours -->
                  <tr>
                    <td style="background:#ffffff;padding:40px 40px 40px;">
                      <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:22px;">
                        Mot de passe oublié&nbsp;? 🔑
                      </h2>
                      <p style="margin:0 0 20px;color:#444;font-size:16px;line-height:1.6;">
                        Bonjour {prenom},<br />
                        Vous avez demandé à réinitialiser le mot de passe de votre compte
                        <strong>Obrigenie</strong>. Cliquez sur le bouton ci-dessous pour en choisir un nouveau.
                      </p>

                      <!-- Bouton d'appel à l'action -->
                      <table cellpadding="0" cellspacing="0" style="margin:32px auto;">
                        <tr>
                          <td style="border-radius:10px;background:#1a1a2e;">
                            <a href="{resetUrl}"
                               style="display:inline-block;padding:16px 36px;color:#ffffff;font-size:16px;font-weight:700;text-decoration:none;letter-spacing:0.3px;">
                              🔒&nbsp;&nbsp;Choisir un nouveau mot de passe
                            </a>
                          </td>
                        </tr>
                      </table>

                      <!-- Lien texte de secours pour les clients e-mail qui bloquent les clics sur bouton -->
                      <p style="margin:0 0 8px;color:#666;font-size:13px;text-align:center;">
                        Si le bouton ne fonctionne pas, copiez ce lien dans votre navigateur :
                      </p>
                      <p style="margin:0 0 24px;text-align:center;">
                        <a href="{resetUrl}" style="color:#4f46e5;font-size:12px;word-break:break-all;">
                          {resetUrl}
                        </a>
                      </p>

                      <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                      <p style="margin:0;color:#999;font-size:12px;line-height:1.6;text-align:center;">
                        Ce lien est valable <strong>1 heure</strong> et ne fonctionne qu'une seule fois.<br />
                        Si vous n'êtes pas à l'origine de cette demande, ignorez cet e-mail :
                        votre mot de passe actuel reste inchangé.
                      </p>
                    </td>
                  </tr>

                  <!-- Pied de page avec mention de copyright -->
                  <tr>
                    <td style="background:#f8f9fa;padding:20px 30px;text-align:center;border-top:1px solid #eee;">
                      <p style="margin:0;color:#bbb;font-size:12px;">
                        © {DateTime.UtcNow.Year} Obrigenie – Votre assistant agenda scolaire
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    // Construit le corps HTML de l'e-mail de bienvenue envoyé aux nouveaux utilisateurs OAuth.
    // Mise en page similaire à l'e-mail de confirmation, mais contient un lien direct vers l'application
    // au lieu d'un bouton de confirmation.
    // prenom : prénom du destinataire pour la personnalisation
    // frontUrl : URL de base du frontend de l'application
    // logoUrl : URL absolue du logo de l'application
    // Retourne une chaîne HTML complète prête à être définie comme corps HTML de l'e-mail
    private static string BuildWelcomeHtml(string prenom, string frontUrl, string logoUrl) => $"""
        <!DOCTYPE html>
        <html lang="fr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Bienvenue sur Obrigenie</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f2f5;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f0f2f5;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.12);">
                  <!-- En-tête blanc avec logo texte -->
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>
                  <!-- Corps blanc avec message de bienvenue et bouton lien vers l'application -->
                  <tr>
                    <td style="background:#ffffff;padding:40px 40px 40px;">
                      <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:22px;">Bienvenue, {prenom}&nbsp;! 🎉</h2>
                      <p style="margin:0 0 20px;color:#444;font-size:16px;line-height:1.6;">
                        Votre compte <strong>Obrigenie</strong> est activé et prêt à l'emploi.<br />
                        Organisez votre agenda scolaire dès maintenant.
                      </p>
                      <!-- Bouton d'appel à l'action pointant directement vers l'application -->
                      <table cellpadding="0" cellspacing="0" style="margin:32px auto;">
                        <tr>
                          <td style="border-radius:10px;background:#1a1a2e;">
                            <a href="{frontUrl}"
                               style="display:inline-block;padding:16px 36px;color:#ffffff;font-size:16px;font-weight:700;text-decoration:none;letter-spacing:0.3px;">
                              🚀&nbsp;&nbsp;Accéder à l'application
                            </a>
                          </td>
                        </tr>
                      </table>
                      <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                      <p style="margin:0;color:#999;font-size:12px;line-height:1.6;text-align:center;">
                        Si vous n'avez pas créé ce compte, ignorez cet email.
                      </p>
                    </td>
                  </tr>
                  <!-- Pied de page -->
                  <tr>
                    <td style="background:#f8f9fa;padding:20px 30px;text-align:center;border-top:1px solid #eee;">
                      <p style="margin:0;color:#bbb;font-size:12px;">
                        © {DateTime.UtcNow.Year} Obrigenie – Votre assistant agenda scolaire
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
