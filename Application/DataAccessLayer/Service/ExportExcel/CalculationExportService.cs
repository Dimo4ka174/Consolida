using Application.DataAccessLayer.Interface.CalculationService;
using Application.ViewModels.OrderModel.ExcelDoc;
using ClosedXML.Excel;

namespace Application.DataAccessLayer.Service.ExportExcel
{
    public class CalculationExportService : ICalculationExportService
    {
        public async Task<MemoryStream> ExportCalculationAsync(ExportCalculationViewModel model)
        {
            await Task.Yield();

            using var workbook = new XLWorkbook();
            workbook.Style.Font.FontName = "Calibri";
            workbook.Style.Font.FontSize = 11;

            // 1. Лист с общими данными и итогами (объединенный)
            AddCombinedSummarySheet(workbook, model);

            // 2. Лист с детальным расчетом по товарам
            AddDetailedCalculationSheet(workbook, model);

            // 3. Лист с детализацией доп расходов
            AddDetailedTaxesSheet(workbook, model);

            // Сохранение в поток
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }

        private void AddCombinedSummarySheet(XLWorkbook workbook, ExportCalculationViewModel model)
        {
            var worksheet = workbook.Worksheets.Add("Общий расчет");

            // ================= ОБЩИЕ ДАННЫЕ (A1:B8) =================
            var row = 1;

            // Заголовок заказа
            worksheet.Cell(row, 1).Value = $"Расчет заказа №{model.OrderNumber}";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 16;
            worksheet.Range(row, 1, row, 2).Merge();
            row += 2;

            // Основные параметры
            AddWorksheetRow(worksheet, ref row, 1, "Дата расчета:", model.CalculationDate);
            AddWorksheetRow(worksheet, ref row, 1, "Курс юаня:", model.ExchangeRate, "#,##0.00 ₽");
            AddWorksheetRow(worksheet, ref row, 1, "Комиссия банка:", model.BankCommissionRate / 100m, "0.00%");

            // Комментарий к заказу (если есть)
            if (!string.IsNullOrEmpty(model.Comment))
            {
                row++;
                worksheet.Cell(row, 1).Value = "Комментарий:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;

                var commentCell = worksheet.Cell(row, 2);
                commentCell.Value = model.Comment;
                commentCell.Style.Alignment.WrapText = true;
                commentCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                commentCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Увеличиваем высоту строки для комментария
                worksheet.Row(row).Height = Math.Max(20, model.Comment.Length / 50 * 20);
                row++;
            }

            // ================= СВОДНЫЙ РАСЧЕТ ЗАКАЗА (D1:E12) =================
            int rightCol = 4;
            int rightRow = 1;

            // Заголовок сводного расчета
            worksheet.Cell(rightRow, rightCol).Value = "СВОДНЫЙ РАСЧЕТ ЗАКАЗА";
            worksheet.Cell(rightRow, rightCol).Style.Font.Bold = true;
            worksheet.Cell(rightRow, rightCol).Style.Font.FontSize = 14;
            worksheet.Range(rightRow, rightCol, rightRow, rightCol + 1).Merge();
            rightRow += 2;

            // Основные данные сводного расчета
            var summaryData = GetSummaryData(model);
            foreach (var item in summaryData)
            {
                var isTotalRow = item.Key.Contains("ИТОГО");
                AddSummaryRow(worksheet, ref rightRow, rightCol, item.Key, item.Value, isTotalRow);
            }

            // ================= АНАЛИЗ ПРИБЫЛЬНОСТИ (A8:B13) =================
            int analysisRow = row + 2;

            worksheet.Cell(analysisRow, 1).Value = "АНАЛИЗ ПРИБЫЛЬНОСТИ";
            worksheet.Cell(analysisRow, 1).Style.Font.Bold = true;
            worksheet.Cell(analysisRow, 1).Style.Font.FontSize = 14;
            worksheet.Range(analysisRow, 1, analysisRow, 2).Merge();
            analysisRow++;

            var analysisData = GetProfitabilityAnalysis(model);
            foreach (var item in analysisData)
            {
                AddAnalysisRow(worksheet, ref analysisRow, 1, item.Key, item.Value);
            }

            // Настройка ширины колонок
            worksheet.Columns().AdjustToContents();
            worksheet.Column(5).Width = 15;
        }

        private void AddDetailedCalculationSheet(XLWorkbook workbook, ExportCalculationViewModel model)
        {
            var worksheet = workbook.Worksheets.Add("Детальный расчет");
            var headers = GetDetailedCalculationHeaders();

            // Заголовки
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                if (i == 19 || i == 20)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 0);
                }
                else
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Данные по товарам
            int row = 2;
            foreach (var product in model.Products)
            {
                worksheet.Cell(row, 1).Value = row - 1;
                worksheet.Cell(row, 2).Value = product.ProductName;
                worksheet.Cell(row, 3).Value = product.Model;
                worksheet.Cell(row, 4).Value = product.Quantity;
                worksheet.Cell(row, 5).Value = product.PriceInCNY;
                worksheet.Cell(row, 6).Value = product.PriceInRUB;
                worksheet.Cell(row, 7).Value = product.PriceTotalRUB;
                worksheet.Cell(row, 8).Value = product.BankCommissionPerUnit;
                worksheet.Cell(row, 9).Value = product.BankCommissionTotal;
                worksheet.Cell(row, 10).Value = product.MarginRate / 100m;
                worksheet.Cell(row, 11).Value = product.MarginPerUnit;
                worksheet.Cell(row, 12).Value = product.MarginTotal;
                worksheet.Cell(row, 13).Value = (product.PriceInRUB + product.MarginPerUnit) * product.Quantity;
                worksheet.Cell(row, 14).Value = product.CodeTNVD ?? "";
                worksheet.Cell(row, 15).Value = product.DutyRate / 100m;
                worksheet.Cell(row, 16).Value = product.DutyTotal;
                worksheet.Cell(row, 17).Value = product.UnforeseenExpensesRate / 100m;
                worksheet.Cell(row, 18).Value = product.UnforeseenExpensesTotal;
                worksheet.Cell(row, 19).Value = product.TotalTaxes;
                worksheet.Cell(row, 20).Value = product.PriceWithoutVAT;
                worksheet.Cell(row, 21).Value = product.TotalWithoutVAT;
                worksheet.Cell(row, 22).Value = product.TotalWithVAT - product.TotalWithoutVAT;
                worksheet.Cell(row, 23).Value = product.TotalWithVAT;
                worksheet.Cell(row, 24).Value = product.Weight;
                worksheet.Cell(row, 25).Value = product.WeightTotal;
                worksheet.Cell(row, 26).Value = product.Comment;
                row++;
            }

            // Форматирование
            FormatDetailedCalculationColumns(worksheet);

            // Итоговая строка
            AddTotalRow(worksheet, row, headers.Length);

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);
        }

        private void AddDetailedTaxesSheet(XLWorkbook workbook, ExportCalculationViewModel model)
        {
            var worksheet = workbook.Worksheets.Add("Детализация доп расходов");

            // Получаем список всех дополнительных налогов
            var allProductTaxes = GetUniqueProductTaxes(model.Products);

            if (!allProductTaxes.Any())
            {
                worksheet.Cell(1, 1).Value = "НЕТ ДОПОЛНИТЕЛЬНЫХ РАСХОДОВ";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Columns().AdjustToContents();
                return;
            }

            int taxCount = allProductTaxes.Count;
            int totalColumns = 1 + taxCount;

            // Заголовок
            worksheet.Cell(1, 1).Value = "ДЕТАЛИЗАЦИЯ ДОПОЛНИТЕЛЬНЫХ РАСХОДОВ";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Range(1, 1, 1, totalColumns).Merge();

            // Заголовки таблицы
            CreateTaxesTableHeaders(worksheet, allProductTaxes);

            // Данные по товарам
            int dataStartRow = FillTaxesTableData(worksheet, model.Products, allProductTaxes);

            // Итоговые строки
            AddTaxesSummaryRows(worksheet, dataStartRow, model.Products.Count, allProductTaxes, model.OrderTaxes);

            // Стили
            ApplyTaxesTableStyles(worksheet, model.Products.Count, totalColumns, dataStartRow);

            worksheet.Columns().AdjustToContents();
        }

        #region Вспомогательные методы

        private void AddWorksheetRow(IXLWorksheet worksheet, ref int row, int startCol, string label, string value)
        {
            worksheet.Cell(row, startCol).Value = label;
            worksheet.Cell(row, startCol + 1).Value = value;
            row++;
        }

        private void AddWorksheetRow(IXLWorksheet worksheet, ref int row, int startCol, string label, decimal value, string numberFormat)
        {
            worksheet.Cell(row, startCol).Value = label;
            worksheet.Cell(row, startCol + 1).Value = value;
            worksheet.Cell(row, startCol + 1).Style.NumberFormat.Format = numberFormat;
            row++;
        }

        private void AddWorksheetRow(IXLWorksheet worksheet, ref int row, int startCol, string label, DateTime value, string dateFormat = "dd.MM.yyyy HH:mm")
        {
            worksheet.Cell(row, startCol).Value = label;
            worksheet.Cell(row, startCol + 1).Value = value;
            worksheet.Cell(row, startCol + 1).Style.DateFormat.Format = dateFormat;
            row++;
        }

        private string[] GetDetailedCalculationHeaders()
        {
            return new[]
            {
                "№", "Наименование", "Модель", "Кол-во",
                "Цена (¥)", "Цена (₽)", "Итого (₽)",
                "Ком. банка ед. (₽)", "Ком. банка сум. (₽)",
                "Маржа %", "Маржа ед. (₽)", "Маржа сум. (₽)",
                "Цена с маржей (₽)", "Код ТН ВЭД", "Пошлина %",
                "Пошлина сум. (₽)", "Непредв. расходы %",
                "Непредв. расходы сум. (₽)", "Доп расходы (₽)",
                "Итого без НДС (₽)", "Итого без НДС сумма (₽)",
                "НДС 22% (₽)", "Итого с НДС (₽)",
                "Вес ед. (кг)", "Вес общ. (кг)", "Комментарий"
            };
        }

        private void FormatDetailedCalculationColumns(IXLWorksheet worksheet)
        {
            var numberColumns = new[] { 5, 6, 7, 8, 9, 11, 12, 13, 16, 18, 19, 20, 21, 22, 23, 24, 25 };
            foreach (var col in numberColumns)
                worksheet.Column(col).Style.NumberFormat.Format = "#,##0.00";

            var percentColumns = new[] { 10, 15, 17 };
            foreach (var col in percentColumns)
                worksheet.Column(col).Style.NumberFormat.Format = "0.00%";
        }

        private void AddTotalRow(IXLWorksheet worksheet, int lastRow, int columnCount)
        {
            var totalRow = lastRow;
            worksheet.Cell(totalRow, 1).Value = "ИТОГО:";
            worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
            worksheet.Range(totalRow, 1, totalRow, 4).Merge();

            // Формулы суммирования
            AddSumFormula(worksheet, totalRow, 7, lastRow - 1);  // Итого (₽)
            AddSumFormula(worksheet, totalRow, 9, lastRow - 1);  // Ком. банка сум. (₽)
            AddSumFormula(worksheet, totalRow, 12, lastRow - 1); // Маржа сум. (₽)
            AddSumFormula(worksheet, totalRow, 16, lastRow - 1); // Пошлина сум. (₽)
            AddSumFormula(worksheet, totalRow, 18, lastRow - 1); // Непредв. расходы сум. (₽)
            AddSumFormula(worksheet, totalRow, 19, lastRow - 1); // Доп расходы (₽)
            AddSumFormula(worksheet, totalRow, 21, lastRow - 1);  // Итого без НДС сумма (₽)
            AddSumFormula(worksheet, totalRow, 23, lastRow - 1);  // Итого с НДС (₽)
            AddSumFormula(worksheet, totalRow, 25, lastRow - 1);  // Вес общ. (кг)

            var totalRange = worksheet.Range(totalRow, 1, totalRow, columnCount);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
        }

        private void AddSumFormula(IXLWorksheet worksheet, int totalRow, int column, int lastDataRow)
        {
            if (lastDataRow >= 2)
            {
                var colLetter = GetExcelColumnName(column);
                worksheet.Cell(totalRow, column).FormulaA1 = $"SUM({colLetter}2:{colLetter}{lastDataRow})";
            }
        }

        private List<CalculationTaxViewModel> GetUniqueProductTaxes(List<CalculationProductViewModel> products)
        {
            return products
                .SelectMany(p => p.ProductTaxes)
                .GroupBy(t => t.Name)
                .Select(g => new CalculationTaxViewModel
                {
                    Name = g.Key,
                    Cost = g.Sum(t => t.Cost),
                    MeasureUnit = g.First().MeasureUnit
                })
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToList();
        }

        private void CreateTaxesTableHeaders(IXLWorksheet worksheet, List<CalculationTaxViewModel> taxes)
        {
            int row = 3, col = 1;

            worksheet.Cell(row, col).Value = "Товар";
            worksheet.Cell(row, col).Style.Font.Bold = true;
            worksheet.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;

            foreach (var tax in taxes)
            {
                worksheet.Cell(row, col).Value = tax.Name;
                worksheet.Cell(row, col).Style.Font.Bold = true;
                worksheet.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                col++;
            }
        }

        private int FillTaxesTableData(IXLWorksheet worksheet, List<CalculationProductViewModel> products, List<CalculationTaxViewModel> taxes)
        {
            int row = 4;

            foreach (var product in products)
            {
                int col = 1;
                worksheet.Cell(row, col).Value = $"{product.ProductName} {product.Model}";
                col++;

                foreach (var tax in taxes)
                {
                    var productTax = product.ProductTaxes?.FirstOrDefault(pt =>
                        string.Equals(pt.Name, tax.Name, StringComparison.OrdinalIgnoreCase));

                    worksheet.Cell(row, col).Value = productTax?.Cost ?? 0;
                    worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    col++;
                }
                row++;
            }

            return row;
        }

        private void AddTaxesSummaryRows(IXLWorksheet worksheet, int currentRow, int productCount,
            List<CalculationTaxViewModel> taxes, List<CalculationTaxViewModel> orderTaxes)
        {
            int firstDataRow = 4;
            int lastDataRow = firstDataRow + productCount - 1;

            // Итого по товарам
            AddTaxesSummaryRow(worksheet, ref currentRow, "Итого по товарам:", taxes, firstDataRow, lastDataRow);

            // Итого по заказу
            AddOrderTaxesSummaryRow(worksheet, ref currentRow, "Итого по заказу:", taxes, orderTaxes, firstDataRow, lastDataRow);
        }

        private void AddTaxesSummaryRow(IXLWorksheet worksheet, ref int row, string label,
            List<CalculationTaxViewModel> taxes, int firstRow, int lastRow)
        {
            int col = 1;
            worksheet.Cell(row, col).Value = label;
            worksheet.Cell(row, col).Style.Font.Bold = true;
            col++;

            for (int i = 0; i < taxes.Count; i++)
            {
                if (lastRow >= firstRow)
                {
                    var colLetter = GetExcelColumnName(col);
                    worksheet.Cell(row, col).FormulaA1 = $"SUM({colLetter}{firstRow}:{colLetter}{lastRow})";
                }
                else
                {
                    worksheet.Cell(row, col).Value = 0;
                }

                worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, col).Style.Font.Bold = true;
                col++;
            }
            row++;
        }

        private void AddOrderTaxesSummaryRow(IXLWorksheet worksheet, ref int row, string label,
            List<CalculationTaxViewModel> taxes, List<CalculationTaxViewModel> orderTaxes,
            int firstRow, int lastRow)
        {
            int col = 1;
            worksheet.Cell(row, col).Value = label;
            worksheet.Cell(row, col).Style.Font.Bold = true;
            col++;

            for (int i = 0; i < taxes.Count; i++)
            {
                var taxName = taxes[i].Name;
                var orderTax = orderTaxes?.FirstOrDefault(ot =>
                    string.Equals(ot.Name, taxName, StringComparison.OrdinalIgnoreCase));

                if (orderTax != null)
                {
                    worksheet.Cell(row, col).Value = orderTax.Cost;
                }
                else if (lastRow >= firstRow)
                {
                    var colLetter = GetExcelColumnName(col);
                    worksheet.Cell(row, col).FormulaA1 = $"SUM({colLetter}{firstRow}:{colLetter}{lastRow})";
                }
                else
                {
                    worksheet.Cell(row, col).Value = 0;
                }

                worksheet.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, col).Style.Font.Bold = true;
                col++;
            }
        }

        private void ApplyTaxesTableStyles(IXLWorksheet worksheet, int productCount, int columnCount, int dataStartRow)
        {
            // Заголовки
            var headerRange = worksheet.Range(3, 1, 3, columnCount);
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Чередующаяся заливка для данных
            if (productCount > 0)
            {
                for (int r = 4; r < 4 + productCount; r++)
                {
                    if ((r - 4) % 2 == 0)
                    {
                        worksheet.Range(r, 1, r, columnCount).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                    }
                }
            }

            // Стиль для итоговых строк
            int firstSummaryRow = 4 + productCount;
            for (int i = 0; i < 2; i++)
            {
                var summaryRow = worksheet.Range(firstSummaryRow + i, 1, firstSummaryRow + i, columnCount);
                summaryRow.Style.Font.Bold = true;
                summaryRow.Style.Fill.BackgroundColor = i == 0 ? XLColor.LightBlue : XLColor.LightGreen;
                summaryRow.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }
        }

        private Dictionary<string, decimal> GetSummaryData(ExportCalculationViewModel model)
        {
            return new Dictionary<string, decimal>
            {
                { "Стоимость товаров (¥)", model.Products.Sum(p => p.PriceInCNY * p.Quantity) },
                { "Стоимость товаров (₽)", model.Products.Sum(p => p.PriceTotalRUB) },
                { "Комиссия банка (₽)", model.Products.Sum(p => p.BankCommissionTotal) },
                { "Маржа (₽)", model.Products.Sum(p => p.MarginTotal) },
                { "Пошлины (₽)", model.Products.Sum(p => p.DutyTotal) },
                { "Непредвиденные расходы (₽)", model.Products.Sum(p => p.UnforeseenExpensesTotal) },
                { "Доп расходы и сборы (₽)", model.Products.Sum(p => p.TotalTaxes) },
                { "Итого без НДС (₽)", model.TotalCostWithoutVAT },
                { "НДС (22%) (₽)", model.TotalVAT },
                { "ИТОГО С НДС (₽)", model.TotalCostWithVAT }
            };
        }

        private void AddSummaryRow(IXLWorksheet worksheet, ref int row, int startCol, string label, decimal value, bool isTotal)
        {
            worksheet.Cell(row, startCol).Value = label;
            worksheet.Cell(row, startCol + 1).Value = value;
            worksheet.Cell(row, startCol + 1).Style.NumberFormat.Format = "#,##0.00 ₽";

            if (isTotal)
            {
                worksheet.Range(row, startCol, row, startCol + 1).Style.Font.Bold = true;
                worksheet.Range(row, startCol, row, startCol + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                worksheet.Range(row, startCol, row, startCol + 1).Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }
            row++;
        }

        private Dictionary<string, decimal> GetProfitabilityAnalysis(ExportCalculationViewModel model)
        {
            decimal totalRevenue = model.TotalCostWithoutVAT;
            decimal totalCost = model.TotalCostWithoutVAT - model.TotalProfit;
            decimal profitMargin = totalRevenue > 0 ? (model.TotalProfit / totalRevenue) * 100 : 0;

            return new Dictionary<string, decimal>
            {
                { "Выручка без НДС (₽)", totalRevenue },
                { "Себестоимость (₽)", totalCost },
                { "Прибыль (₽)", model.TotalProfit },
                { "Рентабельность (%)", profitMargin }
            };
        }

        private void AddAnalysisRow(IXLWorksheet worksheet, ref int row, int startCol, string label, decimal value)
        {
            worksheet.Cell(row, startCol).Value = label;

            if (label.Contains("%"))
            {
                worksheet.Cell(row, startCol + 1).Value = value / 100m;
                worksheet.Cell(row, startCol + 1).Style.NumberFormat.Format = "0.00%";
            }
            else
            {
                worksheet.Cell(row, startCol + 1).Value = value;
                worksheet.Cell(row, startCol + 1).Style.NumberFormat.Format = "#,##0.00 ₽";
            }
            row++;
        }

        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        #endregion
    }
}