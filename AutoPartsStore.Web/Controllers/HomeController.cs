using System.Diagnostics;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPartnerRepository _partnerRepository;

    public HomeController(ILogger<HomeController> logger, ICategoryRepository categoryRepository, IPartnerRepository partnerRepository)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _partnerRepository = partnerRepository;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.GetAllAsync();

        var partners = await _partnerRepository.GetAllAsync();
        ViewBag.Partners = partners.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList();

        return View(categories);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
