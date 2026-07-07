namespace AutoPartsStore.Core.Interfaces;

// Абстракция отправки почты — контроллеры и остальной код зависят только
// от неё, а не от конкретного SMTP-клиента (см. Infrastructure/Email/SmtpEmailSender).
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
