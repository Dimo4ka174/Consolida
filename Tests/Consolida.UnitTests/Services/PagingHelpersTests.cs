using Application.DataAccessLayer.Service.Common;
using FluentAssertions;

namespace Consolida.UnitTests.Services;

public class PagingHelpersTests
{
    [Fact]
    public void Create_BasicCase_ComputesTotals()
    {
        var paging = PagingHelpers.Create(totalItems: 100, page: 1, pageSize: 10);

        paging.TotalItems.Should().Be(100);
        paging.CurrentPage.Should().Be(1);
        paging.PageSize.Should().Be(10);
        paging.TotalPages.Should().Be(10);
    }

    [Fact]
    public void Create_PageLessThanOne_ClampsToOne()
    {
        var paging = PagingHelpers.Create(100, page: 0, pageSize: 10);

        paging.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void Create_NegativePage_ClampsToOne()
    {
        var paging = PagingHelpers.Create(100, page: -5, pageSize: 10);

        paging.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void Create_PageSizeLessThanOne_ClampsToOne()
    {
        var paging = PagingHelpers.Create(100, page: 1, pageSize: 0);

        paging.PageSize.Should().Be(1);
    }

    [Fact]
    public void Create_PageBeyondTotal_ClampsToTotalPages()
    {
        var paging = PagingHelpers.Create(50, page: 100, pageSize: 10);

        paging.CurrentPage.Should().Be(5);
        paging.TotalPages.Should().Be(5);
    }

    [Fact]
    public void Create_ZeroItems_TotalPagesZero()
    {
        var paging = PagingHelpers.Create(0, 1, 10);

        paging.TotalPages.Should().Be(0);
    }

    [Fact]
    public void Create_TotalLessThanPageSize_OnePage()
    {
        var paging = PagingHelpers.Create(3, 1, 10);

        paging.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Create_FewPages_StartEndWithinBounds()
    {
        // 30 items, pageSize 10 → 3 страницы всего.
        // По умолчанию окон ±5 — но их больше, чем страниц. Значит: [1..3]
        var paging = PagingHelpers.Create(30, 2, 10);

        paging.StartPage.Should().Be(1);
        paging.EndPage.Should().Be(3);
    }

    [Fact]
    public void Create_ManyPages_StartEndAroundCurrent()
    {
        // 100 items, pageSize 10 → 10 страниц. Текущая 5, окно ±2 → [3..7]
        var paging = PagingHelpers.Create(100, 5, 10, pagesAroundCurrent: 2);

        paging.StartPage.Should().Be(3);
        paging.EndPage.Should().Be(7);
    }

    [Fact]
    public void Create_NearLastPage_WindowShiftsToFitAtEnd()
    {
        // 100 items, pageSize 10 → 10 страниц. Текущая 10, окно ±2.
        // Окно «сжимается» к концу, сохраняя ширину 5 страниц: [6..10].
        var paging = PagingHelpers.Create(100, 10, 10, pagesAroundCurrent: 2);

        paging.StartPage.Should().Be(6);
        paging.EndPage.Should().Be(10);
    }

    [Fact]
    public void Create_MiddlePages_WindowHasFullWidth()
    {
        // Окно ровно 2*pagesAroundCurrent + 1 страниц в середине диапазона.
        var paging = PagingHelpers.Create(100, 5, 10, pagesAroundCurrent: 2);

        (paging.EndPage - paging.StartPage + 1).Should().Be(5);
    }

    [Fact]
    public void Create_NearFirstPage_StartClampsAndEndShifts()
    {
        // 100 items, pageSize 10 → 10 страниц. Текущая 1, окно ±2.
        // Start упирается в 1, End пересчитывается.
        var paging = PagingHelpers.Create(100, 1, 10, pagesAroundCurrent: 2);

        paging.StartPage.Should().Be(1);
        paging.EndPage.Should().Be(5);
    }

    [Fact]
    public void Create_TotalPagesLessThanWindow_FullRange()
    {
        // 30 items, pageSize 10 → 3 страницы. Окно ±5 больше, чем есть.
        var paging = PagingHelpers.Create(30, 2, 10, pagesAroundCurrent: 5);

        paging.StartPage.Should().Be(1);
        paging.EndPage.Should().Be(3);
    }

    [Fact]
    public void Create_NamePage_CanBeSet()
    {
        var paging = PagingHelpers.Create(10, 1, 10);

        paging.NamePage = "Test";

        paging.NamePage.Should().Be("Test");
    }
}
