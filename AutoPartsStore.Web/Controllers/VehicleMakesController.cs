using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

[Authorize(Roles = "Admin")]
public class VehicleMakesController : Controller
{
    private readonly IVehicleMakeRepository _vehicleMakeRepository;

    public VehicleMakesController(IVehicleMakeRepository vehicleMakeRepository)
    {
        _vehicleMakeRepository = vehicleMakeRepository;
    }

    public async Task<IActionResult> Index()
    {
        var makes = await _vehicleMakeRepository.GetAllOrderedAsync();
        return View(makes);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleMake make)
    {
        if (!ModelState.IsValid)
        {
            return View(make);
        }

        await _vehicleMakeRepository.AddAsync(make);
        await _vehicleMakeRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var make = await _vehicleMakeRepository.GetByIdAsync(id);
        if (make is null)
        {
            return NotFound();
        }

        return View(make);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleMake make)
    {
        if (id != make.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(make);
        }

        _vehicleMakeRepository.Update(make);
        await _vehicleMakeRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var make = await _vehicleMakeRepository.GetByIdAsync(id);
        if (make is null)
        {
            return NotFound();
        }

        return View(make);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var make = await _vehicleMakeRepository.GetByIdAsync(id);
        if (make is not null)
        {
            try
            {
                _vehicleMakeRepository.Remove(make);
                await _vehicleMakeRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Diese Marke kann nicht gelöscht werden, solange ihr noch Modelle zugeordnet sind.");
                return View(make);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
