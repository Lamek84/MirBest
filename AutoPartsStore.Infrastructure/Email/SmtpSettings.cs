namespace AutoPartsStore.Infrastructure.Email;

// Биндится из секции "Smtp" в appsettings.json. Пока Host пуст — письма
// не отправляются (см. SmtpEmailSender), но заявки всё равно сохраняются в БД.
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
