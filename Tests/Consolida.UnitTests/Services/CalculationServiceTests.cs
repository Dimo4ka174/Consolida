using Application.DataAccessLayer.Service.OrderService;
using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.OrderModel;
using FluentAssertions;
using MockQueryable;
using DB.Entity;
using Moq;

namespace Consolida.UnitTests.Services;

public class CalculationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly Mock<IRepository<TaxType>> _taxTypeRepoMock = new();
    private readonly Mock<IRepository<CodeTNVD>> _codeRepoMock = new();
    private readonly Mock<IRepository<OrderTaxProduct>> _orderTaxProductRepoMock = new();
    private readonly Mock<ICurrencyCacheService> _currencyMock = new();
    private readonly CalculationService _sut;

    public CalculationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.GetRepository<Order>()).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<TaxType>()).Returns(_taxTypeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<CodeTNVD>()).Returns(_codeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<OrderTaxProduct>()).Returns(_orderTaxProductRepoMock.Object);

        _sut = new CalculationService(_unitOfWorkMock.Object, _currencyMock.Object);
    }

    [Fact]
    public async Task BuildCalculationViewModelAsync_MinimalOrder_ReturnsCorrectTotals()
    {
        const decimal exchangeRate = 12.5m;
        const int quantity = 2;
        const decimal priceCny = 1000m;

        SetupOrders(new List<Order>
        {
            new() { Id = 1, OrderNumber = "1000/26", ExchangeRate = exchangeRate, IsDeleted = false }
        });

        SetupTaxTypes(new List<TaxType>
        {
            new() { Id = 1, Name = "Комиссия банка", MeasureUnit = new MeasureUnit { Name = "%" } },
            new() { Id = 2, Name = "Маржа", MeasureUnit = new MeasureUnit { Name = "%" } },
            new() { Id = 3, Name = "Не предвиденные расходы", MeasureUnit = new MeasureUnit { Name = "%" } },
        });

        SetupCodes(new List<CodeTNVD>());
        SetupOrderTaxProducts(new List<OrderTaxProduct>());

        var model = new ExportOrderViewModel
        {
            OrderId = 1,
            OrderNumber = "1000/26",
            ExchangeRate = exchangeRate,
            Comment = "Test",
            OrderTaxes = new Dictionary<int, decimal>
            {
                { 1, 2m },   // Комиссия банка 2%
                { 2, 30m },  // Маржа 30%
            },
            Products = new List<ProductData>
            {
                new()
                {
                    ProductId = 1,
                    ProductName = "Test Product",
                    Model = "TP-1",
                    Quantity = quantity,
                    Price = priceCny,
                    Weight = 1.5m,
                    MarginRate = 30m,
                    UnforeseenExpensesRate = 0m,
                    ProductTaxes = new Dictionary<int, ProductTaxItem>()
                }
            }
        };

        var result = await _sut.BuildCalculationViewModelAsync(model, "admin");

        result.Should().NotBeNull();
        result.OrderId.Should().Be(1);
        result.ExchangeRate.Should().Be(exchangeRate);
        result.CalculatedBy.Should().Be("admin");

        // Цена в рублях = 1000 * 12.5 = 12500
        var product = result.Products.Should().ContainSingle().Subject;
        product.PriceInRUB.Should().Be(12500m);
        product.PriceTotalRUB.Should().Be(25000m);  // 12500 * 2

        // Банковская комиссия 2% от 12500 = 250 за единицу, 500 за всё
        product.BankCommissionPerUnit.Should().Be(250m);
        product.BankCommissionTotal.Should().Be(500m);

        // Маржа 30% от 12500 = 3750 за единицу, 7500 за всё
        product.MarginPerUnit.Should().Be(3750m);
        product.MarginTotal.Should().Be(7500m);

        // Итого без НДС = 25000 + 500 + 7500 = 33000
        product.TotalWithoutVAT.Should().Be(33000m);

        // С НДС 22% = 33000 * 1.22 = 40260
        product.TotalWithVAT.Should().Be(40260m);

        // Итоги заказа
        result.TotalCostWithoutVAT.Should().Be(33000m);
        result.TotalCostWithVAT.Should().Be(40260m);
        result.TotalVAT.Should().Be(7260m);
        result.TotalProfit.Should().Be(7500m);
    }

    [Fact]
    public async Task BuildCalculationViewModelAsync_EmptyProductList_ReturnsZeroTotals()
    {
        // Arrange
        SetupOrders(new List<Order>
        {
            new() { Id = 1, OrderNumber = "1000/26", ExchangeRate = 12.5m, IsDeleted = false }
        });
        SetupTaxTypes(new List<TaxType>());
        SetupCodes(new List<CodeTNVD>());
        SetupOrderTaxProducts(new List<OrderTaxProduct>());

        var model = new ExportOrderViewModel
        {
            OrderId = 1,
            OrderNumber = "1000/26",
            ExchangeRate = 12.5m,
            OrderTaxes = new Dictionary<int, decimal>(),
            Products = new List<ProductData>()
        };

        // Act
        var result = await _sut.BuildCalculationViewModelAsync(model, "admin");

        // Assert
        result.Products.Should().BeEmpty();
        result.TotalCostWithoutVAT.Should().Be(0m);
        result.TotalCostWithVAT.Should().Be(0m);
        result.TotalProfit.Should().Be(0m);
    }

    [Fact]
    public async Task BuildCalculationViewModelAsync_ProductWithDutyAndAdditionalTax_IncludesThemInTotal()
    {
        // ============ ARRANGE ============
        const decimal exchangeRate = 12.5m;
        const int quantity = 1;
        const decimal priceCny = 1000m;

        // Справочники
        var measureUnitPercent = new MeasureUnit { Id = 1, Name = "%" };
        var measureUnitRub = new MeasureUnit { Id = 2, Name = "₽" };

        SetupOrders(new List<Order>
        {
            new()
            {
                Id = 1,
                OrderNumber = "1000/26",
                ExchangeRate = exchangeRate,
                IsDeleted = false,
                OrdersProducts = new List<OrderProduct>
                {
                    new()
                    {
                        Id = 1,
                        OrderId = 1,
                        ProductId = 1,
                        Quantity = quantity,
                        Price = priceCny,
                        IsDeleted = false
                    }
                },
                OrderTaxes = new List<OrderTax>()
            }
        });

        SetupTaxTypes(new List<TaxType>
        {
            new() { Id = 1, Name = "Комиссия банка", MeasureUnit = measureUnitPercent },
            new() { Id = 2, Name = "Маржа", MeasureUnit = measureUnitPercent },
            new() { Id = 3, Name = "Не предвиденные расходы", MeasureUnit = measureUnitPercent },
            new() { Id = 4, Name = "Таможенный брокер", MeasureUnit = measureUnitRub },
        });

        // Код ТНВЭД с пошлиной 5%
        SetupCodes(new List<CodeTNVD>
        {
            new() { Id = 1, Name = "8517", Rate = 5m, IsDeleted = false }
        });

        // Дополнительный расход: 25000 ₽ от таможенного брокера
        SetupOrderTaxProducts(new List<OrderTaxProduct>
        {
            new()
            {
                Id = 1,
                OrderId = 1,
                OrderProductId = 1,
                TaxTypeId = 4,
                Cost = 25000m,
                IsDeleted = false,
                OrderProduct = new OrderProduct
                {
                    Id = 1,
                    OrderId = 1,
                    ProductId = 1,
                    IsDeleted = false
                },
                TaxType = new TaxType
                {
                    Id = 4,
                    Name = "Таможенный брокер",
                    MeasureUnit = measureUnitRub
                }
            }
        });

        var model = new ExportOrderViewModel
        {
            OrderId = 1,
            OrderNumber = "1000/26",
            ExchangeRate = exchangeRate,
            OrderTaxes = new Dictionary<int, decimal>
        {
            { 1, 2m },   // Комиссия банка 2%
            { 2, 30m },  // Маржа 30%
        },
            Products = new List<ProductData>
        {
            new()
            {
                ProductId = 1,
                ProductName = "Test Product",
                Model = "TP-1",
                Quantity = quantity,
                Price = priceCny,
                Weight = 1.5m,
                MarginRate = 30m,
                UnforeseenExpensesRate = 0m,
                CodeTNVD = "8517",   // ← код ТНВЭД с пошлиной 5%
                ProductTaxes = new Dictionary<int, ProductTaxItem>()
            }
        }
        };

        // ============ ACT ============
        var result = await _sut.BuildCalculationViewModelAsync(model, "admin");

        // ============ ASSERT ============
        var product = result.Products.Should().ContainSingle().Subject;

        product.PriceInRUB.Should().Be(12500m);

        // Пошлина 5% от 12500 = 625
        product.DutyRate.Should().Be(5m);
        product.DutyTotal.Should().Be(625m);

        // Банковская комиссия 2% = 250
        product.BankCommissionTotal.Should().Be(250m);

        // Маржа 30% = 3750
        product.MarginTotal.Should().Be(3750m);

        // Дополнительные расходы — 25000 (один налог "Таможенный брокер")
        product.ProductTaxes.Should().ContainSingle()
            .Which.Name.Should().Be("Таможенный брокер");
        product.ProductTaxes.First().Cost.Should().Be(25000m);
        product.TotalTaxes.Should().Be(25000m);

        // Итого без НДС = 12500 (цена) + 250 (комиссия) + 3750 (маржа) + 625 (пошлина) + 25000 (расход) = 42125
        product.TotalWithoutVAT.Should().Be(42125m);

        // С НДС 22% = 42125 * 1.22 = 51392.50
        product.TotalWithVAT.Should().Be(42125m * 1.22m);

        // Итого по заказу
        result.TotalCostWithoutVAT.Should().Be(42125m);
        result.TotalProfit.Should().Be(3750m);
    }

    // ============ Helpers ============

    private void SetupOrders(List<Order> orders)
    {
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());
    }

    private void SetupTaxTypes(List<TaxType> taxTypes)
    {
        _taxTypeRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(taxTypes.BuildMock());
    }

    private void SetupCodes(List<CodeTNVD> codes)
    {
        _codeRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(codes.BuildMock());
    }

    private void SetupOrderTaxProducts(List<OrderTaxProduct> taxProducts)
    {
        _orderTaxProductRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(taxProducts.BuildMock());
    }
}
