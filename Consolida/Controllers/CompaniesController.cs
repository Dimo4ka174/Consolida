using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CompanyModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadCompanies")]
        public async Task<IActionResult> Index(int page = 1, string? searchStringCompany = null, string? searchStringCity = null, string? sortOrder = "Name", int pageSize = 8)
        {
            var parameters = new FilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchStringCompany,
                SearchProperty = "Name",
                SortProperty = sortOrder?.Replace("_desc", ""),
                SortDirection = sortOrder?.EndsWith("_desc") ?? false ? "desc" : "asc"
            };

            if (!string.IsNullOrEmpty(searchStringCity))
            {
                parameters.AdditionalFilters.Add(new FilterCondition
                {
                    PropertyPath = "City.Name",
                    SearchValue = searchStringCity
                });
            }

            var result = await _companyService.GetPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "Companies";

            ViewBag.Paging = paging;
            ViewBag.CurrentPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringCity = searchStringCity;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchStringCompany", searchStringCompany ?? string.Empty },
                { "searchStringCity", searchStringCity ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty }
            };

            return View(result.Items);
        }

        [HttpGet]
        [Authorize(Policy = "CreateCompanies")]
        public async Task<IActionResult> Create()
        {
            var model = await _companyService.GetCreateModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyDto model)
        {
            if (!ModelState.IsValid)
            {
                // если модель невалидна, нужно снова заполнить списки
                var freshModel = await _companyService.GetCreateModelAsync();
                model.CitiesList = freshModel.CitiesList;
                model.CountriesList = freshModel.CountriesList;
                return View(model);
            }

            await _companyService.CreateCompanyAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditCompanies")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchStringCompany = null, [FromQuery] string? searchStringCity = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _companyService.GetEditModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringCity, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringCity = searchStringCity;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, CompanyDto model, [FromForm] int page, [FromForm] string? searchStringCompany, [FromForm] string? searchStringCity, [FromForm] string? sortOrder)
        {
            if (id == null || id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var freshModel = await _companyService.GetEditModelAsync(id.Value);
                model.CitiesList = freshModel.CitiesList;
                model.CountriesList = freshModel.CountriesList;
                ViewBag.ReturnPage = page;
                ViewBag.SearchStringCompany = searchStringCompany;
                ViewBag.SearchStringCity = searchStringCity;
                ViewBag.SortOrder = sortOrder;
                return View(model);
            }

            try
            {
                await _companyService.UpdateCompanyAsync(model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _companyService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringCity, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteCompanies")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchStringCompany = null, [FromQuery] string? searchStringCity = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringCity = searchStringCity;
            ViewBag.SortOrder = sortOrder;

            var model = await _companyService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringCity, sortOrder });
            }

            ViewBag.RelatedCustomersCount = await _companyService.GetRelatedCustomersCountAsync(id.Value);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchStringCompany, [FromForm] string? searchStringCity, [FromForm] string? sortOrder)
        {
            try
            {
                await _companyService.DeleteCompanyAsync(id.Value);
                TempData["Success"] = "Компания успешно удалена вместе со всеми связанными клиентами и заказами.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Company ID: {CompanyId}", id);
                TempData["Error"] = $"Ошибка при удалении компании: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringCity, sortOrder });
        }
    }
}
