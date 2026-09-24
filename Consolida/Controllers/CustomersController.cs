using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CustomerModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Consolida.Controllers
{
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadCustomers")]
        public async Task<IActionResult> Index(int page = 1, string? searchStringCompany = null, string? searchStringFirstName = null, string? searchStringLastName = null, string? sortOrder = "LastName", int pageSize = 8)
        {
            var parameters = new FilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchStringFirstName,
                SearchProperty = "FirstName",
                SortProperty = sortOrder?.Replace("_desc", ""),
                SortDirection = sortOrder?.EndsWith("_desc") ?? false ? "desc" : "asc"
            };

            if (!string.IsNullOrEmpty(searchStringCompany))
                parameters.AdditionalFilters.Add(new FilterCondition { PropertyPath = "Company.Name", SearchValue = searchStringCompany });
            if (!string.IsNullOrEmpty(searchStringLastName))
                parameters.AdditionalFilters.Add(new FilterCondition { PropertyPath = "LastName", SearchValue = searchStringLastName });

            var result = await _customerService.GetFilteredPagedAsync(parameters);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                TempData["Error"] = result.ErrorMessage;

            var paging = PagingHelpers.Create(result.TotalItems, parameters.Page, parameters.PageSize);
            paging.NamePage = "Customers";

            ViewBag.Paging = paging;
            ViewBag.CurrentPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringFirstName = searchStringFirstName;
            ViewBag.SearchStringLastName = searchStringLastName;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchStringCompany", searchStringCompany ?? string.Empty },
                { "searchStringFirstName", searchStringFirstName ?? string.Empty },
                { "searchStringLastName", searchStringLastName ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty }
            };

            return View(result.Items);
        }

        [HttpGet]
        [Authorize(Policy = "CreateCustomers")]
        public async Task<IActionResult> Create()
        {
            var model = await _customerService.GetCreateModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerDto model)
        {
            if (!ModelState.IsValid)
            {
                var fresh = await _customerService.GetCreateModelAsync();
                model.CompaniesList = fresh.CompaniesList;
                model.PreferredMethodsList = fresh.PreferredMethodsList;
                return View(model);
            }

            await _customerService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditCustomers")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchStringCompany = null, [FromQuery] string? searchStringFirstName = null, [FromQuery] string? searchStringLastName = null, [FromQuery] string? sortOrder = "LastName")
        {
            if (id == null) return NotFound();

            var model = await _customerService.GetEditModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringFirstName, searchStringLastName, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringFirstName = searchStringFirstName;
            ViewBag.SearchStringLastName = searchStringLastName;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, CustomerDto model, [FromForm] int page, [FromForm] string? searchStringCompany, [FromForm] string? searchStringFirstName, [FromForm] string? searchStringLastName, [FromForm] string? sortOrder)
        {
            if (id == null || id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var fresh = await _customerService.GetEditModelAsync(id.Value);
                model.CompaniesList = fresh.CompaniesList;
                model.PreferredMethodsList = fresh.PreferredMethodsList;
                ViewBag.ReturnPage = page;
                ViewBag.SearchStringCompany = searchStringCompany;
                ViewBag.SearchStringFirstName = searchStringFirstName;
                ViewBag.SearchStringLastName = searchStringLastName;
                ViewBag.SortOrder = sortOrder;
                return View(model);
            }

            try
            {
                await _customerService.UpdateAsync(id.Value, model);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _customerService.ExistsAsync(model.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringFirstName, searchStringLastName, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteCustomers")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchStringCompany = null, [FromQuery] string? searchStringFirstName = null, [FromQuery] string? searchStringLastName = null, [FromQuery] string? sortOrder = "LastName")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchStringCompany = searchStringCompany;
            ViewBag.SearchStringFirstName = searchStringFirstName;
            ViewBag.SearchStringLastName = searchStringLastName;
            ViewBag.SortOrder = sortOrder;

            var model = await _customerService.GetByIdAsync(id);
            if (model == null)
            {
                TempData["Message"] = "Сущность не найдена.";
                return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringFirstName, searchStringLastName, sortOrder });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchStringCompany, [FromForm] string? searchStringFirstName, [FromForm] string? searchStringLastName, [FromForm] string? sortOrder)
        {
            try
            {
                await _customerService.DeleteAsync(id);
                TempData["Success"] = "Клиент успешно удален вместе со всеми связанными заказами.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Customer ID: {CustomerId}", id);
                TempData["Error"] = $"Ошибка при удалении клиента: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchStringCompany, searchStringFirstName, searchStringLastName, sortOrder });
        }
    }
}
