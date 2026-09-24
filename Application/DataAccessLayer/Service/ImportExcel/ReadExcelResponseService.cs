using Application.ViewModels.OrderModel.Products;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using Application.DataAccessLayer.Interface.Excel;

namespace Application.DataAccessLayer.Service.ImportExcel
{
    /// <summary>
    /// Чтение Excel. Преобразование ячеек в объекты ExcelProduct. Установка даты поставки товара на склад в Китай
    /// </summary>
    public class ReadExcelResponseService : IExcelService
    {
        public List<ExcelProduct> ParseProductsFromExcel(IFormFile file)
        {
            const int ManufacturerWithoutShadow = 4;
            const int ManufacturerShadow = 5;
            const int ModelWithoutShadow = 3;
            const int ModelShadow = 4;
            const int QuantityWithoutShadow = 5;
            const int QuantityShadow = 6;
            const int PriceWithoutShadow = 7;
            const int PriceShadow = 8;
            const int DateTimeWithoutShadow = 9;
            const int DateTimeShadow = 10;
            const int CommentWithoutShadow = 11;
            const int CommentShadow = 12;

            var products = new List<ExcelProduct>();
            string? previousLeadTime = null;

            using var stream = new MemoryStream();
            file.CopyTo(stream);
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            var headerRow = worksheet.Row(1);
            bool hasShadowColumn = headerRow.Cell(2).GetString().Trim() == "Наименование";
            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var nameCell = row.Cell(hasShadowColumn ? 3 : 2);
                if (nameCell.IsEmpty()) continue;

                if (!int.TryParse(
                    row.Cell(hasShadowColumn ? QuantityShadow : QuantityWithoutShadow).GetString(),
                    out int quantity))
                    throw new FormatException($"Некорректное количество в строке {row.RowNumber()}");

                if (!decimal.TryParse(
                    row.Cell(hasShadowColumn ? PriceShadow : PriceWithoutShadow).GetString(),
                    out decimal price))
                    throw new FormatException($"Некорректная цена в строке {row.RowNumber()}");

                string leadTime = row.Cell(hasShadowColumn ? DateTimeShadow : DateTimeWithoutShadow).GetString();
                if (string.IsNullOrWhiteSpace(leadTime))
                {
                    leadTime = previousLeadTime
                        ?? throw new InvalidOperationException("Не найдено время поставки");
                }
                else
                {
                    previousLeadTime = leadTime;
                }
                DateTime deliveryDate = CalculateDeliveryDate(leadTime);
                int leadTimeWeeks = ParseLeadTimeWeeks(leadTime);

                products.Add(new ExcelProduct
                {
                    Name = row.Cell(2).GetString(),
                    Model = row.Cell(hasShadowColumn ? ModelShadow : ModelWithoutShadow).GetString(),
                    Manufacturer = row.Cell(hasShadowColumn ? ManufacturerShadow : ManufacturerWithoutShadow).GetString(),
                    Quantity = quantity,
                    Price = price,
                    DeliveryDate = deliveryDate,
                    LeadTime = leadTimeWeeks,
                    Comment = row.Cell(hasShadowColumn ? CommentShadow : CommentWithoutShadow).GetString()
                });
            }

            return products;
        }
        private DateTime CalculateDeliveryDate(string? leadTime)
        {
            if (string.IsNullOrWhiteSpace(leadTime) || leadTime == "Не найдено время поставки")
                return DateTime.Now;

            var matches = Regex.Matches(leadTime, @"\d+");
            if (matches.Count > 0)
            {
                int maxTime = matches.Cast<Match>()
                    .Select(m => int.Parse(m.Value))
                    .Max();

                bool isWeeks = leadTime.Contains("week", StringComparison.OrdinalIgnoreCase);
                int daysToAdd = isWeeks ? maxTime * 7 : maxTime;

                return DateTime.Now.AddDays(daysToAdd);
            }

            return DateTime.Now;
        }

        /// <summary>
        /// Lead time из Excel: недели как есть; иначе дни, округлённые вверх до недель.
        /// </summary>
        private static int ParseLeadTimeWeeks(string? leadTime)
        {
            if (string.IsNullOrWhiteSpace(leadTime) || leadTime == "Не найдено время поставки")
                return 0;

            var matches = Regex.Matches(leadTime, @"\d+");
            if (matches.Count == 0)
                return 0;

            int maxTime = matches.Cast<Match>()
                .Select(m => int.Parse(m.Value))
                .Max();

            bool isWeeks = leadTime.Contains("week", StringComparison.OrdinalIgnoreCase);
            return isWeeks ? maxTime : (int)Math.Ceiling(maxTime / 7.0);
        }
    }
}