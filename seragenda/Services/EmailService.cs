// Import MailKit SMTP client for sending emails over a real SMTP connection
using MailKit.Net.Smtp;
// Import MailKit security options (StartTLS, SSL, etc.)
using MailKit.Security;
// Import MimeKit message and body builder types
using MimeKit;

// File-scoped namespace (C# 10+ style)
namespace seragenda.Services;

/// <summary>
/// Concrete implementation of <see cref="IEmailService"/> that sends transactional emails
/// using MailKit over SMTP (configured via appsettings.json under "EmailSettings").
/// Supports two email types: account confirmation and welcome (post-OAuth signup).
/// </summary>
public class EmailService : IEmailService
{
    // Application configuration provider for reading SMTP credentials and URLs
    private readonly IConfiguration _config;
    // Logger for recording SMTP errors without crashing the calling code
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Constructor — receives configuration and logger via dependency injection.
    /// </summary>
    /// <param name="config">Application configuration (provides EmailSettings section)</param>
    /// <param name="logger">Logger for recording email delivery errors</param>
    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTML email asking the user to click a link to confirm their email address.
    /// The link is valid for 24 hours. If sending fails, the exception is logged and re-thrown
    /// so the caller can decide whether to swallow it or propagate it.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="prenom">Recipient's first name, used to personalise the email body</param>
    /// <param name="confirmationUrl">The full URL the recipient must click to activate their account</param>
    public async Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl)
    {
        // Read SMTP settings from the "EmailSettings" configuration section
        var smtp      = _config.GetSection("EmailSettings");
        // Fall back to a default "noreply" address if the setting is absent
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        // Read the frontend base URL for building the logo image URL
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        // Construct the absolute URL for the application logo used in the email header
        var logoUrl   = $"{frontUrl}/icon-192.png";

        // Build the MIME message envelope
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        // The display name in the To header uses the recipient's first name
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Confirmez votre inscription";

        // Build both an HTML and a plain-text fallback body
        var bodyBuilder = new BodyBuilder
        {
            // Rich HTML email built by the private helper method
            HtmlBody = BuildHtml(prenom, confirmationUrl, logoUrl),
            // Plain-text fallback for email clients that do not render HTML
            TextBody = $"Bonjour {prenom},\n\nConfirmez votre compte ObriGénie en visitant ce lien :\n{confirmationUrl}\n\nLien valable 24 heures."
        };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            // Open a disposable SMTP client connection
            using var client = new SmtpClient();
            // Connect to the SMTP server using StartTLS on the configured port (typically 587)
            await client.ConnectAsync(
                smtp["Host"] ?? "smtp.gmail.com",
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            // Authenticate with the SMTP username and password from configuration
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            // Transmit the message
            await client.SendAsync(message);
            // Gracefully close the SMTP session
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log the error with the recipient address for diagnostics
            _logger.LogError(ex, "Échec d'envoi du mail de confirmation à {Email}", toEmail);
            // Re-throw so the caller can decide to swallow or propagate
            throw;
        }
    }

    /// <summary>
    /// Sends a welcome email to a user who just signed up via OAuth (Google or Microsoft).
    /// Unlike the confirmation email, this email does not contain an activation link —
    /// OAuth accounts are considered confirmed immediately by the provider.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="prenom">Recipient's first name, used to personalise the greeting</param>
    public async Task SendWelcomeEmailAsync(string toEmail, string prenom)
    {
        // Read SMTP settings from configuration
        var smtp      = _config.GetSection("EmailSettings");
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        var logoUrl   = $"{frontUrl}/icon-192.png";

        // Build the MIME message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Bienvenue !";

        var bodyBuilder = new BodyBuilder
        {
            // Rich HTML welcome email
            HtmlBody = BuildWelcomeHtml(prenom, frontUrl, logoUrl),
            // Plain-text fallback
            TextBody = $"Bonjour {prenom},\n\nBienvenue sur ObriGénie ! Votre compte est activé.\n\nAccédez à l'application : {frontUrl}"
        };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["Host"] ?? "smtp.gmail.com",
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Échec d'envoi du mail de bienvenue à {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Builds the HTML body for the email confirmation message.
    /// Uses an inline-style table-based layout for maximum email client compatibility.
    /// The confirmation button links to the provided URL; a plain-text fallback link is also shown.
    /// </summary>
    /// <param name="prenom">Recipient's first name for personalisation</param>
    /// <param name="confirmUrl">The full confirmation URL to embed in the button and fallback link</param>
    /// <param name="logoUrl">Absolute URL to the application logo image (not currently shown in the design)</param>
    /// <returns>A fully formed HTML string ready to be set as the email's HTML body</returns>
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

                  <!-- White header with text logo -->
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>

                  <!-- White body with greeting, explanation, button, and fallback link -->
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

                      <!-- Call-to-action button -->
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

                      <!-- Fallback plain-text link for email clients that block button clicks -->
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

                  <!-- Footer with copyright notice -->
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

    /// <summary>
    /// Builds the HTML body for the welcome email sent to new OAuth users.
    /// Similar layout to the confirmation email, but contains a direct app link
    /// instead of a confirmation button.
    /// </summary>
    /// <param name="prenom">Recipient's first name for personalisation</param>
    /// <param name="frontUrl">The base URL of the frontend application</param>
    /// <param name="logoUrl">Absolute URL to the application logo image</param>
    /// <returns>A fully formed HTML string ready to be set as the email's HTML body</returns>
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
                  <!-- White header with text logo -->
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>
                  <!-- White body with welcome message and app link button -->
                  <tr>
                    <td style="background:#ffffff;padding:40px 40px 40px;">
                      <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:22px;">Bienvenue, {prenom}&nbsp;! 🎉</h2>
                      <p style="margin:0 0 20px;color:#444;font-size:16px;line-height:1.6;">
                        Votre compte <strong>Obrigenie</strong> est activé et prêt à l'emploi.<br />
                        Organisez votre agenda scolaire dès maintenant.
                      </p>
                      <!-- Call-to-action button pointing directly to the app -->
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
                  <!-- Footer -->
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
