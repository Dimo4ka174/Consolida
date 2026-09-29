using Application.ViewModels.OrderNotificationModel;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Entity;
using FluentAssertions;
using MockQueryable;
using DB.Entity;
using Moq;

namespace Consolida.UnitTests.Services;

public class OrderNotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRepository<OrderNotification>> _notificationRepoMock = new();
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly OrderNotificationService _sut;

    public OrderNotificationServiceTests()
    {
        _unitOfWorkMock
            .Setup(u => u.GetRepository<OrderNotification>())
            .Returns(_notificationRepoMock.Object);

        _unitOfWorkMock
            .Setup(u => u.GetRepository<Order>())
            .Returns(_orderRepoMock.Object);

        _sut = new OrderNotificationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsIdAndSavesChanges()
    {
        OrderNotification? capturedEntity = null;

        _notificationRepoMock
            .Setup(r => r.Create(It.IsAny<OrderNotification>()))
            .Callback<OrderNotification>(n =>
            {
                capturedEntity = n;
                n.Id = 42;
            })
            .Returns(Task.CompletedTask);

        var dto = new OrderNotificationCreateDto
        {
            OrderId = 1,
            Title = "  Проверить оплату  ",
            Description = "  Описание  ",
            DueDate = new DateTime(2026, 10, 15, 14, 30, 0)
        };

        // ==================== ACT ====================
        var resultId = await _sut.CreateAsync(dto, "admin");

        // ==================== ASSERT ====================
        resultId.Should().Be(42);

        capturedEntity.Should().NotBeNull();

        capturedEntity!.OrderId.Should().Be(1);
        capturedEntity.Title.Should().Be("Проверить оплату");     // Trim
        capturedEntity.Description.Should().Be("Описание");      // Trim
        capturedEntity.DueDate.Should().Be(new DateTime(2026, 10, 15)); // время обнулено
        capturedEntity.CreatedBy.Should().Be("admin");
        capturedEntity.IsCompleted.Should().BeFalse();
        capturedEntity.IsDeleted.Should().BeFalse();

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDescription_StoresNull()
    {
        // Arrange
        OrderNotification? captured = null;

        _notificationRepoMock
            .Setup(r => r.Create(It.IsAny<OrderNotification>()))
            .Callback<OrderNotification>(n => captured = n)
            .Returns(Task.CompletedTask);

        var dto = new OrderNotificationCreateDto
        {
            OrderId = 1,
            Title = "Test",
            Description = null,
            DueDate = DateTime.Today
        };

        // Act
        await _sut.CreateAsync(dto, "admin");

        // Assert
        captured!.Description.Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_ExistingNotification_MarksCompletedAndReturnsTrue()
    {
        // ==================== ARRANGE ====================
        var notification = new OrderNotification
        {
            Id = 1,
            OrderId = 1,
            Title = "Test",
            DueDate = DateTime.Today.AddDays(3),
            IsCompleted = false,
            IsDeleted = false
        };

        _notificationRepoMock
            .Setup(r => r.GetById(1))
            .ReturnsAsync(notification);

        // ==================== ACT ====================
        var result = await _sut.CompleteAsync(1, "admin");

        // ==================== ASSERT ====================
        result.Should().BeTrue();
        notification.IsCompleted.Should().BeTrue();
        notification.CompletedBy.Should().Be("admin");
        notification.CompletedAt.Should().NotBeNull();

        // Проверяем, что репозиторий получил Update с этой сущностью
        _notificationRepoMock.Verify(
            r => r.Update(notification),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_NonExistentNotification_ReturnsFalseWithoutSaving()
    {
        // Arrange
        _notificationRepoMock
            .Setup(r => r.GetById(999))
            .ReturnsAsync((OrderNotification?)null);

        // Act
        var result = await _sut.CompleteAsync(999, "admin");

        // Assert
        result.Should().BeFalse();

        // SaveChanges НЕ должен вызываться, если ничего не менялось
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_DeletedNotification_ReturnsFalse()
    {
        // Arrange
        var notification = new OrderNotification
        {
            Id = 1,
            OrderId = 1,
            Title = "Test",
            DueDate = DateTime.Today,
            IsCompleted = false,
            IsDeleted = true   // ← уже удалено
        };

        _notificationRepoMock
            .Setup(r => r.GetById(1))
            .ReturnsAsync(notification);

        // Act
        var result = await _sut.CompleteAsync(1, "admin");

        // Assert
        result.Should().BeFalse();
        notification.IsCompleted.Should().BeFalse(); // не изменилось
    }

    [Fact]
    public async Task DeleteAsync_ExistingNotification_MarksAsDeleted()
    {
        // Arrange
        var notification = new OrderNotification
        {
            Id = 1,
            OrderId = 1,
            Title = "Test",
            DueDate = DateTime.Today,
            IsDeleted = false
        };

        _notificationRepoMock
            .Setup(r => r.GetById(1))
            .ReturnsAsync(notification);

        // Act
        var result = await _sut.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        notification.IsDeleted.Should().BeTrue();

        _notificationRepoMock.Verify(r => r.Update(notification), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_CompletedNotification_ResetsCompletionFields()
    {
        // Arrange
        var notification = new OrderNotification
        {
            Id = 1,
            OrderId = 1,
            Title = "Test",
            DueDate = DateTime.Today,
            IsCompleted = true,
            CompletedAt = DateTime.Now.AddHours(-1),
            CompletedBy = "admin",
            IsDeleted = false
        };

        _notificationRepoMock
            .Setup(r => r.GetById(1))
            .ReturnsAsync(notification);

        // Act
        var result = await _sut.RestoreAsync(1, "admin");

        // Assert
        result.Should().BeTrue();
        notification.IsCompleted.Should().BeFalse();
        notification.CompletedAt.Should().BeNull();
        notification.CompletedBy.Should().BeNull();
    }

    [Fact]
    public async Task CheckOrderExistsAsync_OrderExists_ReturnsTrue()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "1000/26", IsDeleted = false }
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());

        // Act
        var result = await _sut.CheckOrderExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckOrderExistsAsync_OrderDeleted_ReturnsFalse()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "1000/26", IsDeleted = true }
        };

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());

        // Act
        var result = await _sut.CheckOrderExistsAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchOrdersAsync_EmptyQuery_ReturnsEmptyListWithoutDbCall()
    {
        // Act
        var result = await _sut.SearchOrdersAsync("");
        var result2 = await _sut.SearchOrdersAsync("   ");
        var result3 = await _sut.SearchOrdersAsync(null!);

        // Assert
        result.Should().BeEmpty();
        result2.Should().BeEmpty();
        result3.Should().BeEmpty();

        // Главное: репозиторий НЕ вызывался — экономим запрос в БД
        _orderRepoMock.Verify(
            r => r.GetQueryable(It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUrgentAsync_ReturnsOverdueAndUpcomingActiveNotifications()
    {
        // ==================== ARRANGE ====================
        var today = DateTime.Today;

        var notifications = new List<OrderNotification>
    {
        new()
        {
            Id = 1, OrderId = 1, Title = "Просрочено (3 дня)",
            DueDate = today.AddDays(-3), IsCompleted = false, IsDeleted = false
        },
        new()
        {
            Id = 2, OrderId = 1, Title = "Сегодня",
            DueDate = today, IsCompleted = false, IsDeleted = false
        },
        new()
        {
            Id = 3, OrderId = 1, Title = "Скоро (3 дня)",
            DueDate = today.AddDays(3), IsCompleted = false, IsDeleted = false
        },
        new()
        {
            Id = 4, OrderId = 1, Title = "Далеко (10 дней)",
            DueDate = today.AddDays(10), IsCompleted = false, IsDeleted = false
        },
        new()
        {
            Id = 5, OrderId = 1, Title = "Уже сделано",
            DueDate = today.AddDays(2), IsCompleted = true, IsDeleted = false
        },
        new()
        {
            Id = 6, OrderId = 1, Title = "Удалено",
            DueDate = today.AddDays(1), IsCompleted = false, IsDeleted = true
        }
    };

        _notificationRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(notifications.BuildMock());

        // ==================== ACT ====================
        var result = await _sut.GetUrgentAsync(7);

        // ==================== ASSERT ====================
        // Попадают 3 штуки
        result.Should().HaveCount(3);

        // Состав (без учёта порядка)
        result.Select(n => n.Id).Should().BeEquivalentTo(new[] { 1, 2, 3 });

        // Сортировка по DueDate (по возрастанию)
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        result[2].Id.Should().Be(3);

        // Проверяем ViewModel-флаги
        result[0].IsOverdue.Should().BeTrue();
        result[0].IsUrgent.Should().BeFalse();

        result[1].IsOverdue.Should().BeFalse();
        result[1].IsUrgent.Should().BeTrue();

        result[2].IsOverdue.Should().BeFalse();
        result[2].IsUrgent.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DeletedNotification_ReturnsFalseAndDoesNotRestore()
    {
        // ==================== ARRANGE ====================
        var notification = new OrderNotification
        {
            Id = 1,
            OrderId = 1,
            Title = "Test",
            DueDate = DateTime.Today,
            IsCompleted = false,
            IsDeleted = true
        };

        _notificationRepoMock
            .Setup(r => r.GetById(1))
            .ReturnsAsync(notification);

        // ==================== ACT ====================
        var result = await _sut.RestoreAsync(1, "admin");

        // ==================== ASSERT ====================
        // Текущее поведение: возвращает false, ничего не меняет.
        result.Should().BeFalse();
        notification.IsDeleted.Should().BeTrue();
        notification.IsCompleted.Should().BeFalse();

        // SaveChanges не вызывался
        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        // Update не вызывался
        _notificationRepoMock.Verify(
            r => r.Update(It.IsAny<OrderNotification>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    public async Task GetAllAsync_RespectsIncludeCompletedFlag(bool includeCompleted, int expectedCount)
    {
        // ==================== ARRANGE ====================
        var today = DateTime.Today;

        var notifications = new List<OrderNotification>
        {
            new() { Id = 1, OrderId = 1, Title = "Активно 1", DueDate = today.AddDays(3), IsCompleted = false, IsDeleted = false },
            new() { Id = 2, OrderId = 1, Title = "Активно 2", DueDate = today.AddDays(5), IsCompleted = false, IsDeleted = false },
            new() { Id = 3, OrderId = 1, Title = "Выполнено", DueDate = today.AddDays(1), IsCompleted = true,  IsDeleted = false },
            new() { Id = 4, OrderId = 1, Title = "Удалено", DueDate = today, IsCompleted = false, IsDeleted = true  }
        };

        _notificationRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(notifications.BuildMock());

        // ==================== ACT ====================
        var result = await _sut.GetAllAsync(includeCompleted);

        // ==================== ASSERT ====================
        result.Should().HaveCount(expectedCount);
        result.All(n => !n.IsCompleted || includeCompleted).Should().BeTrue();

        if (includeCompleted)
        {
            result[0].IsCompleted.Should().BeFalse();
            result[1].IsCompleted.Should().BeFalse();
            result[2].IsCompleted.Should().BeTrue();
        }
    }
}
