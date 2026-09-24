using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Consolida.Controllers
{
    public class ExcelOrderController : Controller
    {
        private readonly IExcelOrderProcessingService _orderProcessingService;
        private readonly ILogger<ExcelOrderController> _logger;

        public ExcelOrderController(
            IExcelOrderProcessingService orderProcessingService,
            ILogger<ExcelOrderController> logger)
        {
            _orderProcessingService = orderProcessingService;
            _logger = logger;
        }

        [HttpPost, ActionName("UploadOrder")]
        [Authorize(Policy = "CreateOrders")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadOrder(IFormFile file)
        {
            const int TENmb = 10 * 1024 * 1024;

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Попытка загрузки пустого файла");
                TempData["ErrorMessage"] = "Файл не выбран или поврежден.";
                return RedirectToAction("Index", "Orders");
            }
            if (!IsExcelFile(file))
            {
                _logger.LogWarning("Попытка загрузить файл, отличный от excel: {FileName}", file.FileName);
                TempData["ErrorMessage"] = "Пожалуйста, загрузите файл в формате Excel.";
                return RedirectToAction("Index", "Orders");
            }
            if (file.Length > TENmb)
            {
                _logger.LogWarning("Файл слишком большой: {FileName} ({Size} байт)", file.FileName, file.Length);
                TempData["ErrorMessage"] = "Файл слишком большой. Максимальный размер — 10 МБ.";
                return RedirectToAction("Index", "Orders");
            }

            try
            {
                var orderModel = await _orderProcessingService.ProcessExcelOrderAsync(file);

                // Сохраняем товары в сессию для последующего заполнения формы Create
                HttpContext.Session.SetString("UploadedProducts",
                    JsonSerializer.Serialize(orderModel.Products));

                _logger.LogInformation("Успешно обработан Excel-файл с {ProductCount} товарами",
                    orderModel.Products.Count);

                return RedirectToAction("Create", "Orders", new
                {
                    fromExcel = true,
                    excelFileName = file.FileName
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации Excel-файла");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "Неподдерживаемый формат файла");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Ошибка формата данных в Excel: {FileName}", file.FileName);
                TempData["ErrorMessage"] =
                    "Ошибка в данных Excel. Проверьте правильность числовых значений (количество, цена).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке файла");
                TempData["ErrorMessage"] =
                    "Произошла ошибка при обработке файла. Пожалуйста, попробуйте еще раз.";
                return RedirectToAction("Index", "Orders");
            }

            return RedirectToAction("Index", "Orders");
        }

        private bool IsExcelFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var allowedMimeTypes = new[]
            {
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-excel"
            };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mimeType = file.ContentType.ToLowerInvariant();

            return allowedExtensions.Contains(fileExtension) &&
                   allowedMimeTypes.Contains(mimeType);
        }
    }
}
