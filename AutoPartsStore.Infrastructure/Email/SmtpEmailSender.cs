using System.Net;
using System.Net.Mail;
using AutoPartsStore.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace AutoPartsStore.Infrastructure.Email;

// Простая реализация на встроенном SmtpClient — без внешних NuGet-пакетов.
// Для продакшена можно заменить на MailKit, интерфейс IEmailSender не изменится.
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            // SMTP не настроен (Smtp:Host пуст в appsettings.json) —
            // явно сигнализируем об этом вызывающему коду вместо того,
            // чтобы падать с невнятной сетевой ошибкой.
            throw new InvalidOperationException("SMTP ist nicht konfiguriert (Smtp:Host fehlt in appsettings.json).");
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}
