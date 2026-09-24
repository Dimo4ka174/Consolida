using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.ProductModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadProducts")]
        public async Task<IActionResult> Index(int page = 1, string? searchString = null, string? searchManufacturer = null, string? sortOrder = "Name", int pageSize = 8)
        {
            var result = await _productService.GetFilteredPagedAsync(page, pageSize, searchString, searchManufacturer, sortOrder);

            ViewBag.Paging = PagingHelpers.Create(result.TotalItems, page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchManufacturer = searchManufacturer;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Manufacturers = await GetManufacturersForFilter();

            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchString", searchString ?? string.Empty },
                { "searchManufacturer", searchManufacturer ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty }
            };

            return View(result.Items);
        }

        [HttpGet]
        [Authorize(Policy = "CreateProducts")]
        public async Task<IActionResult> Create()
        {
            var model = await _productService.GetCreateModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                var fresh = await _productService.GetCreateModelAsync();
                dto.ManufacturersList = fresh.ManufacturersList;
                return View(dto);
            }

            await _productService.CreateAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "EditProducts")]
        public async Task<IActionResult> Edit(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? searchManufacturer = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            var model = await _productService.GetEditModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Продукт не найден.";
                return RedirectToAction(nameof(Index), new { page, searchString, searchManufacturer, sortOrder });
            }

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchManufacturer = searchManufacturer;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ProductDto dto, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? searchManufacturer, [FromForm] string? sortOrder)
        {
            if (id == null || id != dto.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var fresh = await _productService.GetEditModelAsync(id.Value);
                dto.ManufacturersList = fresh.ManufacturersList;
                ViewBag.ReturnPage = page;
                ViewBag.SearchString = searchString;
                ViewBag.SearchManufacturer = searchManufacturer;
                ViewBag.SortOrder = sortOrder;
                return View(dto);
            }

            try
            {
                await _productService.UpdateAsync(id.Value, dto);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _productService.ExistsAsync(dto.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page, searchString, searchManufacturer, sortOrder });
        }

        [HttpGet]
        [Authorize(Policy = "DeleteProducts")]
        public async Task<IActionResult> Delete(int? id, [FromQuery] int page = 1, [FromQuery] string? searchString = null, [FromQuery] string? searchManufacturer = null, [FromQuery] string? sortOrder = "Name")
        {
            if (id == null) return NotFound();

            ViewBag.ReturnPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SearchManufacturer = searchManufacturer;
            ViewBag.SortOrder = sortOrder;

            var model = await _productService.GetEditModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Продукт не найден.";
                return RedirectToAction(nameof(Index), new { page, searchString, searchManufacturer, sortOrder });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id, [FromForm] int page, [FromForm] string? searchString, [FromForm] string? searchManufacturer, [FromForm] string? sortOrder)
        {
            if (id == null) return NotFound();

            try
            {
                await _productService.DeleteAsync(id);
                TempData["Success"] = "Продукт успешно удалён.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Product ID: {Id}", id);
                TempData["Error"] = $"Ошибка при удалении продукта: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchString, searchManufacturer, sortOrder });
        }

        private Task<List<SelectListItem>> GetManufacturersForFilter() => _productService.GetManufacturersSelectListAsync();
    }
}
