using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.ConsolidationModel;
using Microsoft.EntityFrameworkCore;
using DB.Entity.Enum;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class ConsolidationService : IConsolidationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly string[] PoolColors = new[]
        {
            "#FFB3BA", "#FFDFBA", "#FFFFBA", "#BAFFC9", "#BAE1FF", "#E8BAFF", "#FFC8DD", "#B0E0E6",
            "#D4F0C0", "#FFD9E8", "#C9E4FF", "#E6CCFF", "#FDEBD0", "#D5F5E3"
        };

        public ConsolidationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // =====================================================================
        //  Чтение доски
        // =====================================================================

        public async Task<ConsolidationBoardDto> GetBoardAsync(int weekSpan, decimal? weightLimit)
        {
            var allOrders = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => !o.IsDeleted)
                .Include(o => o.Customer.Company)
                .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                .Include(o => o.ConsolidationPool)
                .AsSplitQuery()
                .ToListAsync();

            var board = new ConsolidationBoardDto { WeekSpan = weekSpan };

            var freeOrders = allOrders
                .Where(o => !o.ConsolidationPoolId.HasValue && o.Status == Status.Paid)
                .ToList();

            var pooledOrders = allOrders
                .Where(o => o.ConsolidationPoolId.HasValue)
                .GroupBy(o => o.ConsolidationPoolId.Value)
                .ToList();

            foreach (var order in freeOrders)
            {
                var leadTime = GetOrderLeadTime(order);
                var card = CreateCard(order, leadTime, false);
                AddToColumn(board, card, leadTime);
            }

            foreach (var group in pooledOrders)
            {
                var pool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(group.Key);
                if (pool == null || pool.IsDeleted) continue;
                if (pool.Status != Status.Paid) continue;

                var poolDto = new ConsolidationPoolDto
                {
                    PoolId = pool.Id ?? 0,
                    TargetWeek = pool.TargetWeek,
                    TotalWeight = group.Sum(o => o.TotalWeight),
                    Color = pool.Color,
                    Status = pool.Status,
                    ExpectedDeliveryDate = pool.ExpectedDeliveryDate,
                    LockedByUserId = pool.LockedByUserId,
                    LockedAt = pool.LockedAt
                };

                foreach (var order in group)
                {
                    var leadTime = GetOrderLeadTime(order);
                    var card = CreateCard(order, leadTime, true);
                    card.IsInactive = order.Status != Status.Paid;
                    poolDto.Orders.Add(card);
                }

                board.Pools.Add(poolDto);
            }

            board.Columns = board.Columns.OrderBy(c => c.Week).ToList();
            board.Pools = board.Pools.OrderBy(p => p.TargetWeek).ToList();
            board.Suggestions = CalculateSuggestions(board.Columns, board.Pools, weekSpan, weightLimit);

            return board;
        }

        public async Task<ConsolidationBoardDto> GetTrackingBoardAsync()
        {
            var allOrders = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => !o.IsDeleted && o.ConsolidationPoolId.HasValue)
                .Include(o => o.Customer.Company)
                .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                .Include(o => o.ConsolidationPool)
                .AsSplitQuery()
                .ToListAsync();

            var board = new ConsolidationBoardDto();

            var pools = await _unitOfWork.GetRepository<ConsolidationPool>()
                .GetQueryable()
                .Where(p => !p.IsDeleted && (p.Status == Status.OnTheWay || p.Status == Status.Shipped))
                .ToListAsync();

            foreach (var pool in pools)
            {
                var group = allOrders.Where(o => o.ConsolidationPoolId == pool.Id).ToList();
                var poolDto = new ConsolidationPoolDto
                {
                    PoolId = pool.Id ?? 0,
                    TargetWeek = pool.TargetWeek,
                    TotalWeight = group.Sum(o => o.TotalWeight),
                    Color = pool.Color,
                    Status = pool.Status,
                    ExpectedDeliveryDate = pool.ExpectedDeliveryDate,
                    LockedByUserId = pool.LockedByUserId,
                    LockedAt = pool.LockedAt
                };

                foreach (var order in group)
                {
                    var leadTime = GetOrderLeadTime(order);
                    var card = CreateCard(order, leadTime, true);
                    card.IsInactive = order.Status != pool.Status;
                    poolDto.Orders.Add(card);
                }

                board.Pools.Add(poolDto);
            }

            board.Pools = board.Pools
                .OrderBy(p => p.ExpectedDeliveryDate ?? DateTime.MaxValue)
                .ThenBy(p => p.TargetWeek)
                .ToList();

            return board;
        }

        public async Task<List<ConsolidationPool>> GetPoolsForDeliveryNotificationAsync(int daysBefore)
        {
            var threshold = DateTime.Now.Date.AddDays(daysBefore);
            return await _unitOfWork.GetRepository<ConsolidationPool>()
                .GetQueryable()
                .Include(p => p.Orders)
                    .ThenInclude(o => o.Customer)
                        .ThenInclude(c => c.Company)
                .Where(p => !p.IsDeleted && p.Status == Status.OnTheWay &&
                            p.ExpectedDeliveryDate.HasValue &&
                            p.ExpectedDeliveryDate.Value.Date <= threshold &&
                            p.ExpectedDeliveryDate.Value.Date >= DateTime.Now.Date)
                .ToListAsync();
        }

        // =====================================================================
        //  Лимиты веса
        // =====================================================================

        public async Task<List<ConsolidationWeightLimitDto>> GetWeightLimitsAsync()
        {
            return await _unitOfWork.ConsolidationWeightLimits.GetQueryable()
                .Where(l => !l.IsDeleted)
                .OrderBy(l => l.Value)
                .Select(l => new ConsolidationWeightLimitDto
                {
                    Id = l.Id ?? 0,
                    Value = l.Value,
                    IsSystem = l.IsSystem
                })
                .ToListAsync();
        }

        public async Task<ConsolidationWeightLimitDto> AddWeightLimitAsync(decimal value, string changedBy)
        {
            var existing = await _unitOfWork.ConsolidationWeightLimits.GetQueryable()
                .FirstOrDefaultAsync(l => !l.IsDeleted && l.Value == value);
            if (existing != null)
                throw new InvalidOperationException("Такой лимит уже существует");

            var entity = new ConsolidationWeightLimit
            {
                Value = value,
                IsSystem = false,
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ConsolidationWeightLimits.Create(entity);
            await _unitOfWork.SaveChangesAsync();

            return new ConsolidationWeightLimitDto
            {
                Id = entity.Id ?? 0,
                Value = entity.Value,
                IsSystem = entity.IsSystem
            };
        }

        // =====================================================================
        //  История изменений пула
        // =====================================================================

        private async Task LogPoolEventAsync(int poolId, ConsolidationPoolEventType eventType, string description, string changedBy, string? oldValue = null, string? newValue = null)
        {
            await _unitOfWork.ConsolidationPoolHistories.Create(new ConsolidationPoolHistory
            {
                PoolId = poolId,
                EventType = eventType,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "System" : changedBy,
                ChangedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        public async Task<List<ConsolidationPoolHistoryDto>> GetPoolHistoryAsync(int poolId)
        {
            return await _unitOfWork.ConsolidationPoolHistories.GetQueryable()
                .Where(h => h.PoolId == poolId && !h.IsDeleted)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new ConsolidationPoolHistoryDto
                {
                    Id = h.Id ?? 0,
                    EventType = h.EventType.ToString(),
                    Description = h.Description,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt
                })
                .ToListAsync();
        }

        // =====================================================================
        //  Создание / изменение пула
        // =====================================================================

        public async Task AcceptSuggestionAsync(List<int> orderIds, int? poolId, string changedBy)
        {
            if (orderIds == null || orderIds.Count == 0)
                throw new ArgumentException("Нет заказов для объединения");

            if (poolId.HasValue)
            {
                var pool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(poolId.Value);
                if (pool == null || pool.IsDeleted) throw new InvalidOperationException("Пул не найден");

                var orders = await _unitOfWork.GetRepository<Order>()
                    .GetQueryable()
                    .Where(o => orderIds.Contains(o.Id.Value) && !o.IsDeleted && o.Status == Status.Paid && o.ConsolidationPoolId == null)
                    .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                    .ToListAsync();

                if (orders.Count != orderIds.Count)
                    throw new InvalidOperationException("Некоторые заказы уже в пуле или недоступны");

                int newMaxLeadTime = Math.Max(pool.TargetWeek, orders.Max(o => GetOrderLeadTime(o)));
                pool.TargetWeek = newMaxLeadTime;
                pool.TotalWeight += orders.Sum(o => o.TotalWeight);
                _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);

                foreach (var order in orders)
                {
                    order.ConsolidationPoolId = pool.Id;
                    _unitOfWork.GetRepository<Order>().Update(order);

                    await LogPoolEventAsync(
                        pool.Id!.Value,
                        ConsolidationPoolEventType.OrderAdded,
                        $"Добавлен заказ №{order.OrderNumber} ({order.TotalWeight:N2} кг)",
                        changedBy);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var orders = await _unitOfWork.GetRepository<Order>()
                    .GetQueryable()
                    .Where(o => orderIds.Contains(o.Id.Value) && !o.IsDeleted && o.Status == Status.Paid && o.ConsolidationPoolId == null)
                    .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                    .ToListAsync();

                if (orders.Count != orderIds.Count)
                    throw new InvalidOperationException("Некоторые заказы уже в пуле или недоступны");

                int maxLeadTime = orders.Max(o => GetOrderLeadTime(o));
                decimal totalWeight = orders.Sum(o => o.TotalWeight);
                string color = await GetNextPoolColorAsync();

                var pool = new ConsolidationPool
                {
                    TargetWeek = maxLeadTime,
                    TotalWeight = totalWeight,
                    IsDeleted = false,
                    Color = color
                };

                await _unitOfWork.GetRepository<ConsolidationPool>().Create(pool);
                await _unitOfWork.SaveChangesAsync();

                foreach (var order in orders)
                {
                    order.ConsolidationPoolId = pool.Id;
                    _unitOfWork.GetRepository<Order>().Update(order);
                }

                await LogPoolEventAsync(
                    pool.Id!.Value,
                    ConsolidationPoolEventType.Created,
                    $"Создана отгрузка из {orders.Count} заказ(ов), общий вес {totalWeight:N2} кг",
                    changedBy);

                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task DissolvePoolAsync(int poolId, string changedBy)
        {
            var pool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(poolId);
            if (pool == null || pool.IsDeleted) return;

            var orders = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => o.ConsolidationPoolId == poolId && !o.IsDeleted)
                .ToListAsync();

            // Логируем ДО soft-delete, чтобы запись точно попала в БД
            await LogPoolEventAsync(
                poolId,
                ConsolidationPoolEventType.Dissolved,
                $"Отгрузка разобрана, {orders.Count} заказ(ов) возвращены в свободные",
                changedBy);

            foreach (var order in orders)
            {
                order.ConsolidationPoolId = null;
                _unitOfWork.GetRepository<Order>().Update(order);
            }

            pool.IsDeleted = true;
            _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveOrderFromPoolAsync(int orderId, string changedBy)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var order = await _unitOfWork.GetRepository<Order>()
                    .GetQueryable()
                    .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null || order.ConsolidationPoolId == null)
                    return;

                var poolId = order.ConsolidationPoolId.Value;
                var pool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(poolId);
                if (pool == null || pool.IsDeleted) return;

                var orderNumber = order.OrderNumber;
                var orderWeight = order.TotalWeight;

                order.ConsolidationPoolId = null;
                _unitOfWork.GetRepository<Order>().Update(order);

                var remainingOrders = await _unitOfWork.GetRepository<Order>()
                    .GetQueryable()
                    .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                    .Where(o => o.ConsolidationPoolId == poolId && !o.IsDeleted)
                    .ToListAsync();

                if (remainingOrders.Any())
                {
                    pool.TargetWeek = remainingOrders.Max(o => GetOrderLeadTime(o));
                    pool.TotalWeight = remainingOrders.Sum(o => o.TotalWeight);
                    _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);

                    await LogPoolEventAsync(
                        poolId,
                        ConsolidationPoolEventType.OrderRemoved,
                        $"Удалён заказ №{orderNumber} ({orderWeight:N2} кг)",
                        changedBy);
                }
                else
                {
                    await LogPoolEventAsync(
                        poolId,
                        ConsolidationPoolEventType.Dissolved,
                        $"Последний заказ №{orderNumber} удалён, отгрузка распущена",
                        changedBy);

                    pool.IsDeleted = true;
                    _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);
                }

                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task AddOrderToPoolAsync(int orderId, int poolId, decimal? weightLimit, string changedBy)
        {
            var order = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted && o.ConsolidationPoolId == null);

            if (order == null)
                throw new InvalidOperationException("Заказ не найден или уже в отгрузке");

            var pool = await _unitOfWork.GetRepository<ConsolidationPool>()
                .GetById(poolId);
            if (pool == null || pool.IsDeleted)
                throw new InvalidOperationException("Отгрузка не найдена");

            if (weightLimit.HasValue)
            {
                decimal newTotal = pool.TotalWeight + order.TotalWeight;
                if (newTotal > weightLimit.Value)
                    throw new InvalidOperationException(
                        $"Превышен лимит веса: {newTotal:N1} кг из {weightLimit.Value:N1} кг");
            }

            int orderLeadTime = GetOrderLeadTime(order);
            if (orderLeadTime > pool.TargetWeek)
                pool.TargetWeek = orderLeadTime;

            pool.TotalWeight += order.TotalWeight;
            _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);

            order.ConsolidationPoolId = pool.Id;
            _unitOfWork.GetRepository<Order>().Update(order);

            await LogPoolEventAsync(
                pool.Id!.Value,
                ConsolidationPoolEventType.OrderAdded,
                $"Добавлен заказ №{order.OrderNumber} ({order.TotalWeight:N2} кг)",
                changedBy);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MoveOrderToPoolAsync(int orderId, int targetPoolId, decimal? weightLimit, string changedBy)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var order = await _unitOfWork.GetRepository<Order>()
                    .GetQueryable()
                    .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                    throw new InvalidOperationException("Заказ не найден");

                var targetPool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(targetPoolId);
                if (targetPool == null || targetPool.IsDeleted)
                    throw new InvalidOperationException("Целевая отгрузка не найдена");

                if (weightLimit.HasValue && order.ConsolidationPoolId != targetPoolId)
                {
                    decimal newTotal = targetPool.TotalWeight + order.TotalWeight;
                    if (newTotal > weightLimit.Value)
                        throw new InvalidOperationException(
                            $"Превышен лимит веса: {newTotal:N1} кг из {weightLimit.Value:N1} кг");
                }

                var orderNumber = order.OrderNumber;
                var orderWeight = order.TotalWeight;

                if (order.ConsolidationPoolId.HasValue)
                {
                    var oldPoolId = order.ConsolidationPoolId.Value;
                    if (oldPoolId == targetPoolId)
                        return;

                    var oldPool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(oldPoolId);
                    if (oldPool != null && !oldPool.IsDeleted)
                    {
                        order.ConsolidationPoolId = null;
                        _unitOfWork.GetRepository<Order>().Update(order);

                        var remainingOrders = await _unitOfWork.GetRepository<Order>()
                            .GetQueryable()
                            .Include(o => o.OrdersProducts.Where(op => !op.IsDeleted))
                            .Where(o => o.ConsolidationPoolId == oldPoolId && !o.IsDeleted)
                            .ToListAsync();

                        if (remainingOrders.Any())
                        {
                            oldPool.TargetWeek = remainingOrders.Max(o => GetOrderLeadTime(o));
                            oldPool.TotalWeight = remainingOrders.Sum(o => o.TotalWeight);
                            _unitOfWork.GetRepository<ConsolidationPool>().Update(oldPool);
                        }
                        else
                        {
                            oldPool.IsDeleted = true;
                            _unitOfWork.GetRepository<ConsolidationPool>().Update(oldPool);
                        }

                        await LogPoolEventAsync(
                            oldPoolId,
                            ConsolidationPoolEventType.OrderMovedOut,
                            $"Заказ №{orderNumber} ({orderWeight:N2} кг) перемещён в отгрузку №{targetPoolId}",
                            changedBy);
                    }
                }

                int orderLeadTime = GetOrderLeadTime(order);
                if (orderLeadTime > targetPool.TargetWeek)
                    targetPool.TargetWeek = orderLeadTime;
                targetPool.TotalWeight += order.TotalWeight;
                _unitOfWork.GetRepository<ConsolidationPool>().Update(targetPool);

                order.ConsolidationPoolId = targetPool.Id;
                _unitOfWork.GetRepository<Order>().Update(order);

                await LogPoolEventAsync(
                    targetPoolId,
                    ConsolidationPoolEventType.OrderMovedIn,
                    $"Заказ №{orderNumber} ({orderWeight:N2} кг) перемещён из другой отгрузки",
                    changedBy);

                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task SetPoolStatusAsync(int poolId, Status newStatus, DateTime? expectedDeliveryDate, string changedBy)
        {
            var pool = await _unitOfWork.GetRepository<ConsolidationPool>().GetById(poolId);
            if (pool == null || pool.IsDeleted)
                throw new InvalidOperationException("Отгрузка не найдена");

            var oldPoolStatus = pool.Status;
            var oldExpectedDate = pool.ExpectedDeliveryDate;

            var orders = await _unitOfWork.GetRepository<Order>()
                .GetQueryable()
                .Where(o => o.ConsolidationPoolId == poolId && !o.IsDeleted)
                .ToListAsync();

            foreach (var order in orders)
            {
                var oldStatus = order.Status;
                if (oldStatus == newStatus) continue;

                order.Status = newStatus;
                _unitOfWork.GetRepository<Order>().Update(order);

                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangeDate = DateTime.Now,
                    ChangedBy = changedBy
                };
                await _unitOfWork.GetRepository<OrderStatusHistory>().Create(history);
            }

            pool.Status = newStatus;
            pool.ExpectedDeliveryDate = expectedDeliveryDate;
            _unitOfWork.GetRepository<ConsolidationPool>().Update(pool);

            // Логируем изменение статуса, если он реально поменялся
            if (oldPoolStatus != newStatus)
            {
                await LogPoolEventAsync(
                    poolId,
                    ConsolidationPoolEventType.StatusChanged,
                    $"Статус изменён: {oldPoolStatus.GetDisplayName()} → {newStatus.GetDisplayName()}",
                    changedBy,
                    oldValue: oldPoolStatus.GetDisplayName(),
                    newValue: newStatus.GetDisplayName());
            }

            // Логируем изменение ожидаемой даты доставки, если она поменялась
            if (oldExpectedDate != expectedDeliveryDate)
            {
                var oldDateStr = oldExpectedDate?.ToString("dd.MM.yyyy") ?? "не задана";
                var newDateStr = expectedDeliveryDate?.ToString("dd.MM.yyyy") ?? "не задана";

                await LogPoolEventAsync(
                    poolId,
                    ConsolidationPoolEventType.ExpectedDeliveryDateChanged,
                    $"Ожидаемая дата доставки изменена: {oldDateStr} → {newDateStr}",
                    changedBy,
                    oldValue: oldDateStr,
                    newValue: newDateStr);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // =====================================================================
        //  Приватные хелперы
        // =====================================================================

        private int GetOrderLeadTime(Order order)
        {
            return order.OrdersProducts?
                .Where(op => !op.IsDeleted)
                .Select(op => op.LeadTime)
                .DefaultIfEmpty(0)
                .Max() ?? 0;
        }

        private ConsolidationOrderCardDto CreateCard(Order order, int leadTime, bool isInPool)
        {
            return new ConsolidationOrderCardDto
            {
                OrderId = order.Id ?? 0,
                OrderNumber = order.OrderNumber,
                CompanyName = order.Customer?.Company?.Name ?? "Не указана",
                TotalCost = order.TotalCost,
                TotalWeight = order.TotalWeight,
                LeadTimeWeeks = leadTime,
                IsInPool = isInPool,
                IsInactive = order.Status != Status.Paid,
                StatusDisplay = order.Status.GetDisplayName(),
                LockedByUserId = order.LockedByUserId,
                LockedAt = order.LockedAt
            };
        }

        private void AddToColumn(ConsolidationBoardDto board, ConsolidationOrderCardDto card, int week)
        {
            var column = board.Columns.FirstOrDefault(c => c.Week == week);
            if (column == null)
            {
                column = new ConsolidationColumnDto { Week = week };
                board.Columns.Add(column);
            }
            column.Orders.Add(card);
        }

        private async Task<string> GetNextPoolColorAsync()
        {
            var usedColors = await _unitOfWork.GetRepository<ConsolidationPool>()
                .GetQueryable()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Color))
                .Select(p => p.Color)
                .ToListAsync();

            foreach (var color in PoolColors)
            {
                if (!usedColors.Contains(color))
                    return color;
            }

            return PoolColors[usedColors.Count % PoolColors.Length];
        }

        private List<ConsolidationSuggestionDto> CalculateSuggestions(List<ConsolidationColumnDto> columns, List<ConsolidationPoolDto> pools, int weekSpan, decimal? weightLimit)
        {
            var suggestions = new List<ConsolidationSuggestionDto>();

            // 1. Подсказки из свободных заказов
            var weeks = columns.Select(c => c.Week).Distinct().OrderBy(w => w).ToList();
            for (int i = 0; i < weeks.Count; i++)
            {
                int maxWeek = weeks[i];
                int minWeek = maxWeek - weekSpan;

                var includedOrders = columns
                    .Where(c => c.Week >= minWeek && c.Week <= maxWeek)
                    .SelectMany(c => c.Orders)
                    .Where(o => !o.IsInPool)
                    .ToList();

                if (includedOrders.Count < 2) continue;

                if (weightLimit.HasValue)
                {
                    includedOrders = SelectByWeightLimit(includedOrders, weightLimit.Value);
                    if (includedOrders.Count < 2) continue;
                }

                bool isSubset = suggestions.Any(existing =>
                    existing.PoolId == null &&
                    includedOrders.All(o => existing.OrderIds.Contains(o.OrderId)) &&
                    existing.OrderIds.Count > includedOrders.Count);

                if (!isSubset)
                {
                    suggestions.Add(new ConsolidationSuggestionDto
                    {
                        FromWeek = minWeek,
                        ToWeek = maxWeek,
                        OrderIds = includedOrders.Select(o => o.OrderId).ToList(),
                        TotalWeight = includedOrders.Sum(o => o.TotalWeight)
                    });
                }
            }

            // 2. Подсказки с существующими пулами
            foreach (var pool in pools)
            {
                if (weightLimit.HasValue && pool.TotalWeight >= weightLimit.Value)
                    continue;

                decimal remaining = weightLimit.HasValue
                    ? weightLimit.Value - pool.TotalWeight
                    : decimal.MaxValue;

                var candidatesByWeek = columns
                    .Where(c => Math.Abs(c.Week - pool.TargetWeek) <= weekSpan)
                    .SelectMany(c => c.Orders.Select(o => new { Order = o, Week = c.Week }))
                    .GroupBy(x => x.Week);

                foreach (var group in candidatesByWeek)
                {
                    var groupCards = group.Select(x => x.Order).ToList();

                    if (weightLimit.HasValue)
                    {
                        groupCards = SelectByWeightLimit(groupCards, remaining);
                        if (groupCards.Count == 0) continue;
                    }

                    var orderIds = groupCards.Select(o => o.OrderId).ToList();
                    int maxLeadTime = Math.Max(pool.TargetWeek, group.Key);
                    int minLeadTime = Math.Min(pool.TargetWeek, group.Key);
                    decimal totalWeight = pool.TotalWeight + groupCards.Sum(o => o.TotalWeight);

                    suggestions.Add(new ConsolidationSuggestionDto
                    {
                        PoolId = pool.PoolId,
                        FromWeek = minLeadTime,
                        ToWeek = maxLeadTime,
                        OrderIds = orderIds,
                        TotalWeight = totalWeight
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Жадный отбор: сортируем по убыванию веса, добавляем, если влезает в лимит.
        /// </summary>
        private List<ConsolidationOrderCardDto> SelectByWeightLimit( List<ConsolidationOrderCardDto> orders, decimal limit)
        {
            if (limit <= 0) return new List<ConsolidationOrderCardDto>();

            var result = new List<ConsolidationOrderCardDto>();
            decimal sum = 0;

            foreach (var order in orders.OrderByDescending(o => o.TotalWeight))
            {
                if (sum + order.TotalWeight <= limit)
                {
                    result.Add(order);
                    sum += order.TotalWeight;
                }
            }

            return result;
        }
    }
}