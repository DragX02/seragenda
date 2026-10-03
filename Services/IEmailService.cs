namespace seragenda.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl);

    Task SendPasswordResetEmailAsync(string toEmail, string prenom, string resetUrl);

    Task SendWelcomeEmailAsync(string toEmail, string prenom);
}
