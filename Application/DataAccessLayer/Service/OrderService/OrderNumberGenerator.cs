using Application.DataAccessLayer.Interface.OrderService;
using Application.DataAccessLayer.Interface.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DB.Entity;

namespace Application.DataAccessLayer.Service.OrderService
{
    public partial class OrderNumberGenerator : IOrderNumberGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        [GeneratedRegex(@"^(\d+)(?:-(\d+))?/(\d+)$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        private static partial Regex OrderNumberRegex();

        [GeneratedRegex(@"^(\d+)-(\d+)/\d+$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
        private static partial Regex DuplicateSuffixRegex();

        public OrderNumberGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateOrderNumber()
        {
            return await GenerateOrderNumber(null);
        }

        public async Task<string> GenerateOrderNumber(string? fileName)
        {
            var currentYear = DateTime.Now.ToString("yy");

            // Пытаемся извлечь номер из имени файла, если он передан
            if (!string.IsNullOrEmpty(fileName) && TryExtractFromFileName(fileName, out var extractedNumber))
            {
                // Проверяем формат (должен соответствовать текущему году)
                if (extractedNumber.EndsWith($"/{currentYear}"))
                {
                    // Проверяем, существует ли уже такой номер в базе
                    var exists = await _unitOfWork.GetRepository<Order>()
                        .GetQueryable()
                        .Where(o => !o.IsDeleted)
                        .Where(o => o.OrderNumber == extractedNumber)
                        .AnyAsync();

                    if (!exists)
                        return extractedNumber;

                }
            }

            // Если не удалось извлечь или номер уже существует, генерируем новый
            return await GenerateNewOrderNumber(currentYear);
        }

        public async Task<string> GenerateDuplicateOrderNumber(string originalOrderNumber)
        {
            var match = OrderNumberRegex().Match(originalOrderNumber);

            if (!match.Success)
                return await GenerateOrderNumber();

            var baseNumber = match.Groups[1].Value;
            var year = match.Groups[3].Value;
            var currentYear = DateTime.Now.ToString("yy");

            if (year != currentYear)
                return $"{baseNumber}-1/{currentYear}";

            var prefix = $"{baseNumber}-";
            var suffix = $"/{currentYear}";

            var existingDuplicates = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => !o.IsDeleted
                            && o.OrderNumber.StartsWith(prefix)
                            && o.OrderNumber.EndsWith(suffix))
                .Select(o => o.OrderNumber)
                .ToListAsync();

            var maxDuplicate = 0;

            foreach (var num in existingDuplicates)
            {
                var duplicateMatch = DuplicateSuffixRegex().Match(num);
                if (duplicateMatch.Success
                    && int.TryParse(duplicateMatch.Groups[2].Value, out var dupNum))
                {
                    maxDuplicate = Math.Max(maxDuplicate, dupNum);
                }
            }

            var nextDuplicate = maxDuplicate > 0 ? maxDuplicate + 1 : 1;
            return $"{baseNumber}-{nextDuplicate}/{currentYear}";
        }

        public bool TryExtractFromFileName(string fileName, out string orderNumber)
        {
            orderNumber = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            try
            {
                // Удаляем расширение и путь
                var cleanFileName = Path.GetFileNameWithoutExtension(fileName);

                // Получаем текущий год
                var currentYear = DateTime.Now.ToString("yy");

                // Список паттернов для поиска номера (в порядке приоритета)
                var patterns = new[]
                {
                    // Паттерн 1: номер_любойтекст_Request
                    new { Pattern = @"^(?<number>\d+)_.*?Request", Priority = 1 },
                    // Паттерн 2: номер_любойтекст Request (с пробелом)
                    new { Pattern = @"^(?<number>\d+)_.*\s+Request", Priority = 2 },
                    // Паттерн 3: номер Request
                    new { Pattern = @"^(?<number>\d+)\s+Request", Priority = 3 },
                    // Паттерн 4: номер_Request
                    new { Pattern = @"^(?<number>\d+)_Request", Priority = 4 },
                    // Паттерн 5: Просто 1-8 цифр в начале строки
                    new { Pattern = @"^(?<number>\d+)", Priority = 5 }
                };

                foreach (var pattern in patterns.OrderBy(p => p.Priority))
                {
                    var match = Regex.Match(cleanFileName, pattern.Pattern, RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        var number = match.Groups["number"].Value;

                        // Парсим номер и форматируем
                        if (int.TryParse(number, out int parsedNumber))
                        {
                            orderNumber = $"{parsedNumber}/{currentYear}";
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<string> GenerateNewOrderNumber(string currentYear)
        {
            var allOrderNumbers = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => !o.IsDeleted)
                .Where(o => !string.IsNullOrEmpty(o.OrderNumber))
                .Where(o => o.OrderNumber.EndsWith($"/{currentYear}"))
                .Select(o => o.OrderNumber)
                .ToListAsync();

            int maxNumber = 0;

            foreach (var num in allOrderNumbers)
            {
                var parts = num.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int number))
                {
                    if (number > maxNumber) maxNumber = number;
                }
            }

            int nextNumber = maxNumber + 1;
            return $"{nextNumber}/{currentYear}";
        }
    }
}
