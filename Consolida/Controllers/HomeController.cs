using Application.DataAccessLayer.Interface.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Consolida.Models;

namespace Consolida.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOrderNotificationService _notificationService;

        public HomeController(IOrderNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UrgentNotifications = await _notificationService.GetUrgentAsync(7);
            }
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
