namespace Application.DataAccessLayer.Interface.OrderService
{
    public interface IOrderNumberGenerator
    {
        /// <summary>
        /// Генерирует новый номер заказа
        /// </summary>
        Task<string> GenerateOrderNumber();

        /// <summary>
        /// Генерирует номер заказа с возможностью извлечения из имени файла
        /// </summary>
        Task<string> GenerateOrderNumber(string? fileName);

        /// <summary>
        /// Генерация номера дубля
        /// </summary>
        /// <param name="originalOrderNumber"></param>
        /// <returns></returns>
        Task<string> GenerateDuplicateOrderNumber(string originalOrderNumber);

        /// <summary>
        /// Пытается извлечь номер заказа из имени файла
        /// </summary>
        bool TryExtractFromFileName(string fileName, out string orderNumber);
    }
}