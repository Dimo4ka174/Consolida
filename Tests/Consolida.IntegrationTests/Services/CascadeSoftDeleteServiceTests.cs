using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using FluentAssertions;
using DB.Entity;
using Moq;
using DB;

namespace Consolida.IntegrationTests.Services;

public class CascadeSoftDeleteServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;
    private CascadeSoftDeleteService _sut = null!;

    // ============================================================
    // IAsyncLifetime - для InitializeAsync / DisposeAsync, которые вызываются для каждого теста
    // ============================================================

    public async Task InitializeAsync()
    {
        // 1. Открываем in-memory Sqlite. Он существует только пока открыт connection.
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        // 2. Создаём DbContext поверх этого соединения
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);

        // 3. Создаём схему БД
        await _context.Database.EnsureCreatedAsync();

        // 4. Готовим ServiceProvider для UnitOfWork
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        var serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWork(_context, serviceProvider);

        // 5. Мокаем кэш-сервисы — они нам не нужны в этом тесте
        var cityCacheMock = new Mock<ICacheService<City>>();
        var companyCacheMock = new Mock<ICacheService<Company>>();
        var manufacturerCacheMock = new Mock<ICacheService<Manufacturer>>();
        var measureUnitCacheMock = new Mock<ICacheService<MeasureUnit>>();
        var taxTypeCacheMock = new Mock<ICacheService<TaxType>>();

        // 6. Создаём SUT
        _sut = new CascadeSoftDeleteService(
            _unitOfWork,
            NullLogger<CascadeSoftDeleteService>.Instance,
            cityCacheMock.Object,
            companyCacheMock.Object,
            manufacturerCacheMock.Object,
            measureUnitCacheMock.Object,
            taxTypeCacheMock.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    // ============================================================
    // TESTS
    // ============================================================

    [Fact]
    public async Task DeleteOrder_MarksOrderAndRelatedEntitiesAsDeleted()
    {
        // ==================== ARRANGE ====================
        // Собираем минимальный граф: Order → OrderProduct + OrderTax + OrderStatusHistory
        var order = new Order
        {
            Id = 1,
            OrderNumber = "1000/26",
            CustomerId = null,
            CreationDate = DateTime.UtcNow,
            LastChangeDate = DateTime.UtcNow,
            DateCreationTKP = DateTime.UtcNow,
            LastStatusChangeDate = DateTime.UtcNow,
            Priority = DB.Entity.Enum.Priority.Medium,
            Status = DB.Entity.Enum.Status.Paid,
            ExchangeRate = 12.5m,
            TotalWeight = 10m,
            TotalCost = 1000m,
            IsDeleted = false
        };
        _context.Orders.Add(order);

        var product = new Product
        {
            Id = 1,
            Name = "Test",
            Model = "TP-1",
            Price = 100m,
            IsDeleted = false
        };
        _context.Products.Add(product);

        var orderProduct = new OrderProduct
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 1,
            Price = 100m,
            Weight = 1.5m,
            TotalPrice = 100m,
            DeliveryDate = DateTime.UtcNow,
            LeadTime = 9,
            Comment = "",
            IsDeleted = false
        };
        _context.OrdersProducts.Add(orderProduct);

        var statusHistory = new OrderStatusHistory
        {
            Id = 1,
            OrderId = 1,
            OldStatus = DB.Entity.Enum.Status.Registered,
            NewStatus = DB.Entity.Enum.Status.Paid,
            ChangeDate = DateTime.UtcNow,
            ChangedBy = "admin",
            IsDeleted = false
        };
        _context.OrderStatusHistory.Add(statusHistory);

        await _context.SaveChangesAsync();

        // ==================== ACT ====================
        await _sut.DeleteOrder(1);

        // ==================== ASSERT ====================
        _context.ChangeTracker.Clear();// ExecuteUpdateAsync работает в обход ChangeTracker. Он обновляет БД, но tracked-сущности в памяти остаются со старыми значениями.

        var deletedOrder = await _context.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(o => o.Id == 1);
        deletedOrder.IsDeleted.Should().BeTrue();

        var deletedOrderProduct = await _context.OrdersProducts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(op => op.Id == 1);
        deletedOrderProduct.IsDeleted.Should().BeTrue();

        var deletedHistory = await _context.OrderStatusHistory
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(h => h.Id == 1);
        deletedHistory.IsDeleted.Should().BeTrue();

        // Продукт НЕ должен быть удалён — Order.Product не входит в каскад Order
        var product2 = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(p => p.Id == 1);
        product2.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrder_NonExistentOrder_DoesNotThrow()
    {
        // ==================== ACT ====================
        // Метод не проверяет, существует ли заказ, просто пытается помечать
        Func<Task> act = () => _sut.DeleteOrder(999);

        // ==================== ASSERT ====================
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteOrder_InvalidId_ThrowsArgumentException()
    {
        // ==================== ACT + ASSERT ====================
        Func<Task> act = () => _sut.DeleteOrder(0);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid Order ID*");
    }
}
