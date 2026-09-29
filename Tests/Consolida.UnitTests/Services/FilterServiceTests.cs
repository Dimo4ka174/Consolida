using Application.DataAccessLayer.Service.Common;
using FluentAssertions;
using DB.Abstract;

namespace Consolida.UnitTests.Services;

public class FilterServiceTests
{
    private readonly FilterService<TestEntity> _sut = new();

    private static readonly List<TestEntity> Data = new()
    {
        new() { Id = 1, Name = "Alpha",   Number = 10, Price = 100m, Status = TestEnum.First,  Nested = new() { Value = "X" } },
        new() { Id = 2, Name = "beta",    Number = 20, Price = 200m, Status = TestEnum.Second, Nested = new() { Value = "Y" } },
        new() { Id = 3, Name = "Gamma",   Number = 30, Price = 300m, Status = TestEnum.First,  Nested = new() { Value = "Z" } },
        new() { Id = 4, Name = "delta",   Number = 40, Price = 400m, Status = TestEnum.None,   Nested = new() { Value = "W" } },
        new() { Id = 5, Name = "EPSILON", Number = 50, Price = 500m, Status = TestEnum.Second, Nested = new() { Value = "X" } }
    };

    // Базовые сценарии

    [Fact]
    public void ApplyFilters_NoFilters_ReturnsAllPaged()
    {
        var parameters = new FilterParams { Page = 1, PageSize = 10 };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(5);
        result.Data.Should().HaveCount(5);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ApplyFilters_Pagination_WorksCorrectly()
    {
        var parameters = new FilterParams { Page = 2, PageSize = 2 };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(5);
        result.Data.Should().HaveCount(2);
        result.Data.Select(x => x.Id).Should().BeEquivalentTo(new int?[] { 3, 4 });
    }

    [Fact]
    public void ApplyFilters_Enumerable_OverloadUsesSameLogic()
    {
        var parameters = new FilterParams { Page = 1, PageSize = 10 };

        var result = _sut.ApplyFilters(Data.AsEnumerable(), parameters);

        result.TotalItems.Should().Be(5);
    }

    // Поиск по строке

    [Fact]
    public void ApplyFilters_SearchString_CaseInsensitiveContains()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SearchString = "alp",
            SearchProperty = "Name"
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.Data.Should().ContainSingle();
        result.Data[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public void ApplyFilters_SearchStringUppercase_MatchesLowercase()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SearchString = "BETA",
            SearchProperty = "Name"
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.Data.Should().ContainSingle();
        result.Data[0].Name.Should().Be("beta");
    }

    [Fact]
    public void ApplyFilters_SearchStringWithoutProperty_IgnoresFilter()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SearchString = "Alpha"
            // SearchProperty не задан
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(5);
    }

    [Fact]
    public void ApplyFilters_EmptySearchString_SkipsFilter()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SearchString = "",
            SearchProperty = "Name"
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(5);
    }

    // Операторы

    [Theory]
    [InlineData("==", 30, 1)]
    [InlineData("!=", 30, 4)]
    [InlineData(">", 30, 2)]
    [InlineData(">=", 30, 3)]
    [InlineData("<", 30, 2)]
    [InlineData("<=", 30, 3)]
    public void ApplyFilters_NumericOperators_Work(string op, int value, int expectedCount)
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Number", SearchValue = value.ToString(), Operator = op }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(expectedCount);
    }

    // Enum

    [Fact]
    public void ApplyFilters_EnumByName_ParsesCorrectly()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Status", SearchValue = "First", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public void ApplyFilters_EnumByName_CaseInsensitive()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Status", SearchValue = "second", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public void ApplyFilters_EnumByNumber_ParsesCorrectly()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Status", SearchValue = "1", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(2);
    }

    // Nested property

    [Fact]
    public void ApplyFilters_NestedProperty_ResolvesViaDotPath()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Nested.Value", SearchValue = "X", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public void ApplyFilters_NestedPropertyPath_CaseInsensitivePropertyNames()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "nested.value", SearchValue = "Y", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(1);
    }

    // Сортировка

    [Fact]
    public void ApplyFilters_SortByNameAsc_ReturnsSorted()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SortProperty = "Name",
            SortDirection = "asc"
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.Data.Select(x => x.Name)
            .Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyFilters_SortByNumberDesc_ReturnsSortedDesc()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            SortProperty = "Number",
            SortDirection = "desc"
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.Data.Select(x => x.Number).Should().BeInDescendingOrder();
    }

    [Fact]
    public void ApplyFilters_NoSortProperty_SortsByIdAsc()
    {
        var parameters = new FilterParams { Page = 1, PageSize = 10 };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.Data.Select(x => x.Id).Should().BeInAscendingOrder();
    }

    // Комбинации 

    [Fact]
    public void ApplyFilters_MultipleAdditionalFilters_AllApplied()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Status", SearchValue = "First", Operator = "==" },
                new() { PropertyPath = "Number", SearchValue = "10", Operator = ">=" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(2);
    }

    // Ошибки

    [Fact]
    public void ApplyFilters_UnknownProperty_ReturnsErrorMessage()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "DoesNotExist", SearchValue = "x", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public void ApplyFilters_InvalidConversion_ReturnsErrorMessage()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Number", SearchValue = "not-a-number", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ApplyFilters_UnsupportedOperator_ReturnsErrorMessage()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "Number", SearchValue = "10", Operator = "~=" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ApplyFilters_WhitespacePropertyPath_SkipsFilter()
    {
        var parameters = new FilterParams
        {
            Page = 1,
            PageSize = 10,
            AdditionalFilters = new List<FilterCondition>
            {
                new() { PropertyPath = "   ", SearchValue = "10", Operator = "==" }
            }
        };

        var result = _sut.ApplyFilters(Data.AsQueryable(), parameters);

        result.TotalItems.Should().Be(5);
    }
}

// Тестовые сущности

internal class TestEntity : IEntity
{
    public int? Id { get; set; }
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = "";
    public int Number { get; set; }
    public decimal Price { get; set; }
    public TestEnum Status { get; set; }
    public NestedEntity? Nested { get; set; }
}

internal class NestedEntity
{
    public string Value { get; set; } = "";
}

internal enum TestEnum
{
    None = 0,
    First = 1,
    Second = 2
}
