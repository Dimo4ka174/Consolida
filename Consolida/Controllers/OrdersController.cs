using System.Text;
using System.Text.Json;
using Application.DataAccessLayer.CacheService;
using Application.DataAccessLayer.Interface.CalculationService;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.OrderModel;
using Application.ViewModels.OrderModel.Products;
using DB.Entity.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    public class OrdersController : Controller
    {
        #region Поля
        private readonly IOrderListService _orderListService;
        private readonly IOrderDeleteService _orderDeleteService;
        private readonly IOrderDetailsService _orderDetailsService;
        private readonly IOrderCreationService _orderCreationService;
        private readonly IOrderSaveService _orderSaveService;
        private readonly ICalculationService _calculationService;
        private readonly ICalculationExportService _calculationExportService;
        private readonly IOrderDuplicateService _orderDuplicateService;
        private readonly ICascadeSoftDeleteService _deleteService;
        private readonly IOrderNumberGenerator _orderNumberGenerator;
        private readonly IEnumCacheService _enumCacheService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderListService orderListService,
            IOrderDeleteService orderDeleteService,
            IOrderDetailsService orderDetailsService,
            IOrderCreationService orderCreationService,
            IOrderSaveService orderSaveService,
            ICalculationService calculationService,
            ICalculationExportService calculationExportService,
            IOrderDuplicateService orderDuplicateService,
            ICascadeSoftDeleteService deleteService,
            IOrderNumberGenerator orderNumberGenerator,
            IEnumCacheService enumCacheService,
            ILogger<OrdersController> logger)
        {
            _orderListService = orderListService;
            _orderDeleteService = orderDeleteService;
            _orderDetailsService = orderDetailsService;
            _orderCreationService = orderCreationService;
            _orderSaveService = orderSaveService;
            _calculationService = calculationService;
            _calculationExportService = calculationExportService;
            _orderDuplicateService = orderDuplicateService;
            _deleteService = deleteService;
            _orderNumberGenerator = orderNumberGenerator;
            _enumCacheService = enumCacheService;
            _logger = logger;
        }
        #endregion

        [HttpGet]
        [Authorize(Policy = "ReadOrders")]
        public async Task<IActionResult> Index(
            int page = 1,
            string? searchCompanyName = null,
            string? searchOrderNumber = null,
            Priority? priority = null,
            Status? status = null,
            DateTime? searchDateFrom = null,
            DateTime? searchDateTo = null,
            string? sortOrder = "Date_desc",
            decimal? minAmount = null,
            decimal? maxAmount = null,
            int pageSize = 8)
        {
            var filter = new OrderFilterParams
            {
                Page = page,
                PageSize = pageSize,
                SearchCompanyName = searchCompanyName,
                SearchOrderNumber = searchOrderNumber,
                Priority = priority,
                Status = status,
                SearchDateFrom = searchDateFrom,
                SearchDateTo = searchDateTo,
                SortOrder = sortOrder,
                MinAmount = minAmount,
                MaxAmount = maxAmount
            };

            var orders = await _orderListService.GetOrdersAsync(filter);
            var priorities = await _orderListService.GetPriorityListAsync();
            var statuses = await _orderListService.GetStatusListAsync();

            ViewBag.Paging = PagingHelpers.Create(orders.TotalItems, page, pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.searchCompanyName = searchCompanyName;
            ViewBag.SearchOrderNumber = searchOrderNumber;
            ViewBag.Priority = priority;
            ViewBag.Status = status;
            ViewBag.Priorities = priorities;
            ViewBag.Statuses = statuses;
            ViewBag.SearchDateFrom = searchDateFrom?.ToString("yyyy-MM-dd");
            ViewBag.SearchDateTo = searchDateTo?.ToString("yyyy-MM-dd");
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;

            ViewBag.FilterParams = new Dictionary<string, object>
            {
                { "searchCompanyName", searchCompanyName ?? string.Empty },
                { "searchOrderNumber", searchOrderNumber ?? string.Empty },
                { "priority", priority?.ToString() ?? string.Empty },
                { "status", status?.ToString() ?? string.Empty },
                { "searchDateFrom", searchDateFrom?.ToString("yyyy-MM-dd") ?? string.Empty },
                { "searchDateTo", searchDateTo?.ToString("yyyy-MM-dd") ?? string.Empty },
                { "sortOrder", sortOrder ?? string.Empty },
                { "minAmount", minAmount?.ToString() ?? string.Empty },
                { "maxAmount", maxAmount?.ToString() ?? string.Empty }
            };

            return View(orders.Items);
        }

        [HttpGet, ActionName("Details")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Details(int id)
        {
            var viewModel = await _orderDetailsService.GetOrderDetailsAsync(id);
            if (viewModel == null) return NotFound();

            return View(viewModel);
        }

        [HttpPost, ActionName("SaveDataOrder")]
        [Authorize(Policy = "CreateOrders")]
        [Consumes("application/json")]
        public async Task<IActionResult> SaveDataOrder([FromBody] ExportOrderViewModel model)
        {
            TempData.Remove("SuccessMessage");
            TempData.Remove("ErrorMessage");
            TempData.Remove("DownloadLink");
            TempData.Remove("OptimizationMessage");

            try
            {
                var saveResult = await _orderSaveService.SaveOrderDataAsync(model);
                if (!saveResult.Success) return BadRequest(saveResult.ErrorMessage);

                if (model.DownloadDocument)
                {
                    var excelStream = await _orderSaveService.GenerateTKPAsync(model, saveResult.Order);

                    if (!string.IsNullOrEmpty(model.DeliveryOptimizationMessage))
                    {
                        TempData["OptimizationMessage"] = model.DeliveryOptimizationMessage;
                        TempData["OptimizationMessageShown"] = false;
                    }

                    TempData["SuccessMessage"] = "Данные сохранены. ТКП успешно сформировано.";

                    var year = DateTime.Now.ToString("yy");
                    var tkpNumber = model.TkpSettings?.TkpNumber?.Trim();
                    if (string.IsNullOrEmpty(tkpNumber))
                        tkpNumber = saveResult.Order.Id?.ToString() ?? "";

                    var fileName = $"05-{tkpNumber}_{year} ТКП.xlsx";

                    return File(excelStream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }

                TempData["SuccessMessage"] = "Данные успешно сохранены.";
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during saving order data");
                TempData["ErrorMessage"] = "Произошла ошибка при сохранении данных.";
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost, ActionName("ExportCalculation")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> ExportCalculation([FromBody] ExportOrderViewModel model)
        {
            try
            {
                var calculationData = await _calculationService.BuildCalculationViewModelAsync(model, User.Identity?.Name ?? "System");
                var excelStream = await _calculationExportService.ExportCalculationAsync(calculationData);

                return File(excelStream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Расчет_заказа_{calculationData.OrderNumber}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при экспорте расчета");
                return StatusCode(500, new { error = "Произошла ошибка при формировании расчета." });
            }
        }

        [HttpPost, ActionName("DuplicateOrder")]
        [Authorize(Policy = "CreateOrders")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuplicateOrder([FromBody] DuplicateOrderViewModel model)
        {
            try
            {
                var duplicateResult = await _orderDuplicateService.DuplicateOrderAsync(model);

                if (!duplicateResult.Success)
                {
                    return BadRequest(duplicateResult.ErrorMessage);
                }

                return Ok(new
                {
                    success = true,
                    newOrderId = duplicateResult.NewOrderId,
                    newOrderNumber = duplicateResult.NewOrderNumber,
                    message = "Заказ успешно продублирован"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating order");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet, ActionName("Create")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Create(bool fromExcel = false, string? excelFileName = null)
        {
            string orderNumber = (fromExcel && !string.IsNullOrEmpty(excelFileName))
                ? await _orderNumberGenerator.GenerateOrderNumber(excelFileName)
                : await _orderNumberGenerator.GenerateOrderNumber();

            var model = new OrderFormDto { OrderNumber = orderNumber };
            await PrepareCreateModel(model);
            return View(model);
        }

        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderFormDto model)
        {
            await PrepareCreateModel(model);
            var result = await _orderCreationService.CreateOrderAsync(model, User.Identity?.Name ?? "System");

            if (result.Success)
            {
                HttpContext.Session.Remove("UploadedProducts");
                TempData["SuccessMessage"] = "Заказ успешно создан.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }

            TempData["ErrorMessage"] = string.Join("; ", result.Errors);
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "DeleteOrders")]
        public async Task<IActionResult> Delete(
            int? id,
            [FromQuery] int page = 1,
            [FromQuery] string? searchCompanyName = null,
            [FromQuery] string? searchOrderNumber = null,
            [FromQuery] Priority? priority = null,
            [FromQuery] Status? status = null,
            [FromQuery] DateTime? searchDateFrom = null,
            [FromQuery] DateTime? searchDateTo = null,
            [FromQuery] string? sortOrder = "Date_desc",
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null)
        {
            if (id == null) return NotFound();

            var model = await _orderDeleteService.GetDeleteModelAsync(id.Value);
            if (model == null)
            {
                TempData["Message"] = "Заказ не найден.";
                return RedirectToAction(nameof(Index), new { page, searchCompanyName, searchOrderNumber, priority, status, searchDateFrom, searchDateTo, sortOrder, minAmount, maxAmount });
            }

            ViewBag.ReturnPage = page;
            ViewBag.searchCompanyName = searchCompanyName;
            ViewBag.SearchOrderNumber = searchOrderNumber;
            ViewBag.Priority = priority;
            ViewBag.Status = status;
            ViewBag.SearchDateFrom = searchDateFrom?.ToString("yyyy-MM-dd");
            ViewBag.SearchDateTo = searchDateTo?.ToString("yyyy-MM-dd");
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;

            ViewBag.OrderProductsCount = model.Products.Count;
            ViewBag.OrderTaxesCount = await _orderDeleteService.GetOrderTaxesCountAsync(id.Value);

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int? id,
            [FromForm] int page,
            [FromForm] string? searchCompanyName,
            [FromForm] string? searchOrderNumber,
            [FromForm] Priority? priority,
            [FromForm] Status? status,
            [FromForm] DateTime? searchDateFrom,
            [FromForm] DateTime? searchDateTo,
            [FromForm] string? sortOrder,
            [FromForm] decimal? minAmount,
            [FromForm] decimal? maxAmount)
        {
            try
            {
                await _deleteService.DeleteOrder(id.Value);
                TempData["Success"] = "Заказ успешно удален вместе со всеми связанными данными.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Order ID: {OrderId}", id);
                TempData["Error"] = $"Ошибка при удалении заказа: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { page, searchCompanyName, searchOrderNumber, priority, status, searchDateFrom, searchDateTo, sortOrder, minAmount, maxAmount });
        }

        private async Task PrepareCreateModel(OrderFormDto model)
        {
            model.PrioritiesList = _enumCacheService.GetCachedEnumList<Priority>();

            var uploadedProductsJson = HttpContext.Session.GetString("UploadedProducts");
            if (!string.IsNullOrEmpty(uploadedProductsJson))
            {
                model.Products = JsonSerializer.Deserialize<List<OrderProductViewModel>>(uploadedProductsJson)
                    ?? new List<OrderProductViewModel>();
            }
        }
    }
}
