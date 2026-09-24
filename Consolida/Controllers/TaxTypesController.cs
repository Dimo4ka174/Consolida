using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.TaxTypeModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class TaxTypesController : Controller
    {
        private readonly ITaxTypeService _taxTypeService;
        private readonly ILogger<TaxTypesController> _logger;

        public TaxTypesController(
            ITaxTypeService taxTypeService,
            ILogger<TaxTypesController> logger)
        {
            _taxTypeService = taxTypeService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadTaxTypes")]
        public async Task<IActionResult> Index(int page = 1, string? searchString = null, string? searchStringMeasureUnit = null, string? sortOrder = "Name", int pageSize = 8)
        {
            var sortProperty = sortOrder?.Replace("_desc", "");
            if (sortProperty == "MeasureUnit")
                sortProperty = "MeasureUnit.Name";

            var parameters = new FilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchProperty = "Name",
                SortProperty = sortProperty,
                SortDirection = sortOrder?.EndsWith("_desc") ?? false ? "desc" : "asc"
            };

            if (!string.IsNullOrEmpty(searchStringMeasureUnit))
            {
                parameters.AdditionalFilters.Add(new FilterCondition
                {
                    PropertyPath = "MeasureUnit.Name",
                    SearchValue = searchStringMeasureUnit
                });
            }

            var result = await _taxTypeService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "TaxTypes";

            ViewBag.Paging = paging;
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchStringMeasureUnit = searchStringMeasureUnit;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchString", searchString ?? string.Empty },
                { "searchStringMeasureUnit", searchStringMeasureUnit ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty }
            };

            return View(result.Items);
        }

        [HttpGet]
        [Authorize(Policy = "CreateTaxTypes")]
        public async Task<IActionResult> Create()
        {
            var model = await _taxTypeService.GetCreateModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxTypeDto model)
        {
            if (!ModelState.IsValid)
            {
                var fresh = await _taxTypeService.GetCreateModelAsync();
                model.MeasureUnitsList = fresh.MeasureUnitsList;
                return View(model);
            }

            await _taxTypeService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditTaxTypes")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? searchStringMeasureUnit = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _taxTypeService.GetEditModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, searchStringMeasureUnit, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchStringMeasureUnit = searchStringMeasureUnit;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, TaxTypeDto model, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? searchStringMeasureUnit, [FromForm] string? sortOrder)
        {
            if (id == null || id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var fresh = await _taxTypeService.GetEditModelAsync(id.Value);
                model.MeasureUnitsList = fresh.MeasureUnitsList;
                ViewBag.ReturnPage = page;
                ViewBag.SearchString = searchString;
                ViewBag.SearchStringMeasureUnit = searchStringMeasureUnit;
                ViewBag.SortOrder = sortOrder;
                return View(model);
            }

            try
            {
                await _taxTypeService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _taxTypeService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, searchStringMeasureUnit, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteTaxTypes")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? searchStringMeasureUnit = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchStringMeasureUnit = searchStringMeasureUnit;
            ViewBag.SortOrder = sortOrder;

            var model = await _taxTypeService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchString, searchStringMeasureUnit, sortOrder });
            }

            ViewBag.IsSoftDelete = true;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? searchStringMeasureUnit, [FromForm] string? sortOrder)
        {
            if (id == null) return NotFound();

            try
            {
                await _taxTypeService.DeleteAsync(id);
                TempData["Success"] = "Тип налога помечен как удалённый.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TaxType ID: {Id}", id);
                TempData["Error"] = $"Ошибка при удалении типа налога: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, searchStringMeasureUnit, sortOrder });
        }
    }
}
