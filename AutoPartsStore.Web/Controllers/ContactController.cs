using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AutoPartsStore.Web.Controllers;

public class ContactController : Controller
{
    // Реальный адрес MIRBEST, куда падают заявки с формы.
    private const string RecipientEmail = "info@mirbest.de";

    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactMessageRepository contactMessageRepository,
        IEmailSender emailSender,
        ILogger<ContactController> logger)
    {
        _contactMessageRepository = contactMessageRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("FormPolicy")]
    public async Task<IActionResult> Index(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entity = new ContactMessage
        {
            Name = model.Name,
            Email = model.Email,
            Message = model.Message
        };

        // Сначала сохраняем в БД — сообщение не потеряется, даже если
        // SMTP не настроен или временно недоступен.
        await _contactMessageRepository.AddAsync(entity);
        await _contactMessageRepository.SaveChangesAsync();

        try
        {
            var subject = $"Neue Kontaktanfrage von {model.Name}";
            var body = $"Name: {model.Name}\nE-Mail: {model.Email}\n\nNachricht:\n{model.Message}";
            await _emailSender.SendEmailAsync(RecipientEmail, subject, body);

            entity.EmailSent = true;
            _contactMessageRepository.Update(entity);
            await _contactMessageRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Заявка уже в БД — клиенту техническую ошибку SMTP не показываем,
            // только логируем для разработчика/админа.
            _logger.LogWarning(ex, "Kontaktformular: E-Mail-Versand fehlgeschlagen.");
        }

        TempData["ContactSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }
}
