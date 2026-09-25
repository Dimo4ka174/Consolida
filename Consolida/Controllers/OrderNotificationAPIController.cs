using Application.DataAccessLayer.Interface.Entities;
using Application.ViewModels.OrderNotificationModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ReadOrders")]
    public class OrderNotificationAPIController : ControllerBase
    {
        private readonly IOrderNotificationService _service;

        public OrderNotificationAPIController(IOrderNotificationService service)
        {
            _service = service;
        }

        private string CurrentUser => User?.Identity?.Name ?? "System";

        [HttpGet("GetForOrder/{orderId}")]
        public async Task<IActionResult> GetForOrder(int orderId)
        {
            var list = await _service.GetForOrderAsync(orderId);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("GetUrgent")]
        public async Task<IActionResult> GetUrgent(int daysAhead = 7)
        {
            var list = await _service.GetUrgentAsync(daysAhead);
            return Ok(new { success = true, data = list });
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(bool includeCompleted = false)
        {
            var list = await _service.GetAllAsync(includeCompleted);
            return Ok(new { success = true, data = list });
        }

        [HttpPost("Create")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Create([FromBody] OrderNotificationCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || dto.OrderId <= 0)
                return BadRequest(new { success = false, message = "Заполните все обязательные поля" });

            var orderExists = await _service.CheckOrderExistsAsync(dto.OrderId);
            if (!orderExists)
                return BadRequest(new { success = false, message = "Заказ не найден" });

            var id = await _service.CreateAsync(dto, CurrentUser);
            return Ok(new { success = true, id, message = "Напоминание создано" });
        }

        [HttpPost("Complete/{id}")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Complete(int id)
        {
            var ok = await _service.CompleteAsync(id, CurrentUser);
            return ok
                ? Ok(new { success = true, message = "Отмечено как выполненное" })
                : NotFound(new { success = false, message = "Напоминание не найдено" });
        }

        [HttpPost("Delete/{id}")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok
                ? Ok(new { success = true, message = "Удалено" })
                : NotFound(new { success = false, message = "Напоминание не найдено" });
        }

        [HttpGet("SearchOrders")]
        public async Task<IActionResult> SearchOrders(string query)
        {
            var list = await _service.SearchOrdersAsync(query);
            return Ok(list);
        }

        [HttpPost("Restore/{id}")]
        [Authorize(Policy = "CreateOrders")]
        public async Task<IActionResult> Restore(int id)
        {
            var ok = await _service.RestoreAsync(id, CurrentUser);
            return ok
                ? Ok(new { success = true, message = "Напоминание восстановлено" })
                : NotFound(new { success = false, message = "Напоминание не найдено" });
        }
    }
}
