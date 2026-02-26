namespace seragenda.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl);
}
