using Application.DataAccessLayer.Interface.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.Common
{
    public class LockService : ILockService
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
        private readonly IUnitOfWork _unitOfWork;

        public LockService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LockResult> TryLockOrderAsync(int orderId, string userId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var order = await _unitOfWork.Orders.GetQueryable()
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                    return new LockResult { Success = false, Message = "Заказ не найден" };

                var now = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(order.LockedByUserId)
                    && order.LockedByUserId != userId
                    && order.LockedAt.HasValue
                    && (now - order.LockedAt.Value) < LockTimeout)
                {
                    return new LockResult
                    {
                        Success = false,
                        LockedBy = order.LockedByUserId,
                        LockedAt = order.LockedAt,
                        Message = $"Заказ редактируется пользователем {order.LockedByUserId}"
                    };
                }

                order.LockedByUserId = userId;
                order.LockedAt = now;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                return new LockResult { Success = true };
            });
        }

        public async Task<LockResult> TryLockPoolAsync(int poolId, string userId)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var pool = await _unitOfWork.ConsolidationPools.GetQueryable()
                    .FirstOrDefaultAsync(p => p.Id == poolId && !p.IsDeleted);

                if (pool == null)
                    return new LockResult { Success = false, Message = "Отгрузка не найдена" };

                var now = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(pool.LockedByUserId)
                    && pool.LockedByUserId != userId
                    && pool.LockedAt.HasValue
                    && (now - pool.LockedAt.Value) < LockTimeout)
                {
                    return new LockResult
                    {
                        Success = false,
                        LockedBy = pool.LockedByUserId,
                        LockedAt = pool.LockedAt,
                        Message = $"Отгрузка редактируется пользователем {pool.LockedByUserId}"
                    };
                }

                pool.LockedByUserId = userId;
                pool.LockedAt = now;
                _unitOfWork.ConsolidationPools.Update(pool);
                await _unitOfWork.SaveChangesAsync();

                return new LockResult { Success = true };
            });
        }

        public async Task UnlockOrderAsync(int orderId, string userId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var order = await _unitOfWork.Orders.GetQueryable()
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null) return;

                // Снимаем только свою блокировку
                if (order.LockedByUserId == userId)
                {
                    order.LockedByUserId = null;
                    order.LockedAt = null;
                    _unitOfWork.Orders.Update(order);
                    await _unitOfWork.SaveChangesAsync();
                }
            });
        }

        public async Task UnlockPoolAsync(int poolId, string userId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var pool = await _unitOfWork.ConsolidationPools.GetQueryable()
                    .FirstOrDefaultAsync(p => p.Id == poolId && !p.IsDeleted);

                if (pool == null) return;

                if (pool.LockedByUserId == userId)
                {
                    pool.LockedByUserId = null;
                    pool.LockedAt = null;
                    _unitOfWork.ConsolidationPools.Update(pool);
                    await _unitOfWork.SaveChangesAsync();
                }
            });
        }

        public async Task ReleaseExpiredLocksAsync()
        {
            var threshold = DateTime.UtcNow - LockTimeout;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var expiredOrders = await _unitOfWork.Orders.GetQueryable()
                    .Where(o => o.LockedByUserId != null && o.LockedAt != null && o.LockedAt < threshold)
                    .ToListAsync();

                foreach (var order in expiredOrders)
                {
                    order.LockedByUserId = null;
                    order.LockedAt = null;
                    _unitOfWork.Orders.Update(order);
                }

                var expiredPools = await _unitOfWork.ConsolidationPools.GetQueryable()
                    .Where(p => p.LockedByUserId != null && p.LockedAt != null && p.LockedAt < threshold)
                    .ToListAsync();

                foreach (var pool in expiredPools)
                {
                    pool.LockedByUserId = null;
                    pool.LockedAt = null;
                    _unitOfWork.ConsolidationPools.Update(pool);
                }

                await _unitOfWork.SaveChangesAsync();
            });
        }
    }
}