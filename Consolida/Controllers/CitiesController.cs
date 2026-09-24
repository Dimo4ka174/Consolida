using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Application.ViewModels.CityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class CitiesController : Controller
    {
        private readonly ICityService _cityService;
        private readonly ILogger<CitiesController> _logger;

        public CitiesController(ICityService cityService, ILogger<CitiesController> logger)
        {
            _cityService = cityService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadCities")]
        public async Task<IActionResult> Index(int page = 1, string? searchString = null, string? sortOrder = "Name", int pageSize = 8)
        {
            var parameters = new FilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchProperty = "Name",
                SortProperty = sortOrder?.Replace("_desc", ""),
                SortDirection = sortOrder?.EndsWith("_desc") ?? false ? "desc" : "asc"
            };

            var result = await _cityService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "Cities";

            ViewBag.Paging = paging;
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchString", searchString ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty }
            };

            return View(result.Items);
        }

        [HttpGet]
        [Authorize(Policy = "CreateCities")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CityDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _cityService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditCities")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var city = await _cityService.GetByIdAsync(id);
            if (city == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, CityDto model, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
        {
            if (id == null || id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnPage = page;
                ViewBag.SearchString = searchString;
                ViewBag.SortOrder = sortOrder;
                return View(model);
            }

            try
            {
                await _cityService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _cityService.CityExistsAsync(model.Id.Value))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteCities")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var city = await _cityService.GetByIdAsync(id);
            if (city == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
            }

            ViewBag.RelatedCompaniesCount = await _cityService.GetRelatedCompaniesCountAsync(id.Value);
            return View(city);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
        {
            try
            {
                await _cityService.DeleteCityWithRelatedDataAsync(id.Value);
                TempData["Success"] = "Город успешно удалён вместе со всеми связанными компаниями.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting City ID: {CityId}", id);
                TempData["Error"] = $"Ошибка при удалении города: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }
    }
}
