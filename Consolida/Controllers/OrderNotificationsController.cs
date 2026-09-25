using Application.DataAccessLayer.Interface.Entities;
using Application.ViewModels.OrderNotificationModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consolida.Controllers
{
    [Authorize(Policy = "ReadOrders")]
    public class OrderNotificationsController : Controller
    {
        private readonly IOrderNotificationService _notificationService;

        public OrderNotificationsController(IOrderNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool showCompleted = false)
        {
            ViewBag.ShowCompleted = showCompleted;
            var list = await _notificationService.GetAllAsync(showCompleted);
            return View(list);
        }

        [HttpPost]
        [Authorize(Policy = "CreateOrders")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderNotificationCreateDto dto)
        {
            if (!ModelState.IsValid || dto.OrderId <= 0)
            {
                TempData["ErrorMessage"] = "Заполните все поля";
                return RedirectToAction(nameof(Index));
            }

            await _notificationService.CreateAsync(dto, User.Identity?.Name ?? "System");
            TempData["SuccessMessage"] = "Напоминание создано";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = "CreateOrders")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var ok = await _notificationService.RestoreAsync(id, User.Identity?.Name ?? "System");
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] =
                ok ? "Напоминание восстановлено" : "Не удалось восстановить напоминание";
            return RedirectToAction(nameof(Index));
        }
    }
}
