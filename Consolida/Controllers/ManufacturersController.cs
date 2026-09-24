using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ManufacturerModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class ManufacturersController : Controller
    {
        private readonly IManufacturerService _manufacturerService;
        private readonly ILogger<ManufacturersController> _logger;

        public ManufacturersController(
            IManufacturerService manufacturerService,
            ILogger<ManufacturersController> logger)
        {
            _manufacturerService = manufacturerService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadManufacturers")]
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

            var result = await _manufacturerService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "Manufacturers";

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
        [Authorize(Policy = "CreateManufacturers")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ManufacturerDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _manufacturerService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditManufacturers")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _manufacturerService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ManufacturerDto model, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
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
                await _manufacturerService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _manufacturerService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteManufacturers")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var model = await _manufacturerService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
        {
            if (id == null) return NotFound();

            try
            {
                await _manufacturerService.DeleteAsync(id);
                TempData["Success"] = "Производитель успешно удалён.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Manufacturer ID: {Id}", id);
                TempData["Error"] = $"Ошибка при удалении производителя: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }
    }
}
