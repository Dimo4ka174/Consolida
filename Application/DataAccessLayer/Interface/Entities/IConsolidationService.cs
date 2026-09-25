using Application.ViewModels.ConsolidationModel;
using DB.Entity.Enum;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface IConsolidationService
    {
        Task<ConsolidationBoardDto> GetBoardAsync(int weekSpan, decimal? weightLimit);

        Task<List<ConsolidationWeightLimitDto>> GetWeightLimitsAsync();
        Task<ConsolidationWeightLimitDto> AddWeightLimitAsync(decimal value, string changedBy);

        Task AcceptSuggestionAsync(List<int> orderIds, int? poolId, string changedBy);
        Task DissolvePoolAsync(int poolId, string changedBy);
        Task RemoveOrderFromPoolAsync(int orderId, string changedBy);
        Task AddOrderToPoolAsync(int orderId, int poolId, decimal? weightLimit, string changedBy);
        Task MoveOrderToPoolAsync(int orderId, int targetPoolId, decimal? weightLimit, string changedBy);
        Task SetPoolStatusAsync(int poolId, Status newStatus, DateTime? expectedDeliveryDate, string changedBy);

        Task<List<ConsolidationPoolHistoryDto>> GetPoolHistoryAsync(int poolId);

        Task<ConsolidationBoardDto> GetTrackingBoardAsync();
        Task<List<ConsolidationPool>> GetPoolsForDeliveryNotificationAsync(int daysBefore);
    }
}