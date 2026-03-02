// File-scoped namespace (C# 10+ style)
namespace seragenda.Services;

/// <summary>
/// Defines the contract for the transactional email service.
/// Implementations are responsible for sending emails via SMTP or any other transport.
/// Registered in the DI container as a scoped service; the concrete class is <see cref="EmailService"/>.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an account confirmation email containing a clickable link.
    /// The link must be opened by the user to activate their newly created local account.
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="prenom">The recipient's first name, used to personalise the email greeting</param>
    /// <param name="confirmationUrl">
    /// The full confirmation URL (e.g., https://obrigenie.app/confirm-email?token=...)
    /// that the user must visit to complete their registration.
    /// </param>
    Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl);

    /// <summary>
    /// Sends a welcome email to a user who just created an account via OAuth (Google or Microsoft).
    /// No confirmation link is needed because the OAuth provider already verified the email.
    /// </summary>
    /// <param name="toEmail">The recipient's email address</param>
    /// <param name="prenom">The recipient's first name, used to personalise the email greeting</param>
    Task SendWelcomeEmailAsync(string toEmail, string prenom);
}
