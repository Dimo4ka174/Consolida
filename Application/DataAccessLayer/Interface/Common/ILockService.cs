namespace Application.DataAccessLayer.Interface.Common
{
    public interface ILockService
    {
        Task<LockResult> TryLockOrderAsync(int orderId, string userId);
        Task<LockResult> TryLockPoolAsync(int poolId, string userId);
        Task UnlockOrderAsync(int orderId, string userId);
        Task UnlockPoolAsync(int poolId, string userId);
        Task ReleaseExpiredLocksAsync();
    }
    public class LockResult
    {
        public bool Success { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? Message { get; set; }
    }
}
