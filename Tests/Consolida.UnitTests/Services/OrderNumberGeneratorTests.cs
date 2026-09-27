using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.OrderService;
using MockQueryable.Moq;
using FluentAssertions;
using MockQueryable;
using DB.Entity;
using Moq;

namespace Consolida.UnitTests.Services;

public class OrderNumberGeneratorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRepository<Order>> _orderRepoMock = new();
    private readonly OrderNumberGenerator _sut;

    public OrderNumberGeneratorTests()
    {
        _unitOfWorkMock
            .Setup(u => u.GetRepository<Order>())
            .Returns(_orderRepoMock.Object);

        _sut = new OrderNumberGenerator(_unitOfWorkMock.Object);
    }

    // ---------- TryExtractFromFileName ----------

    [Theory]
    [InlineData("1234Request.xlsx", "1234/26")]
    [InlineData("1234_Request.xlsx", "1234/26")]
    [InlineData("1234 Request.xlsx", "1234/26")]
    [InlineData("1234_Request_23.01.2026.xlsx", "1234/26")]
    [InlineData("1234 Levin Request_12.11.2025.xlsx", "1234/26")]
    public void TryExtractFromFileName_MatchesCommonPatterns_ReturnsTrue(string fileName, string expectedPrefix)
    {
        // Act
        var success = _sut.TryExtractFromFileName(fileName, out var orderNumber);

        // Assert
        success.Should().BeTrue();
        orderNumber.Should().StartWith(expectedPrefix.Split('/')[0] + "/");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Request.xlsx")]
    [InlineData("Request_1234.xlsx")]
    [InlineData("Request 1234.xlsx")]
    [InlineData("file_without_number.xlsx")]
    public void TryExtractFromFileName_NoNumberInName_ReturnsFalse(string fileName)
    {
        var success = _sut.TryExtractFromFileName(fileName, out var orderNumber);

        success.Should().BeFalse();
        orderNumber.Should().BeEmpty();
    }

    // ---------- GenerateDuplicateOrderNumber ----------

    [Fact]
    public async Task GenerateDuplicateOrderNumber_NoDuplicatesYet_ReturnsBaseNumberWithDashOne()
    {
        // Arrange
        SetupEmptyOrderNumbersLookup();
        var original = $"1000/{DateTime.Now:yy}";

        // Act
        var result = await _sut.GenerateDuplicateOrderNumber(original);

        // Assert
        result.Should().Be($"1000-1/{DateTime.Now:yy}");
    }

    [Fact]
    public async Task GenerateDuplicateOrderNumber_ExistingDuplicates_ReturnsNextNumber()
    {
        // Arrange
        var year = DateTime.Now.ToString("yy");
        SetupOrderNumbersLookup(new[] { $"1000/{year}", $"1000-1/{year}", $"1000-2/{year}" });

        // Act
        var result = await _sut.GenerateDuplicateOrderNumber($"1000/{year}");

        // Assert
        result.Should().Be($"1000-3/{year}");
    }

    [Fact]
    public async Task GenerateDuplicateOrderNumber_MalformedOriginal_FallsBackToNewNumber()
    {
        // Arrange
        SetupEmptyOrderNumbersLookup();

        // Act
        var result = await _sut.GenerateDuplicateOrderNumber("not-a-number");

        // Assert — просто проверяем, что вернулся корректный номер N/YY
        result.Should().MatchRegex(@"^\d+/\d{2}$");
    }

    // ---------- Helpers ----------

    private void SetupEmptyOrderNumbersLookup()
    {
        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(new List<Order>().BuildMock());
    }

    private void SetupOrderNumbersLookup(IEnumerable<string> existingNumbers)
    {
        var orders = existingNumbers.Select(n => new Order
        {
            Id = 1,
            OrderNumber = n,
            IsDeleted = false
        }).ToList();

        _orderRepoMock
            .Setup(r => r.GetQueryable(It.IsAny<bool>()))
            .Returns(orders.BuildMock());
    }
}
