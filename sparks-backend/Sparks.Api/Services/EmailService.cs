using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Sparks.Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public virtual Task SendTemporaryPasswordEmailAsync(string toEmail, string toName, string tempPassword) =>
        SendAsync(
            toEmail,
            toName,
            "Votre compte SPARKS a été créé",
            $"""
                <p>Bonjour {toName},</p>
                <p>Un compte SPARKS a été créé pour vous.</p>
                <p>
                    Identifiant de connexion : <strong>{toEmail}</strong><br />
                    Mot de passe temporaire : <strong>{tempPassword}</strong>
                </p>
                <p>Merci de vous connecter et de changer ce mot de passe dès votre première connexion.</p>
                """
        );

    public virtual Task SendPasswordResetCodeEmailAsync(string toEmail, string toName, string code) =>
        SendAsync(
            toEmail,
            toName,
            "Réinitialisation de votre mot de passe SPARKS",
            $"""
                <p>Bonjour {toName},</p>
                <p>Voici votre code de vérification pour réinitialiser votre mot de passe SPARKS :</p>
                <p style="font-size: 24px; font-weight: 700; letter-spacing: 4px;">{code}</p>
                <p>Ce code expire dans 15 minutes. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
                """
        );

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("Smtp");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp["FromName"], smtp["FromAddress"]));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient { Timeout = 15000 };
        _logger.LogInformation("Sending email to {ToEmail}: connecting to {Host}:{Port}...", toEmail, smtp["Host"], smtp.GetValue<int>("Port"));
        await client.ConnectAsync(smtp["Host"], smtp.GetValue<int>("Port"), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        _logger.LogInformation("Email sent to {ToEmail} successfully.", toEmail);
    }
}
