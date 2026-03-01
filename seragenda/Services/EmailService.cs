using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace seragenda.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl)
    {
        var smtp      = _config.GetSection("EmailSettings");
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        var logoUrl   = $"{frontUrl}/icon-192.png";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Confirmez votre inscription";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildHtml(prenom, confirmationUrl, logoUrl),
            TextBody = $"Bonjour {prenom},\n\nConfirmez votre compte ObriGénie en visitant ce lien :\n{confirmationUrl}\n\nLien valable 24 heures."
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
            _logger.LogError(ex, "Échec d'envoi du mail de confirmation à {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string prenom)
    {
        var smtp      = _config.GetSection("EmailSettings");
        var fromEmail = smtp["FromEmail"] ?? "noreply@obrigenie.app";
        var fromName  = smtp["FromName"]  ?? "ObriGénie";
        var frontUrl  = _config["AppSettings:FrontendUrl"] ?? "https://obrigenie.app";
        var logoUrl   = $"{frontUrl}/icon-192.png";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(prenom, toEmail));
        message.Subject = "ObriGénie – Bienvenue !";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildWelcomeHtml(prenom, frontUrl, logoUrl),
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

                  <!-- Corps blanc -->
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

                      <!-- Bouton -->
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

                      <!-- Lien texte -->
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
                  <tr>
                    <td style="background:#ffffff;padding:32px 30px 24px;text-align:center;border-bottom:1px solid #eee;">
                      <span style="font-size:26px;font-weight:800;color:#1a1a2e;letter-spacing:-0.5px;">Obrigenie</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#ffffff;padding:40px 40px 40px;">
                      <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:22px;">Bienvenue, {prenom}&nbsp;! 🎉</h2>
                      <p style="margin:0 0 20px;color:#444;font-size:16px;line-height:1.6;">
                        Votre compte <strong>Obrigenie</strong> est activé et prêt à l'emploi.<br />
                        Organisez votre agenda scolaire dès maintenant.
                      </p>
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
