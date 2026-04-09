// Espace de noms limité au fichier (style C# 10+)
namespace seragenda.Services;

// Définit le contrat du service d'e-mails transactionnels.
// Les implémentations sont responsables de l'envoi d'e-mails via SMTP ou tout autre transport.
// Enregistré dans le conteneur DI en tant que service scopé ; la classe concrète est EmailService.
public interface IEmailService
{
    // Envoie un e-mail de confirmation de compte contenant un lien cliquable.
    // Le lien doit être ouvert par l'utilisateur pour activer son nouveau compte local.
    // toEmail : adresse e-mail du destinataire
    // prenom : prénom du destinataire, utilisé pour personnaliser le message de bienvenue
    // confirmationUrl : URL complète de confirmation (ex. https://obrigenie.app/confirm-email?token=...)
    //                   que l'utilisateur doit visiter pour terminer son inscription
    Task SendConfirmationEmailAsync(string toEmail, string prenom, string confirmationUrl);

    // Envoie un e-mail de bienvenue à un utilisateur qui vient de créer un compte via OAuth (Google ou Microsoft).
    // Aucun lien de confirmation n'est nécessaire car le fournisseur OAuth a déjà vérifié l'e-mail.
    // toEmail : adresse e-mail du destinataire
    // prenom : prénom du destinataire, utilisé pour personnaliser le message de bienvenue
    Task SendWelcomeEmailAsync(string toEmail, string prenom);
}
