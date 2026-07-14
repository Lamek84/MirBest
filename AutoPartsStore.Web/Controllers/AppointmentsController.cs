using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;

namespace AutoPartsStore.Web.Controllers;

// Book — публичная форма записи на Termin (без регистрации, как форма контактов).
// Index/Confirm/Cancel — админ-панель со списком заявок.
public class AppointmentsController : Controller
{
    private const string RecipientEmail = "info@mirbest.de";

    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentRepository appointmentRepository,
        IEmailSender emailSender,
        ILogger<AppointmentsController> logger)
    {
        _appointmentRepository = appointmentRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Book(string? service)
    {
        PopulateOptions(service);
        var model = new AppointmentViewModel();
        if (!string.IsNullOrWhiteSpace(service))
        {
            // Vorbelegung, z. B. per Link von einer Detailing-Paket-Seite
            // (asp-route-service="..."). Siehe PopulateOptions — der Wert wird
            // der Dropdown-Liste hinzugefügt, falls er dort noch nicht steht.
            model.ServiceType = service;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("FormPolicy")]
    public async Task<IActionResult> Book(AppointmentViewModel model)
    {
        if (model.PreferredDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.PreferredDate), "Das Datum darf nicht in der Vergangenheit liegen.");
        }

        if (!ModelState.IsValid)
        {
            PopulateOptions(model.ServiceType);
            return View(model);
        }

        var appointment = new Appointment
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            VehicleInfo = model.VehicleInfo,
            ServiceType = model.ServiceType,
            PreferredDate = model.PreferredDate.Date,
            TimeSlot = model.TimeSlot,
            Message = model.Message,
            Status = AppointmentStatus.New
        };

        // Заявка сохраняется в БД независимо от того, дойдёт ли письмо —
        // тот же принцип, что и у формы контактов (см. ContactController).
        await _appointmentRepository.AddAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();

        try
        {
            var subject = $"Neue Terminanfrage: {model.ServiceType}";
            var body = "Name: " + model.Name
                + "\nTelefon: " + model.Phone
                + "\nE-Mail: " + model.Email
                + "\nFahrzeug: " + model.VehicleInfo
                + "\nLeistung: " + model.ServiceType
                + "\nWunschtermin: " + model.PreferredDate.ToString("dd.MM.yyyy") + " (" + model.TimeSlot + ")"
                + "\n\nNachricht:\n" + model.Message;

            await _emailSender.SendEmailAsync(RecipientEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Terminanfrage: E-Mail-Versand fehlgeschlagen.");
        }

        TempData["AppointmentSuccess"] = true;
        return RedirectToAction(nameof(Book));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var appointments = await _appointmentRepository.GetAllOrderedAsync();
        return View(appointments);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        await SetStatusAsync(id, AppointmentStatus.Confirmed);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        await SetStatusAsync(id, AppointmentStatus.Cancelled);
        return RedirectToAction(nameof(Index));
    }

    private async Task SetStatusAsync(int id, string status)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(id);
        if (appointment is not null)
        {
            appointment.Status = status;
            _appointmentRepository.Update(appointment);
            await _appointmentRepository.SaveChangesAsync();
        }
    }

    private void PopulateOptions(string? extraServiceType = null)
    {
        // Feste Liste + optional ein zusätzlicher Wert, der von außen übergeben
        // wurde (z. B. Name eines Detailing-Pakets) und noch nicht enthalten ist —
        // so bleibt die Liste dynamisch, ohne AppointmentServiceTypes anfassen zu müssen.
        var serviceTypes = AppointmentServiceTypes.All.ToList();
        if (!string.IsNullOrWhiteSpace(extraServiceType)
            && !serviceTypes.Contains(extraServiceType, StringComparer.OrdinalIgnoreCase))
        {
            serviceTypes.Add(extraServiceType);
        }

        ViewBag.ServiceTypes = new SelectList(serviceTypes);
        ViewBag.TimeSlots = new SelectList(AppointmentTimeSlots.All);
    }
}
