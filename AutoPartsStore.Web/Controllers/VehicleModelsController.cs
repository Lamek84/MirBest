using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

[Authorize(Roles = "Admin")]
public class VehicleModelsController : Controller
{
    private readonly IVehicleModelRepository _vehicleModelRepository;
    private readonly IVehicleMakeRepository _vehicleMakeRepository;

    public VehicleModelsController(IVehicleModelRepository vehicleModelRepository, IVehicleMakeRepository vehicleMakeRepository)
    {
        _vehicleModelRepository = vehicleModelRepository;
        _vehicleMakeRepository = vehicleMakeRepository;
    }

    public async Task<IActionResult> Index()
    {
        var models = await _vehicleModelRepository.GetAllWithMakeAsync();
        return View(models);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateMakesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateMakesAsync(model.VehicleMakeId);
            return View(model);
        }

        await _vehicleModelRepository.AddAsync(model);
        await _vehicleModelRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateMakesAsync(model.VehicleMakeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateMakesAsync(model.VehicleMakeId);
            return View(model);
        }

        _vehicleModelRepository.Update(model);
        await _vehicleModelRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var model = await _vehicleModelRepository.GetByIdAsync(id);
        if (model is not null)
        {
            try
            {
                _vehicleModelRepository.Remove(model);
                await _vehicleModelRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Dieses Modell kann nicht gelöscht werden, solange es noch Ersatzteilen zugeordnet ist.");
                return View(model);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateMakesAsync(int? selectedMakeId = null)
    {
        var makes = await _vehicleMakeRepository.GetAllOrderedAsync();
        ViewBag.VehicleMakeId = new SelectList(makes, "Id", "Name", selectedMakeId);
    }
}
