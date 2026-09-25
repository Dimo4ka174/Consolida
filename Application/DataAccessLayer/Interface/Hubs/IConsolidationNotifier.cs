namespace Application.DataAccessLayer.Interface.Hubs
{
    public interface IConsolidationNotifier
    {
        Task NotifyBoardChangedAsync(ConsolidationChangeInfo info);
    }

    public class ConsolidationChangeInfo
    {
        public string ChangedBy { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public int? PoolId { get; set; }
        public int? TargetPoolId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}