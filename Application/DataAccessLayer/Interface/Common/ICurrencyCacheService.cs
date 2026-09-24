using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DataAccessLayer.Interface.Common
{
    /// <summary>
    /// Сервис получения актуального курса юаня к рублю.
    /// Использует внешнее API ЦБ РФ и кэширует результат.
    /// </summary>
    public interface ICurrencyCacheService
    {
        Task<decimal> GetExchangeRateAsync(CancellationToken cancellationToken = default);
        Task UpdateCacheAsync(CancellationToken cancellationToken = default);
        Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
    }
}
