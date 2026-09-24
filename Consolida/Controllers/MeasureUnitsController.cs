using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.MeasureUnitModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class MeasureUnitsController : Controller
    {
        private readonly IMeasureUnitService _measureUnitService;
        private readonly ILogger<MeasureUnitsController> _logger;

        public MeasureUnitsController(
            IMeasureUnitService measureUnitService,
            ILogger<MeasureUnitsController> logger)
        {
            _measureUnitService = measureUnitService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadMeasureUnits")]
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

            var result = await _measureUnitService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "MeasureUnits";

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
        [Authorize(Policy = "CreateMeasureUnits")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MeasureUnitDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _measureUnitService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditMeasureUnits")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _measureUnitService.GetByIdAsync(id);
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
        public async Task<IActionResult> Edit(int? id, MeasureUnitDto model, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
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
                await _measureUnitService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _measureUnitService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteMeasureUnits")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var model = await _measureUnitService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
            }

            ViewBag.IsSoftDelete = true;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
        {
            if (id == null) return NotFound();

            try
            {
                await _measureUnitService.DeleteAsync(id);
                TempData["Success"] = "Единица измерения помечена как удалённая.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MeasureUnit ID: {Id}", id);
                TempData["Error"] = $"Ошибка при удалении единицы измерения: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }
    }
}
