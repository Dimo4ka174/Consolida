using Application.DataAccessLayer.Service.Common;
using Application.DataAccessLayer.Service.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using FluentAssertions;
using DB.Entity.Enum;
using DB.Entity;
using DB;

namespace Consolida.IntegrationTests.Services;

public class ConsolidationBoardTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;
    private ConsolidationService _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        var serviceProvider = services.BuildServiceProvider();

        _unitOfWork = new UnitOfWork(_context, serviceProvider);

        _sut = new ConsolidationService(_unitOfWork);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetBoardAsync_NoOrders_ReturnsEmptyBoard()
    {
        var board = await _sut.GetBoardAsync(weekSpan: 1, weightLimit: null);

        board.Should().NotBeNull();
        board.Columns.Should().BeEmpty();
        board.Pools.Should().BeEmpty();
        board.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBoardAsync_FreePaidOrders_GroupsByLeadTimeIntoColumns()
    {
        // ==================== ARRANGE ====================
        var company = new Company
        {
            Id = 1,
            Name = "Test Company",
            Country = Country.Russia,
            IsDeleted = false
        };
        _context.Companies.Add(company);

        var customer = new Customer
        {
            Id = 1,
            CompanyId = 1,
            LastName = "Ivanov",
            FirstName = "Ivan",
            IsDeleted = false
        };
        _context.Customers.Add(customer);

        // Свободный заказ 8 недель
        var order1 = new Order
        {
            Id = 1,
            OrderNumber = "1001/26",
            CustomerId = 1,
            Status = Status.Paid,
            TotalWeight = 100m,
            TotalCost = 1000m,
            ConsolidationPoolId = null,
            IsDeleted = false,
            CreationDate = DateTime.UtcNow,
            LastChangeDate = DateTime.UtcNow,
            DateCreationTKP = DateTime.UtcNow,
            LastStatusChangeDate = DateTime.UtcNow,
            Priority = Priority.Medium,
            ExchangeRate = 12.5m,
            OrdersProducts = new List<OrderProduct>
            {
                new() { Id = 1, Quantity = 1, Price = 100, Weight = 100m,
                        TotalPrice = 100, DeliveryDate = DateTime.UtcNow,
                        LeadTime = 8, Comment = "", IsDeleted = false }
            }
        };

        // Свободный заказ 6 недель
        var order2 = new Order
        {
            Id = 2,
            OrderNumber = "1002/26",
            CustomerId = 1,
            Status = Status.Paid,
            TotalWeight = 200m,
            TotalCost = 2000m,
            ConsolidationPoolId = null,
            IsDeleted = false,
            CreationDate = DateTime.UtcNow,
            LastChangeDate = DateTime.UtcNow,
            DateCreationTKP = DateTime.UtcNow,
            LastStatusChangeDate = DateTime.UtcNow,
            Priority = Priority.Medium,
            ExchangeRate = 12.5m,
            OrdersProducts = new List<OrderProduct>
            {
                new() { Id = 2, Quantity = 1, Price = 200, Weight = 200m,
                        TotalPrice = 200, DeliveryDate = DateTime.UtcNow,
                        LeadTime = 6, Comment = "", IsDeleted = false }
            }
        };

        // Заказ в статусе Registered — не должен попасть в free (не Paid)
        var order3 = new Order
        {
            Id = 3,
            OrderNumber = "1003/26",
            CustomerId = 1,
            Status = Status.Registered,
            TotalWeight = 300m,
            TotalCost = 3000m,
            ConsolidationPoolId = null,
            IsDeleted = false,
            CreationDate = DateTime.UtcNow,
            LastChangeDate = DateTime.UtcNow,
            DateCreationTKP = DateTime.UtcNow,
            LastStatusChangeDate = DateTime.UtcNow,
            Priority = Priority.Medium,
            ExchangeRate = 12.5m,
            OrdersProducts = new List<OrderProduct>
            {
                new() { Id = 3, Quantity = 1, Price = 300, Weight = 300m,
                        TotalPrice = 300, DeliveryDate = DateTime.UtcNow,
                        LeadTime = 8, Comment = "", IsDeleted = false }
            }
        };

        _context.Orders.AddRange(order1, order2, order3);
        await _context.SaveChangesAsync();

        // ==================== ACT ====================
        var board = await _sut.GetBoardAsync(weekSpan: 1, weightLimit: null);

        // ==================== ASSERT ====================
        // Колонки: 6 недель (order2) и 8 недель (order1). order3 отфильтрован.
        board.Columns.Should().HaveCount(2);

        var week6 = board.Columns.Single(c => c.Week == 6);
        week6.Orders.Should().HaveCount(1);
        week6.Orders[0].OrderId.Should().Be(2);

        var week8 = board.Columns.Single(c => c.Week == 8);
        week8.Orders.Should().HaveCount(1);
        week8.Orders[0].OrderId.Should().Be(1);

        // Никаких пулов
        board.Pools.Should().BeEmpty();
        board.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBoardAsync_TwoOrdersSameWeek_CreatesSuggestion()
    {
        // ==================== ARRANGE ====================
        var company = new Company { Id = 1, Name = "Test", Country = Country.Russia, IsDeleted = false };
        _context.Companies.Add(company);
        var customer = new Customer { Id = 1, CompanyId = 1, LastName = "I", FirstName = "I", IsDeleted = false };
        _context.Customers.Add(customer);

        // Два заказа с одинаковым lead time 7 недель, оба Paid и свободны
        for (int i = 1; i <= 2; i++)
        {
            _context.Orders.Add(new Order
            {
                Id = i,
                OrderNumber = $"100{i}/26",
                CustomerId = 1,
                Status = Status.Paid,
                TotalWeight = 100m * i,
                TotalCost = 1000m,
                ConsolidationPoolId = null,
                IsDeleted = false,
                CreationDate = DateTime.UtcNow,
                LastChangeDate = DateTime.UtcNow,
                DateCreationTKP = DateTime.UtcNow,
                LastStatusChangeDate = DateTime.UtcNow,
                Priority = Priority.Medium,
                ExchangeRate = 12.5m,
                OrdersProducts = new List<OrderProduct>
                {
                    new() { Id = i, Quantity = 1, Price = 100, Weight = 100m * i,
                            TotalPrice = 100, DeliveryDate = DateTime.UtcNow,
                            LeadTime = 7, Comment = "", IsDeleted = false }
                }
            });
        }
        await _context.SaveChangesAsync();

        // ==================== ACT ====================
        var board = await _sut.GetBoardAsync(weekSpan: 1, weightLimit: null);

        // ==================== ASSERT ====================
        board.Columns.Should().HaveCount(1);
        board.Columns[0].Week.Should().Be(7);
        board.Columns[0].Orders.Should().HaveCount(2);

        // Должна быть подсказка объединить оба заказа
        board.Suggestions.Should().HaveCount(1);
        board.Suggestions[0].PoolId.Should().BeNull();
        board.Suggestions[0].OrderIds.Should().BeEquivalentTo(new[] { 1, 2 });
        board.Suggestions[0].TotalWeight.Should().Be(300m);
    }

    [Fact]
    public async Task GetBoardAsync_WeightLimitFiltersSuggestion()
    {
        // ==================== ARRANGE ====================
        var company = new Company { Id = 1, Name = "Test", Country = Country.Russia, IsDeleted = false };
        _context.Companies.Add(company);
        var customer = new Customer { Id = 1, CompanyId = 1, LastName = "I", FirstName = "I", IsDeleted = false };
        _context.Customers.Add(customer);

        // 3 заказа по 100 кг каждый, одинаковый lead time
        for (int i = 1; i <= 3; i++)
        {
            _context.Orders.Add(new Order
            {
                Id = i,
                OrderNumber = $"100{i}/26",
                CustomerId = 1,
                Status = Status.Paid,
                TotalWeight = 100m,
                TotalCost = 1000m,
                ConsolidationPoolId = null,
                IsDeleted = false,
                CreationDate = DateTime.UtcNow,
                LastChangeDate = DateTime.UtcNow,
                DateCreationTKP = DateTime.UtcNow,
                LastStatusChangeDate = DateTime.UtcNow,
                Priority = Priority.Medium,
                ExchangeRate = 12.5m,
                OrdersProducts = new List<OrderProduct>
                {
                    new() { Id = i, Quantity = 1, Price = 100, Weight = 100m,
                            TotalPrice = 100, DeliveryDate = DateTime.UtcNow,
                            LeadTime = 7, Comment = "", IsDeleted = false }
                }
            });
        }
        await _context.SaveChangesAsync();

        // ==================== ACT ====================
        // Лимит 250 кг → жадно отберёт 2 заказа из 3
        var board = await _sut.GetBoardAsync(weekSpan: 1, weightLimit: 250m);

        // ==================== ASSERT ====================
        board.Suggestions.Should().HaveCount(1);
        board.Suggestions[0].OrderIds.Should().HaveCount(2);
        board.Suggestions[0].TotalWeight.Should().BeLessThanOrEqualTo(250m);
    }

    [Fact]
    public async Task GetBoardAsync_ActivePool_BuildsPoolDtoWithOrders()
    {
        // ==================== ARRANGE ====================
        var company = new Company { Id = 1, Name = "Test", Country = Country.Russia, IsDeleted = false };
        _context.Companies.Add(company);
        var customer = new Customer { Id = 1, CompanyId = 1, LastName = "I", FirstName = "I", IsDeleted = false };
        _context.Customers.Add(customer);

        var pool = new ConsolidationPool
        {
            Id = 1,
            TargetWeek = 8,
            TotalWeight = 200m,
            Status = Status.Paid,
            Color = "#FF0000",
            IsDeleted = false
        };
        _context.ConsolidationPools.Add(pool);

        // Заказ в пуле
        var orderInPool = new Order
        {
            Id = 1,
            OrderNumber = "1001/26",
            CustomerId = 1,
            Status = Status.Paid,
            TotalWeight = 200m,
            TotalCost = 2000m,
            ConsolidationPoolId = 1,
            IsDeleted = false,
            CreationDate = DateTime.UtcNow,
            LastChangeDate = DateTime.UtcNow,
            DateCreationTKP = DateTime.UtcNow,
            LastStatusChangeDate = DateTime.UtcNow,
            Priority = Priority.Medium,
            ExchangeRate = 12.5m,
            OrdersProducts = new List<OrderProduct>
            {
                new() { Id = 1, Quantity = 1, Price = 200, Weight = 200m,
                        TotalPrice = 200, DeliveryDate = DateTime.UtcNow,
                        LeadTime = 8, Comment = "", IsDeleted = false }
            }
        };
        _context.Orders.Add(orderInPool);

        // Пул в статусе Shipped — не должен попасть на доску
        var shippedPool = new ConsolidationPool
        {
            Id = 2,
            TargetWeek = 7,
            TotalWeight = 100m,
            Status = Status.Shipped,
            Color = "#00FF00",
            IsDeleted = false
        };
        _context.ConsolidationPools.Add(shippedPool);

        await _context.SaveChangesAsync();

        // ==================== ACT ====================
        var board = await _sut.GetBoardAsync(weekSpan: 1, weightLimit: null);

        // ==================== ASSERT ====================
        // Только 1 активный пул (Paid)
        board.Pools.Should().HaveCount(1);
        board.Pools[0].PoolId.Should().Be(1);
        board.Pools[0].TargetWeek.Should().Be(8);
        board.Pools[0].Color.Should().Be("#FF0000");
        board.Pools[0].Orders.Should().HaveCount(1);
        board.Pools[0].Orders[0].OrderId.Should().Be(1);

        // Свободных заказов нет — колонки пусты
        board.Columns.Should().BeEmpty();
    }
}
