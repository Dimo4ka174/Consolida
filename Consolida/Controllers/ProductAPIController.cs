using Application.DataAccessLayer.Interface.Entities;
using Application.ViewModels.ProductModel;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private readonly IProductApiService _productApiService;
        private readonly ILogger<ProductAPIController> _logger;

        public ProductAPIController(
            IProductApiService productApiService,
            ILogger<ProductAPIController> logger)
        {
            _productApiService = productApiService;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int limit = 10, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest(new { message = "Запрос должен содержать минимум 2 символа" });

            var products = await _productApiService.SearchAsync(query, limit, ct);
            return Ok(products);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductApiRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, message = "Название обязательно" });

            try
            {
                var product = await _productApiService.CreateProductAsync(request, ct);
                return Ok(new { success = true, product });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product via API");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("GetManufacturers")]
        public async Task<IActionResult> GetManufacturers(CancellationToken ct = default)
        {
            var manufacturers = await _productApiService.GetManufacturersLookupAsync(ct);
            return Ok(manufacturers);
        }

        [HttpPost("CreateManufacturer")]
        public async Task<IActionResult> CreateManufacturer([FromBody] CreateManufacturerApiRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, message = "Название обязательно" });

            try
            {
                var manufacturer = await _productApiService.CreateManufacturerIfNotExistsAsync(request.Name, ct);
                return Ok(new { success = true, manufacturer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manufacturer via API");
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
