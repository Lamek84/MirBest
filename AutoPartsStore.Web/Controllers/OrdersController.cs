using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderRepository _orderRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(IOrderRepository orderRepository, UserManager<ApplicationUser> userManager)
    {
        _orderRepository = orderRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var orders = await _orderRepository.GetByUserAsync(userId);
        return View(orders);
    }

    private const int AdminOrdersPageSize = 30;

    // Admin-Übersicht "Alle Bestellungen" — alle Kunden, neueste zuerst,
    // gefiltert nach Status/Datum und seitenweise (sonst irgendwann unbrauchbar lang).
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminIndex(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        var (orders, totalCount) = await _orderRepository.SearchAllAsync(status, fromDate, toDate, page, AdminOrdersPageSize);

        // Order kennt nur die UserId (String) — Kunden-E-Mail für die Anzeige
        // gesammelt aus Identity nachschlagen, statt pro Bestellung einzeln.
        var userIds = orders.Select(o => o.UserId).Distinct().ToList();
        var emailsById = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id);
        ViewBag.CustomerEmails = emailsById;

        // SelectList übernimmt die "selected"-Markierung selbst (siehe ProductsController
        // für dasselbe Muster) — sicherer als ein manuelles selected="@(...)" im View,
        // das bei ViewBag (dynamic) nicht zuverlässig als Bool ausgewertet würde.
        ViewBag.StatusOptions = new SelectList(
            new[] { OrderStatus.PendingPayment, OrderStatus.Paid, OrderStatus.PaymentFailed, OrderStatus.Cancelled },
            status);

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)AdminOrdersPageSize));
        ViewBag.TotalCount = totalCount;

        return View(orders);
    }

    // Alte Bestellungen aus "Alle Bestellungen" entfernen — z. B. Test-/Fehlbestellungen
    // oder einfach Aufräumen nach längerer Zeit. Items werden per Cascade-Delete
    // mitgelöscht (siehe OrderConfiguration), Bestand/Bonuspunkte bleiben unangetastet.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is not null)
        {
            _orderRepository.Remove(order);
            await _orderRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(AdminIndex));
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _orderRepository.GetByIdWithItemsAsync(id);

        if (order is null)
        {
            return NotFound();
        }

        if (order.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return View(order);
    }
}
