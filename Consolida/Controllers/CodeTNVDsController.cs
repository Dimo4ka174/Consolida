using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CodeTNVDModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class CodeTNVDsController : Controller
    {
        private readonly ICodeTNVDService _codeTNVDService;
        private readonly ILogger<CodeTNVDsController> _logger;

        public CodeTNVDsController(
            ICodeTNVDService codeTNVDService,
            ILogger<CodeTNVDsController> logger)
        {
            _codeTNVDService = codeTNVDService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadCodesTNVD")]
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

            var result = await _codeTNVDService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TempData["Error"] = result.ErrorMessage;
            }

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "CodesTNVD";

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
        [Authorize(Policy = "CreateCodesTNVD")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CodeTNVDdto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _codeTNVDService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditCodesTNVD")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _codeTNVDService.GetByIdAsync(id);
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
        public async Task<IActionResult> Edit(int? id, CodeTNVDdto model, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? sortOrder)
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
                await _codeTNVDService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _codeTNVDService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteCodesTNVD")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var model = await _codeTNVDService.GetByIdAsync(id);
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
            try
            {
                await _codeTNVDService.DeleteAsync(id);
                TempData["Success"] = "Код ТНВЭД помечен как удалённый.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CodeTNVD ID: {CodeTNVDId}", id);
                TempData["Error"] = $"Ошибка при удалении кода ТНВЭД: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, sortOrder });
        }
    }
}
