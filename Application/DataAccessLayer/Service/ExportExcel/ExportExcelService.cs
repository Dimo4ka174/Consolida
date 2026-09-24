using Application.ViewModels.OrderModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using DB.Entity;
using Application.DataAccessLayer.Interface.Excel;
using Application.DataAccessLayer.Service.Common;

namespace Application.DataAccessLayer.Service.ExportExcel
{
    /// <summary>
    /// Выгрузка заказа в excel формат
    /// </summary>
    public class ExportExcelService : IExportExcelService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ExportExcelService> _logger;
        private readonly RussianNumberToWordsConverter _russianConverter;

        public ExportExcelService(IWebHostEnvironment env, ILogger<ExportExcelService> logger, RussianNumberToWordsConverter russianConverter)
        {
            _env = env;
            _logger = logger;
            _russianConverter = russianConverter;
        }

        public async Task<MemoryStream> GenerateTKPAsync(ExportOrderViewModel model, Order order, TkpSettings? tkpSettings = null)
        {
            await Task.Yield();
            try
            {
                var templatePath = Path.Combine(_env.WebRootPath, "documentTemplates", "TKP_template.xlsx");
                ValidateTemplatePath(templatePath);

                using (var workbook = new XLWorkbook(templatePath))
                {
                    var worksheet = workbook.Worksheet("ТКП");

                    FillDocumentHeader(worksheet, order, tkpSettings);
                    FillProductsTable(worksheet, model);

                    int nextRow = FillTotalsSection(worksheet, model);

                    for (int r = nextRow; r <= nextRow + 3; r++)
                    {
                        worksheet.Row(r).Clear(XLClearOptions.Contents);
                    }

                    worksheet.Cell(nextRow, 1).Value = "Срок поставки: " +
                        (tkpSettings != null && !string.IsNullOrWhiteSpace(tkpSettings.DeliveryTime) ? tkpSettings.DeliveryTime : "недель с момента получения предоплаты") + ";";
                    worksheet.Cell(nextRow + 1, 1).Value = "Условия доставки: " +
                        (tkpSettings != null && !string.IsNullOrWhiteSpace(tkpSettings.DeliveryTerms) ? tkpSettings.DeliveryTerms : "до склада Заказчика (стоимость доставки включена в стоимость товара)") + ";";
                    worksheet.Cell(nextRow + 2, 1).Value = "Условия оплаты: " +
                        (tkpSettings != null && !string.IsNullOrWhiteSpace(tkpSettings.PaymentTerms) ? tkpSettings.PaymentTerms : "100% предоплата") + ";";
                    worksheet.Cell(nextRow + 3, 1).Value = "Срок действия предложения: до " +
                        DateTime.Now.AddMonths(1).ToString("dd.MM.yyyy") + ";";

                    return SaveWorkbookToStream(workbook);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации ТКП");
                throw;
            }
        }

        #region Private Methods - Document Sections

        private void FillDocumentHeader(IXLWorksheet worksheet, Order order, TkpSettings? tkpSettings)
        {
            worksheet.Cell(8, 1).Value = DateTime.Now.ToString("dd.MM.yyyy");

            var year = DateTime.Now.ToString("yy");
            var tkpNumber = tkpSettings?.TkpNumber?.Trim();
            if (string.IsNullOrEmpty(tkpNumber))
                tkpNumber = order.Id?.ToString() ?? "";

            var headerText = $"№05 {tkpNumber}/{year}";

            var headerCell = worksheet.Cell(8, 3);
            headerCell.Value = headerText;
            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerCell.Style.Font.FontName = "Times New Roman";
            headerCell.Style.Font.FontSize = 11;

            worksheet.Cell(9, 1).Value = $"Запрос № {order.OrderNumber}";
            FillCustomerInfo(worksheet, order);
        }

        private void FillCustomerInfo(IXLWorksheet worksheet, Order order)
        {
            var fullName = FormatCustomerFullName(order.Customer);
            string salutation;

            // Проверяем, чтобы ФИО не было "Заинтересованные лица" или пустым
            if (string.IsNullOrEmpty(fullName) || fullName.Equals("Заинтересованные лица", StringComparison.OrdinalIgnoreCase))
            {
                salutation = "Уважаемые дамы и господа!";
            }
            else
            {
                salutation = $"Уважаемый(ая) {fullName}!";
            }

            worksheet.Cell(13, 1).Value = salutation;
            worksheet.Range(13, 1, 13, 10).Merge();

            var greetingCell = worksheet.Cell(13, 1);
            greetingCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            greetingCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            greetingCell.Style.Font.FontName = "Times New Roman";
            greetingCell.Style.Font.FontSize = 11;

            if (order.Customer != null)
            {
                // Компания
                if (order.Customer.Company != null && !string.IsNullOrEmpty(order.Customer.Company.Name))
                {
                    worksheet.Cell(8, 7).Value = order.Customer.Company.Name;
                    var companyRange = worksheet.Range(8, 7, 8, 10);
                    companyRange.Merge();

                    var companyCell = worksheet.Cell(8, 7);
                    companyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    companyCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    companyCell.Style.Font.FontName = "Times New Roman";
                    companyCell.Style.Font.FontSize = 11;
                }

                // Должность
                if (!string.IsNullOrEmpty(order.Customer.PositionJob))
                {
                    worksheet.Cell(9, 7).Value = order.Customer.PositionJob;
                    var positionRange = worksheet.Range(9, 7, 9, 10);
                    positionRange.Merge();

                    var positionCell = worksheet.Cell(9, 7);
                    positionCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    positionCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    positionCell.Style.Font.FontName = "Times New Roman";
                    positionCell.Style.Font.FontSize = 11;
                }

                // ФИО
                if (!string.IsNullOrEmpty(fullName) &&
                    !fullName.Equals("Заинтересованные лица", StringComparison.OrdinalIgnoreCase))
                {
                    worksheet.Cell(10, 7).Value = fullName;
                    var nameRange = worksheet.Range(10, 7, 10, 10);
                    nameRange.Merge();

                    var nameCell = worksheet.Cell(10, 7);
                    nameCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    nameCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    nameCell.Style.Font.FontName = "Times New Roman";
                    nameCell.Style.Font.FontSize = 11;
                }
            }
        }

        private string FormatCustomerFullName(Customer customer)
        {
            var nameParts = new List<string>();

            if (!string.IsNullOrEmpty(customer.LastName))
                nameParts.Add(customer.LastName);

            if (!string.IsNullOrEmpty(customer.FirstName))
                nameParts.Add(customer.FirstName);

            if (!string.IsNullOrEmpty(customer.MiddleName))
                nameParts.Add(customer.MiddleName);

            return string.Join(" ", nameParts);
        }

        private void FillProductsTable(IXLWorksheet worksheet, ExportOrderViewModel model)
        {
            int startRow = 19;

            foreach (var product in model.Products)
            {
                FillProductRow(worksheet, startRow++, product, product == model.Products.Last());
            }

            FormatProductsTable(worksheet, 19, startRow - 1);
        }
        private int FillTotalsSection(IXLWorksheet worksheet, ExportOrderViewModel model)
        {
            int lastDataRow = 19 + model.Products.Count - 1;

            var totalWithoutVat = model.Products.Sum(p => p.CalculatedTotals.AmountWithoutVAT);
            var totalVat = totalWithoutVat * 0.22m;
            var totalWithVat = totalWithoutVat + totalVat;

            worksheet.Cell(lastDataRow + 1, 1).Value =
                $"Итого: {_russianConverter.FormatMoney(totalWithVat)}, " +
                $"в т. ч. НДС 22% - {_russianConverter.FormatMoney(totalVat, false)} РУБ";
            worksheet.Cell(lastDataRow + 1, 1).Style.Alignment.WrapText = true;

            // Заполнение сумм
            worksheet.Cell(lastDataRow + 1, 10).Value = totalWithoutVat;
            worksheet.Cell(lastDataRow + 2, 10).Value = totalVat;
            worksheet.Cell(lastDataRow + 3, 10).Value = totalWithVat;

            // Возвращаем строку для подвала: 9 пустых строк после последней строки товаров
            return lastDataRow + 12;
        }
        #endregion

        #region Private Methods - Helpers

        private void ValidateTemplatePath(string templatePath)
        {
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Шаблон ТКП не найден по пути: {TemplatePath}", templatePath);
                throw new FileNotFoundException("Шаблон ТКП не найден");
            }
        }
        private void FillProductRow(IXLWorksheet worksheet, int row, ProductData product, bool isLast)
        {
            worksheet.Cell(row, 1).Value = row - 18; // Номер позиции

            // Создаём RichText для ячейки с товаром
            var cell = worksheet.Cell(row, 2);
            var richText = cell.CreateRichText();
            richText.ClearText();

            // Наименование
            richText.AddText(product.ProductName);

            // Модель жирным
            if (!string.IsNullOrEmpty(product.Model))
            {
                richText.AddText(Environment.NewLine);
                var modelRun = richText.AddText(product.Model);
                modelRun.Bold = true;
            }

            // Производитель
            if (!string.IsNullOrEmpty(product.ManufacturerName))
            {
                richText.AddText(Environment.NewLine);
                richText.AddText(product.ManufacturerName);
            }

            // Устанавливаем стиль ячейки
            cell.Style.Font.FontName = "Times New Roman";
            cell.Style.Font.FontSize = 11;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Объединение ячеек
            worksheet.Range(row, 2, row, 7).Merge();

            // Остальные ячейки
            worksheet.Cell(row, 8).Value = product.Quantity;
            worksheet.Cell(row, 9).Value = product.CalculatedTotals.UnitPriceWithoutVAT;
            worksheet.Cell(row, 10).Value = product.CalculatedTotals.AmountWithoutVAT;
            worksheet.Row(row).Height = 45;

            FormatProductRow(worksheet, row);

            if (!isLast)
            {
                worksheet.Row(row).InsertRowsBelow(1);
                worksheet.Range(row, 1, row, 10).Unmerge();
            }
        }

        private string GetOptionalText(string value) => string.IsNullOrEmpty(value) ? string.Empty : Environment.NewLine + value;
        
        private void FormatProductRow(IXLWorksheet worksheet, int row)
        {
            var style = worksheet.Cell(row, 1).Style;
            style.Font.FontName = "Times New Roman";
            style.Font.FontSize = 11;
            style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(row, 2).Style.Alignment.WrapText = true;
            worksheet.Cell(row, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00 ₽";
            worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00 ₽";
        }
        private void FormatProductsTable(IXLWorksheet worksheet, int startRow, int endRow)
        {
            var dataRange = worksheet.Range(startRow, 1, endRow, 10);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            //worksheet.Rows(startRow, endRow).AdjustToContents();
        }
        private MemoryStream SaveWorkbookToStream(XLWorkbook workbook)
        {
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        #endregion
    }
}
