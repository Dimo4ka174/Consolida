using Application.DataAccessLayer.Interface.OrderService;
using Application.ViewModels.OrderModel.Products;
using Application.ViewModels.OrderModel.Api;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Consolida.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly IOrderLookupService _lookupService;
        private readonly IOrderTaxManagementService _taxManagementService;
        private readonly IOrderStatusService _statusService;
        private readonly IQuickCreateService _quickCreateService;
        private readonly ILogger<OrderAPIController> _logger;

        public OrderAPIController(
            IOrderLookupService lookupService,
            IOrderTaxManagementService taxManagementService,
            IOrderStatusService statusService,
            IQuickCreateService quickCreateService,
            ILogger<OrderAPIController> logger)
        {
            _lookupService = lookupService;
            _taxManagementService = taxManagementService;
            _statusService = statusService;
            _quickCreateService = quickCreateService;
            _logger = logger;
        }

        // ============================================================
        // Lookup
        // ============================================================

        [HttpGet("SearchCodes")]
        public async Task<IActionResult> SearchCodes(string query, CancellationToken ct)
        {
            try
            {
                var codes = await _lookupService.SearchCodesAsync(query, ct);
                return Ok(codes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска кода ТН ВЭД");
                return StatusCode(500, new { success = false, message = "Ошибка поиска кода" });
            }
        }

        [HttpGet("SearchMetrologicalInfo")]
        public async Task<IActionResult> SearchMetrologicalInfo(string number, CancellationToken ct)
        {
            try
            {
                var info = await _lookupService.SearchMetrologicalInfoAsync(number, ct);
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска данных первичной поверки {Number}", number);
                return StatusCode(500, new { success = false, message = "Ошибка поиска" });
            }
        }

        [HttpGet("SearchTaxTypes")]
        public async Task<IActionResult> SearchTaxTypes(string query, CancellationToken ct)
        {
            try
            {
                var taxTypes = await _lookupService.SearchTaxTypesAsync(query, ct);
                return Ok(taxTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска типов налогов");
                return StatusCode(500, new { success = false, message = "Ошибка поиска" });
            }
        }

        [HttpGet("SearchCompanies")]
        public async Task<IActionResult> SearchCompanies(string query, CancellationToken ct)
        {
            try
            {
                var companies = await _lookupService.SearchCompaniesAsync(query, ct);
                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска компаний");
                return StatusCode(500, new { success = false, message = "Ошибка поиска" });
            }
        }

        [HttpGet("GetCustomersByCompany")]
        public async Task<IActionResult> GetCustomersByCompany(int companyId, CancellationToken ct)
        {
            try
            {
                var customers = await _lookupService.GetCustomersByCompanyAsync(companyId, ct);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                return StatusCode(500, new { error = "Ошибка загрузки клиентов" });
            }
        }

        // ============================================================
        // Tax management
        // ============================================================

        [HttpPost("UpdateCodeRate")]
        public async Task<IActionResult> UpdateCodeRate([FromBody] UpdateCodeRateRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _taxManagementService.UpdateCodeRateAsync(request, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления ставки кода ТН ВЭД");
                return StatusCode(500, new { success = false, message = "Ошибка обновления ставки" });
            }
        }

        [HttpPost("AddTaxToOrder")]
        public async Task<IActionResult> AddTaxToOrder([FromBody] AddTaxToOrderRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _taxManagementService.AddTaxToOrderAsync(request.OrderId, request.TaxTypeId, ct);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    tax = new
                    {
                        Id = result.TaxId,
                        Name = result.TaxName,
                        Cost = result.TaxCost,
                        MeasureUnit = result.MeasureUnit,
                        IsCalculated = false
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления налога к заказу");
                return StatusCode(500, new { success = false, message = "Ошибка при добавлении налога" });
            }
        }

        [HttpPost("RemoveTaxFromOrder")]
        public async Task<IActionResult> RemoveTaxFromOrder([FromBody] RemoveTaxFromOrderRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _taxManagementService.RemoveTaxFromOrderAsync(request.OrderId, request.TaxTypeId, ct);

                if (!result.Success)
                    return NotFound(new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления налога из заказа");
                return StatusCode(500, new { success = false, message = "Ошибка при удалении налога" });
            }
        }

        // ============================================================
        // Status
        // ============================================================

        [HttpPost("UpdateOrderStatus")]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
        {
            try
            {
                var currentUser = User?.Identity?.Name ?? "System";
                var result = await _statusService.UpdateStatusAsync(request.OrderId, request.StatusId, currentUser, ct);

                if (!result.Success)
                    return NotFound(new { success = false, message = result.Message });

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    oldStatus = result.OldStatus,
                    newStatus = result.NewStatus,
                    changeDate = result.ChangeDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    changedBy = result.ChangedBy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса заказа");
                return StatusCode(500, new { success = false, message = "Ошибка обновления статуса" });
            }
        }

        [HttpGet("GetOrderStatusHistory/{orderId}")]
        public async Task<IActionResult> GetOrderStatusHistory(int orderId, CancellationToken ct)
        {
            try
            {
                var history = await _statusService.GetHistoryAsync(orderId, ct);
                return Ok(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения истории статусов заказа {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Ошибка получения истории" });
            }
        }

        // ============================================================
        // Session
        // ============================================================

        [RequestSizeLimit(10 * 1024 * 1024)]
        [HttpPost("UpdateAllProductsInSession")]
        public IActionResult UpdateAllProductsInSession([FromBody] List<OrderProductViewModel> products)
        {
            try
            {
                if (products == null || !products.Any())
                    return BadRequest(new { Message = "Список товаров пуст" });

                HttpContext.Session.SetString("UploadedProducts", JsonSerializer.Serialize(products));

                return Ok(new { Success = true, Count = products.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления сессии товаров");
                return StatusCode(500, new { Success = false, Message = "Ошибка при сохранении товаров" });
            }
        }

        // ============================================================
        // Quick create
        // ============================================================

        [HttpPost("CreateCompany")]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _quickCreateService.CreateCompanyAsync(request.Name, ct);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, id = result.Id, name = result.Name, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка быстрого создания компании");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка" });
            }
        }

        [HttpPost("CreateCustomer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _quickCreateService.CreateCustomerAsync(
                    request.CompanyId, request.FirstName, request.LastName, ct);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, id = result.Id, fullName = result.FullName, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка быстрого создания клиента");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка" });
            }
        }

        [HttpPost("CreateManufacturer")]
        public async Task<IActionResult> CreateManufacturer([FromBody] CreateManufacturerRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _quickCreateService.CreateManufacturerAsync(request.Name, ct);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                return Ok(new { success = true, id = result.Id, name = result.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка быстрого создания производителя");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка" });
            }
        }

        // ============================================================
        // Request DTOs (только для тел запросов, у которых нет отдельной DTO)
        // ============================================================

        public class AddTaxToOrderRequest
        {
            public int OrderId { get; set; }
            public int TaxTypeId { get; set; }
        }

        public class RemoveTaxFromOrderRequest
        {
            public int OrderId { get; set; }
            public int TaxTypeId { get; set; }
        }

        public class UpdateOrderStatusRequest
        {
            public int OrderId { get; set; }
            public int StatusId { get; set; }
        }
    }
}
