using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Entity;
using FluentAssertions;
using DB.Entity.Enum;
using MockQueryable;
using DB.Entity;
using Moq;

namespace Consolida.UnitTests.Services;

public class ConsolidationServiceTests
{
    private const int SourcePoolId = 5;
    private const int TargetPoolId = 7;
    private const string OrderNumber = "1001/26";
    private const string SecondOrderNumber = "1002/26";
    private const string ThirdOrderNumber = "1003/26";

    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly Mock<IRepository<ConsolidationPool>> _poolRepoMock = new();
    private readonly Mock<IRepository<ConsolidationPoolHistory>> _poolHistoryRepoMock = new();
    private readonly Mock<IRepository<ConsolidationWeightLimit>> _weightLimitRepoMock = new();
    private readonly ConsolidationService _sut;

    private static Order CreateFreeOrder(int id, string orderNumber, decimal weight, int leadTime, Status status = Status.Paid)
    {
        return new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            TotalWeight = weight,
            Status = status,
            ConsolidationPoolId = null,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
        {
            new() { LeadTime = leadTime, IsDeleted = false }
        }
        };
    }

    public ConsolidationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.GetRepository<Order>()).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ConsolidationPool>()).Returns(_poolRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ConsolidationPoolHistory>()).Returns(_poolHistoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<OrderStatusHistory>())
            .Returns(new Mock<IRepository<OrderStatusHistory>>().Object);

        _unitOfWorkMock.Setup(u => u.ConsolidationWeightLimits).Returns(_weightLimitRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ConsolidationPoolHistories).Returns(_poolHistoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ConsolidationPools).Returns(_poolRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());

        _sut = new ConsolidationService(_unitOfWorkMock.Object);
    }

    // AddWeightLimitAsync

    [Fact]
    public async Task AddWeightLimitAsync_ExistingValue_ThrowsInvalidOperationException()
    {
        var existingLimits = new List<ConsolidationWeightLimit>
        {
            new() { Id = 1, Value = 500m, IsSystem = true, IsDeleted = false }
        };

        _weightLimitRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(existingLimits.BuildMock());

        Func<Task> act = () => _sut.AddWeightLimitAsync(500m, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*уже существует*");

        _weightLimitRepoMock.Verify(r => r.Create(It.IsAny<ConsolidationWeightLimit>()), Times.Never);
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddWeightLimitAsync_NewValue_CreatesEntityAndReturnsDto()
    {
        _weightLimitRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<ConsolidationWeightLimit>().BuildMock());

        ConsolidationWeightLimit? captured = null;

        _weightLimitRepoMock
            .Setup(r => r.Create(It.IsAny<ConsolidationWeightLimit>()))
            .Callback<ConsolidationWeightLimit>(e =>
            {
                captured = e;
                e.Id = 77;
            })
            .Returns(Task.CompletedTask);

        var result = await _sut.AddWeightLimitAsync(750m, "manager");

        result.Should().NotBeNull();
        result.Id.Should().Be(77);
        result.Value.Should().Be(750m);
        result.IsSystem.Should().BeFalse();

        captured.Should().NotBeNull();
        captured!.CreatedBy.Should().Be("manager");
        captured.IsDeleted.Should().BeFalse();

        _weightLimitRepoMock.Verify(r => r.Create(It.IsAny<ConsolidationWeightLimit>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // SetPoolStatusAsync

    [Fact]
    public async Task SetPoolStatusAsync_PoolNotFound_ThrowsInvalidOperationException()
    {
        _poolRepoMock
            .Setup(r => r.GetById(999))
            .ReturnsAsync((ConsolidationPool?)null);

        Func<Task> act = () => _sut.SetPoolStatusAsync(
            999, Status.OnTheWay, null, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не найдена*");
    }

    [Fact]
    public async Task SetPoolStatusAsync_ChangesStatusForPoolAndAllItsOrders()
    {
        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            Status = Status.Paid,
            TargetWeek = 8,
            TotalWeight = 100m,
            Color = "#fff",
            IsDeleted = false
        };

        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = OrderNumber, ConsolidationPoolId = SourcePoolId,
                    Status = Status.Paid, IsDeleted = false },
            new() { Id = 2, OrderNumber = SecondOrderNumber, ConsolidationPoolId = SourcePoolId,
                    Status = Status.Paid, IsDeleted = false },
            new() { Id = 3, OrderNumber = ThirdOrderNumber, ConsolidationPoolId = TargetPoolId,
                    Status = Status.Paid, IsDeleted = false }
        };

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());

        var historyRepoMock = new Mock<IRepository<OrderStatusHistory>>();
        _unitOfWorkMock.Setup(u => u.GetRepository<OrderStatusHistory>())
            .Returns(historyRepoMock.Object);

        await _sut.SetPoolStatusAsync(SourcePoolId, Status.OnTheWay, DateTime.Today.AddDays(30), "admin");

        // Пул обновлён
        pool.Status.Should().Be(Status.OnTheWay);
        pool.ExpectedDeliveryDate.Should().Be(DateTime.Today.AddDays(30));
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);

        // Оба заказа пула обновлены
        orders[0].Status.Should().Be(Status.OnTheWay);
        orders[1].Status.Should().Be(Status.OnTheWay);

        // Заказ из другого пула — НЕ тронут
        orders[2].Status.Should().Be(Status.Paid);

        // Два OrderStatusHistory — с правильным OrderId
        historyRepoMock.Verify(
            r => r.Create(It.Is<OrderStatusHistory>(h =>
                h.OrderId.HasValue &&
                (h.OrderId.Value == 1 || h.OrderId.Value == 2) &&
                h.NewStatus == Status.OnTheWay &&
                h.OldStatus == Status.Paid &&
                h.ChangedBy == "admin")),
            Times.Exactly(2));

        // Обе записи в истории пула
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.StatusChanged)),
            Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.ExpectedDeliveryDateChanged)),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPoolStatusAsync_SameStatusAndDate_NoHistoryRecorded()
    {
        var sameDate = DateTime.Today.AddDays(30);

        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            Status = Status.OnTheWay,
            ExpectedDeliveryDate = sameDate,
            TargetWeek = 8,
            TotalWeight = 100m,
            IsDeleted = false
        };

        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = SourcePoolId,
            Status = Status.OnTheWay,
            IsDeleted = false
        };

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());

        await _sut.SetPoolStatusAsync(SourcePoolId, Status.OnTheWay, sameDate, "admin");

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Never);
    }

    [Fact]
    public async Task SetPoolStatusAsync_ClearingDeliveryDate_LogsDateChangedToNotSet()
    {
        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            Status = Status.OnTheWay,
            ExpectedDeliveryDate = DateTime.Today.AddDays(30),
            TargetWeek = 8,
            TotalWeight = 100m,
            IsDeleted = false
        };

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order>().BuildMock());

        await _sut.SetPoolStatusAsync(SourcePoolId, Status.OnTheWay, expectedDeliveryDate: null, "admin");

        pool.ExpectedDeliveryDate.Should().BeNull();

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.ExpectedDeliveryDateChanged &&
                h.NewValue == "не задана")),
            Times.Once);
    }

    // AddOrderToPoolAsync

    [Fact]
    public async Task AddOrderToPoolAsync_ExceedsWeightLimit_ThrowsInvalidOperationException()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            TotalWeight = 300m,
            ConsolidationPoolId = null,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        var pool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 8,
            TotalWeight = 800m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(pool);

        Func<Task> act = () => _sut.AddOrderToPoolAsync(
            orderId: 1, poolId: TargetPoolId, weightLimit: 1000m, changedBy: "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Превышен лимит веса*");

        order.ConsolidationPoolId.Should().BeNull();
        pool.TotalWeight.Should().Be(800m);
    }

    [Fact]
    public async Task AddOrderToPoolAsync_WithinLimit_AddsOrderAndUpdatesPool()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            TotalWeight = 150m,
            ConsolidationPoolId = null,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 9, IsDeleted = false }
            }
        };

        var pool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 8,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(pool);

        await _sut.AddOrderToPoolAsync(1, TargetPoolId, 1000m, "admin");

        order.ConsolidationPoolId.Should().Be(TargetPoolId);
        pool.TotalWeight.Should().Be(450m);
        pool.TargetWeek.Should().Be(9);

        _orderRepoMock.Verify(r => r.Update(order), Times.Once);
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.OrderAdded &&
                h.PoolId == TargetPoolId)),
            Times.Once);
    }

    [Fact]
    public async Task AddOrderToPoolAsync_NullWeightLimit_SkipsLimitCheck()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            TotalWeight = 999_999m,
            ConsolidationPoolId = null,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 9, IsDeleted = false }
            }
        };

        var pool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 8,
            TotalWeight = 100m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(pool);

        await _sut.AddOrderToPoolAsync(1, TargetPoolId, weightLimit: null, "admin");

        order.ConsolidationPoolId.Should().Be(TargetPoolId);
        pool.TotalWeight.Should().Be(100m + 999_999m);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Once);
    }

    [Fact]
    public async Task AddOrderToPoolAsync_OrderLeadTimeSmallerThanPoolTargetWeek_TargetWeekUnchanged()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            TotalWeight = 100m,
            ConsolidationPoolId = null,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 5, IsDeleted = false }
            }
        };

        var pool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 10,
            TotalWeight = 200m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(pool);

        await _sut.AddOrderToPoolAsync(1, TargetPoolId, weightLimit: 1000m, "admin");

        pool.TargetWeek.Should().Be(10);
        pool.TotalWeight.Should().Be(300m);
    }

    // RemoveOrderFromPoolAsync

    [Fact]
    public async Task RemoveOrderFromPoolAsync_LastOrderInPool_DissolvesPool()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            TotalWeight = 150m,
            ConsolidationPoolId = SourcePoolId,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 150m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);

        await _sut.RemoveOrderFromPoolAsync(1, "admin");

        order.ConsolidationPoolId.Should().BeNull();
        _orderRepoMock.Verify(r => r.Update(order), Times.Once);

        pool.IsDeleted.Should().BeTrue();
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.Dissolved &&
                h.PoolId == SourcePoolId)),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveOrderFromPoolAsync_OrderNotInPool_DoesNothing()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = null,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());

        await _sut.RemoveOrderFromPoolAsync(1, "admin");

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
        _poolRepoMock.Verify(r => r.Update(It.IsAny<ConsolidationPool>()), Times.Never);
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveOrderFromPoolAsync_OtherOrdersRemain_UpdatesPoolInsteadOfDissolving()
    {
        var orderToRemove = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 150m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        var otherOrder = new Order
        {
            Id = 2,
            OrderNumber = SecondOrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 200m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 6, IsDeleted = false }
            }
        };

        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 350m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { orderToRemove, otherOrder }.BuildMock());

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);

        await _sut.RemoveOrderFromPoolAsync(1, "admin");

        orderToRemove.ConsolidationPoolId.Should().BeNull();

        pool.IsDeleted.Should().BeFalse();
        pool.TotalWeight.Should().Be(200m);
        pool.TargetWeek.Should().Be(6);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.OrderRemoved &&
                h.PoolId == SourcePoolId)),
            Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.Dissolved)),
            Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // MoveOrderToPoolAsync

    [Fact]
    public async Task MoveOrderToPoolAsync_BetweenPools_UpdatesBothAndLogsHistory()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 150m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 9, IsDeleted = false }
            }
        };

        var sourcePool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false
        };

        var targetPool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 10,
            TotalWeight = 400m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(sourcePool);
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(targetPool);

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());

        await _sut.MoveOrderToPoolAsync(1, TargetPoolId, weightLimit: null, changedBy: "admin");

        order.ConsolidationPoolId.Should().Be(TargetPoolId);

        targetPool.TotalWeight.Should().Be(550m);
        targetPool.TargetWeek.Should().Be(10);

        sourcePool.IsDeleted.Should().BeTrue();

        _poolRepoMock.Verify(r => r.Update(sourcePool), Times.Once);
        _poolRepoMock.Verify(r => r.Update(targetPool), Times.Once);
        _orderRepoMock.Verify(r => r.Update(order), Times.AtLeastOnce());

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.OrderMovedOut &&
                h.PoolId == SourcePoolId)),
            Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.OrderMovedIn &&
                h.PoolId == TargetPoolId)),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveOrderToPoolAsync_ExceedsTargetLimit_Throws()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = null,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        var targetPool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 8,
            TotalWeight = 800m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(targetPool);
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());

        Func<Task> act = () => _sut.MoveOrderToPoolAsync(1, TargetPoolId, weightLimit: 1000m, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Превышен лимит веса*");

        order.ConsolidationPoolId.Should().BeNull();
        targetPool.TotalWeight.Should().Be(800m);
    }

    [Fact]
    public async Task MoveOrderToPoolAsync_SameTargetPool_EarlyReturnNoChanges()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 150m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>()
        };

        var samePool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(samePool);

        await _sut.MoveOrderToPoolAsync(1, SourcePoolId, weightLimit: null, "admin");

        order.ConsolidationPoolId.Should().Be(SourcePoolId);
        samePool.TotalWeight.Should().Be(300m);
        samePool.IsDeleted.Should().BeFalse();

        _poolRepoMock.Verify(r => r.Update(It.IsAny<ConsolidationPool>()), Times.Never);
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MoveOrderToPoolAsync_SourcePoolHasOtherOrders_SourcePoolUpdatedNotDeleted()
    {
        var orderToMove = new Order
        {
            Id = 1,
            OrderNumber = OrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 150m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 7, IsDeleted = false }
            }
        };

        var remainingOrder = new Order
        {
            Id = 2,
            OrderNumber = SecondOrderNumber,
            ConsolidationPoolId = SourcePoolId,
            TotalWeight = 200m,
            Status = Status.Paid,
            IsDeleted = false,
            OrdersProducts = new List<OrderProduct>
            {
                new() { LeadTime = 6, IsDeleted = false }
            }
        };

        var sourcePool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 350m,
            Status = Status.Paid,
            IsDeleted = false
        };

        var targetPool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 10,
            TotalWeight = 400m,
            Status = Status.Paid,
            IsDeleted = false
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { orderToMove, remainingOrder }.BuildMock());
        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(sourcePool);
        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(targetPool);

        await _sut.MoveOrderToPoolAsync(1, TargetPoolId, weightLimit: null, "admin");

        sourcePool.IsDeleted.Should().BeFalse();
        sourcePool.TotalWeight.Should().Be(200m);
        sourcePool.TargetWeek.Should().Be(6);

        targetPool.TotalWeight.Should().Be(550m);
        targetPool.TargetWeek.Should().Be(10);

        _poolRepoMock.Verify(r => r.Update(sourcePool), Times.Once);
        _poolRepoMock.Verify(r => r.Update(targetPool), Times.Once);
    }

    // DissolvePoolAsync

    [Fact]
    public async Task DissolvePoolAsync_ExistingPool_ReleasesOrdersAndMarksPoolDeleted()
    {
        var pool = new ConsolidationPool
        {
            Id = SourcePoolId,
            TargetWeek = 8,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false
        };

        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = OrderNumber, ConsolidationPoolId = SourcePoolId,
                    TotalWeight = 150m, IsDeleted = false },
            new() { Id = 2, OrderNumber = SecondOrderNumber, ConsolidationPoolId = SourcePoolId,
                    TotalWeight = 150m, IsDeleted = false },
            new() { Id = 3, OrderNumber = ThirdOrderNumber, ConsolidationPoolId = TargetPoolId,
                    TotalWeight = 100m, IsDeleted = false }
        };

        _poolRepoMock.Setup(r => r.GetById(SourcePoolId)).ReturnsAsync(pool);

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());

        await _sut.DissolvePoolAsync(SourcePoolId, "admin");

        orders[0].ConsolidationPoolId.Should().BeNull();
        orders[1].ConsolidationPoolId.Should().BeNull();
        orders[2].ConsolidationPoolId.Should().Be(TargetPoolId);

        pool.IsDeleted.Should().BeTrue();
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.Dissolved &&
                h.PoolId == SourcePoolId &&
                h.ChangedBy == "admin")),
            Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DissolvePoolAsync_NonExistentPool_DoesNothing()
    {
        _poolRepoMock
            .Setup(r => r.GetById(999))
            .ReturnsAsync((ConsolidationPool?)null);

        await _sut.DissolvePoolAsync(999, "admin");

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
        _poolRepoMock.Verify(r => r.Update(It.IsAny<ConsolidationPool>()), Times.Never);
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // AcceptSuggestionAsync

    [Fact]
    public async Task AcceptSuggestionAsync_NullOrderIds_ThrowsArgumentException()
    {
        Func<Task> act = () => _sut.AcceptSuggestionAsync(null!, poolId: null, "admin");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Нет заказов*");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_EmptyOrderIds_ThrowsArgumentException()
    {
        Func<Task> act = () => _sut.AcceptSuggestionAsync(
            new List<int>(), poolId: null, "admin");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Нет заказов*");
    }

    [Fact]
    public async Task AcceptSuggestionAsync_ExistingPool_PoolNotFound_Throws()
    {
        _poolRepoMock
            .Setup(r => r.GetById(TargetPoolId))
            .ReturnsAsync((ConsolidationPool?)null);

        Func<Task> act = () => _sut.AcceptSuggestionAsync(
            new List<int> { 1 }, TargetPoolId, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не найден*");
    }

    [Fact]
    public async Task AcceptSuggestionAsync_ExistingPool_NotAllOrdersAvailable_Throws()
    {
        // Запросили 2 заказа, а в БД доступен только один
        var availableOrder = CreateFreeOrder(1, OrderNumber, 150m, 9);

        _poolRepoMock.Setup(r => r.GetById(TargetPoolId))
            .ReturnsAsync(new ConsolidationPool
            {
                Id = TargetPoolId,
                TargetWeek = 8,
                TotalWeight = 100m,
                Status = Status.Paid,
                IsDeleted = false
            });

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { availableOrder }.BuildMock());

        Func<Task> act = () => _sut.AcceptSuggestionAsync(
            new List<int> { 1, 2 }, TargetPoolId, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*уже в пуле или недоступны*");

        _poolHistoryRepoMock.Verify(
            r => r.Create(It.IsAny<ConsolidationPoolHistory>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_ExistingPool_AddsAllAndUpdatesPool()
    {
        // ==================== ARRANGE ====================
        var pool = new ConsolidationPool
        {
            Id = TargetPoolId,
            TargetWeek = 8,
            TotalWeight = 300m,
            Status = Status.Paid,
            IsDeleted = false
        };

        var order1 = CreateFreeOrder(1, OrderNumber, 150m, leadTime: 9);
        var order2 = CreateFreeOrder(2, SecondOrderNumber, 200m, leadTime: 6);

        _poolRepoMock.Setup(r => r.GetById(TargetPoolId)).ReturnsAsync(pool);
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order1, order2 }.BuildMock());

        // ==================== ACT ====================
        await _sut.AcceptSuggestionAsync(
            new List<int> { 1, 2 }, poolId: TargetPoolId, "admin");

        // ==================== ASSERT ====================
        // Пул пересчитан: TargetWeek = max(8, max(9, 6)) = 9
        pool.TargetWeek.Should().Be(9);
        pool.TotalWeight.Should().Be(650m);

        // Оба заказа привязаны к пулу
        order1.ConsolidationPoolId.Should().Be(TargetPoolId);
        order2.ConsolidationPoolId.Should().Be(TargetPoolId);

        // Пул обновлён, оба заказа обновлены
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);
        _orderRepoMock.Verify(r => r.Update(order1), Times.Once);
        _orderRepoMock.Verify(r => r.Update(order2), Times.Once);

        // Две записи OrderAdded с правильным PoolId
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.OrderAdded &&
                h.PoolId == TargetPoolId &&
                h.ChangedBy == "admin")),
            Times.Exactly(2));

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_NewPool_CreatesPoolAndAssignsAllOrders()
    {
        // ==================== ARRANGE ====================
        var order1 = CreateFreeOrder(1, OrderNumber, 150m, leadTime: 9);
        var order2 = CreateFreeOrder(2, SecondOrderNumber, 200m, leadTime: 6);

        ConsolidationPool? capturedPool = null;
        var newPoolId = 100;

        _poolRepoMock
            .Setup(r => r.Create(It.IsAny<ConsolidationPool>()))
            .Callback<ConsolidationPool>(p =>
            {
                capturedPool = p;
                p.Id = newPoolId;
            })
            .Returns(Task.CompletedTask);

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order> { order1, order2 }.BuildMock());

        // Для GetNextPoolColorAsync — пустой список пулов → вернётся первый цвет из палитры
        _poolRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<ConsolidationPool>().BuildMock());

        // ==================== ACT ====================
        await _sut.AcceptSuggestionAsync(
            new List<int> { 1, 2 }, poolId: null, "admin");

        // ==================== ASSERT ====================
        // Пул создан с правильными параметрами
        capturedPool.Should().NotBeNull();
        capturedPool!.TargetWeek.Should().Be(9);
        capturedPool.TotalWeight.Should().Be(350m);
        capturedPool.IsDeleted.Should().BeFalse();
        capturedPool.Color.Should().NotBeNullOrEmpty();

        // Оба заказа привязаны к новому пулу
        order1.ConsolidationPoolId.Should().Be(newPoolId);
        order2.ConsolidationPoolId.Should().Be(newPoolId);

        // Пул создан, оба заказа обновлены
        _poolRepoMock.Verify(r => r.Create(It.IsAny<ConsolidationPool>()), Times.Once);
        _orderRepoMock.Verify(r => r.Update(order1), Times.Once);
        _orderRepoMock.Verify(r => r.Update(order2), Times.Once);

        // Запись о создании пула
        _poolHistoryRepoMock.Verify(
            r => r.Create(It.Is<ConsolidationPoolHistory>(h =>
                h.EventType == ConsolidationPoolEventType.Created &&
                h.PoolId == newPoolId &&
                h.ChangedBy == "admin")),
            Times.Once);

        // SaveChanges вызывается дважды: создание пула и запись истории
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
