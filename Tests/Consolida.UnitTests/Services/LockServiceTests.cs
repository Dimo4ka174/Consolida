using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using FluentAssertions;
using MockQueryable;
using DB.Entity;
using Moq;

namespace Consolida.UnitTests.Services;

public class LockServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly Mock<IRepository<ConsolidationPool>> _poolRepoMock = new();
    private readonly LockService _sut;

    public LockServiceTests()
    {
        _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _uowMock.Setup(u => u.ConsolidationPools).Returns(_poolRepoMock.Object);

        // ExecuteInTransactionAsync просто вызывает переданный делегат — эмулируем поведение реального UnitOfWork без реальной транзакции.
        _uowMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<LockResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<Task<LockResult>>, CancellationToken>((action, _) => action());

        _uowMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new LockService(_uowMock.Object);
    }

    // TryLockOrderAsync

    [Fact]
    public async Task TryLockOrderAsync_OrderNotFound_ReturnsFailure()
    {
        SetupOrders(new List<Order>());

        var result = await _sut.TryLockOrderAsync(999, "user1");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    [Fact]
    public async Task TryLockOrderAsync_FreeOrder_LocksAndReturnsSuccess()
    {
        var order = new Order { Id = 1, OrderNumber = "1000/26", IsDeleted = false };
        SetupOrders(new List<Order> { order });

        var result = await _sut.TryLockOrderAsync(1, "user1");

        result.Success.Should().BeTrue();
        order.LockedByUserId.Should().Be("user1");
        order.LockedAt.Should().NotBeNull();

        _orderRepoMock.Verify(r => r.Update(order), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryLockOrderAsync_AlreadyLockedByAnotherUser_ReturnsFailure()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow
        };
        SetupOrders(new List<Order> { order });

        var result = await _sut.TryLockOrderAsync(1, "user1");

        result.Success.Should().BeFalse();
        result.LockedBy.Should().Be("user2");
        result.Message.Should().Contain("user2");

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task TryLockOrderAsync_LockedBySameUser_RefreshesLock()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        SetupOrders(new List<Order> { order });

        var result = await _sut.TryLockOrderAsync(1, "user1");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TryLockOrderAsync_ExpiredLockFromAnotherUser_TakesLock()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow.AddMinutes(-10) // старше TTL в 5 минут
        };
        SetupOrders(new List<Order> { order });

        var result = await _sut.TryLockOrderAsync(1, "user1");

        result.Success.Should().BeTrue();
        order.LockedByUserId.Should().Be("user1");
    }

    [Fact]
    public async Task TryLockOrderAsync_DeletedOrder_TreatedAsNotFound()
    {
        var order = new Order { Id = 1, OrderNumber = "1000/26", IsDeleted = true };
        SetupOrders(new List<Order> { order });

        var result = await _sut.TryLockOrderAsync(1, "user1");

        result.Success.Should().BeFalse();
    }

    // TryLockPoolAsync

    [Fact]
    public async Task TryLockPoolAsync_PoolNotFound_ReturnsFailure()
    {
        SetupPools(new List<ConsolidationPool>());

        var result = await _sut.TryLockPoolAsync(999, "user1");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("не найдена");
    }

    [Fact]
    public async Task TryLockPoolAsync_FreePool_Locks()
    {
        var pool = new ConsolidationPool { Id = 1, IsDeleted = false };
        SetupPools(new List<ConsolidationPool> { pool });

        var result = await _sut.TryLockPoolAsync(1, "user1");

        result.Success.Should().BeTrue();
        pool.LockedByUserId.Should().Be("user1");
        pool.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TryLockPoolAsync_LockedByAnotherUser_ReturnsFailure()
    {
        var pool = new ConsolidationPool
        {
            Id = 1,
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow
        };
        SetupPools(new List<ConsolidationPool> { pool });

        var result = await _sut.TryLockPoolAsync(1, "user1");

        result.Success.Should().BeFalse();
        result.LockedBy.Should().Be("user2");
    }

    // UnlockOrderAsync

    [Fact]
    public async Task UnlockOrderAsync_OwnLock_Releases()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow
        };
        SetupOrders(new List<Order> { order });

        await _sut.UnlockOrderAsync(1, "user1");

        order.LockedByUserId.Should().BeNull();
        order.LockedAt.Should().BeNull();
        _orderRepoMock.Verify(r => r.Update(order), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockOrderAsync_AnotherUserLock_DoesNotRelease()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow
        };
        SetupOrders(new List<Order> { order });

        await _sut.UnlockOrderAsync(1, "user1");

        order.LockedByUserId.Should().Be("user2");
        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlockOrderAsync_OrderNotFound_DoesNothing()
    {
        SetupOrders(new List<Order>());

        await _sut.UnlockOrderAsync(999, "user1");

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlockOrderAsync_NoLockSet_DoesNothing()
    {
        var order = new Order { Id = 1, OrderNumber = "1000/26", IsDeleted = false };
        SetupOrders(new List<Order> { order });

        await _sut.UnlockOrderAsync(1, "user1");

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
    }

    // UnlockPoolAsync

    [Fact]
    public async Task UnlockPoolAsync_OwnLock_Releases()
    {
        var pool = new ConsolidationPool
        {
            Id = 1,
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow
        };
        SetupPools(new List<ConsolidationPool> { pool });

        await _sut.UnlockPoolAsync(1, "user1");

        pool.LockedByUserId.Should().BeNull();
        pool.LockedAt.Should().BeNull();
        _poolRepoMock.Verify(r => r.Update(pool), Times.Once);
    }

    [Fact]
    public async Task UnlockPoolAsync_AnotherUserLock_DoesNotRelease()
    {
        var pool = new ConsolidationPool
        {
            Id = 1,
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow
        };
        SetupPools(new List<ConsolidationPool> { pool });

        await _sut.UnlockPoolAsync(1, "user1");

        pool.LockedByUserId.Should().Be("user2");
        _poolRepoMock.Verify(r => r.Update(It.IsAny<ConsolidationPool>()), Times.Never);
    }

    // ReleaseExpiredLocksAsync

    [Fact]
    public async Task ReleaseExpiredLocksAsync_ExpiredOrder_Releases()
    {
        var expiredOrder = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var freshOrder = new Order
        {
            Id = 2,
            OrderNumber = "1001/26",
            IsDeleted = false,
            LockedByUserId = "user2",
            LockedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        SetupOrders(new List<Order> { expiredOrder, freshOrder });
        SetupPools(new List<ConsolidationPool>());

        await _sut.ReleaseExpiredLocksAsync();

        expiredOrder.LockedByUserId.Should().BeNull();
        expiredOrder.LockedAt.Should().BeNull();

        freshOrder.LockedByUserId.Should().Be("user2");
        freshOrder.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReleaseExpiredLocksAsync_ExpiredPool_Releases()
    {
        var expiredPool = new ConsolidationPool
        {
            Id = 1,
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        SetupOrders(new List<Order>());
        SetupPools(new List<ConsolidationPool> { expiredPool });

        await _sut.ReleaseExpiredLocksAsync();

        expiredPool.LockedByUserId.Should().BeNull();
        expiredPool.LockedAt.Should().BeNull();
    }

    [Fact]
    public async Task ReleaseExpiredLocksAsync_NothingExpired_NoUpdates()
    {
        var freshOrder = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            IsDeleted = false,
            LockedByUserId = "user1",
            LockedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        SetupOrders(new List<Order> { freshOrder });
        SetupPools(new List<ConsolidationPool>());

        await _sut.ReleaseExpiredLocksAsync();

        _orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
        _poolRepoMock.Verify(r => r.Update(It.IsAny<ConsolidationPool>()), Times.Never);
    }

    // Helpers

    private void SetupOrders(List<Order> orders)
    {
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());
    }

    private void SetupPools(List<ConsolidationPool> pools)
    {
        _poolRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(pools.BuildMock());
    }
}
