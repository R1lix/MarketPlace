using Catalog.Application.Common;

namespace Catalog.UnitTests;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 10, 10)]
    public void TotalPages_IsRoundedUp(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>([], 1, pageSize, totalCount);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void Navigation_FirstMiddleLastPage()
    {
        PagedResult<int> Page(int page) => new([], page, PageSize: 10, TotalCount: 25);

        Assert.False(Page(1).HasPreviousPage);
        Assert.True(Page(1).HasNextPage);
        Assert.True(Page(2).HasPreviousPage);
        Assert.True(Page(2).HasNextPage);
        Assert.False(Page(3).HasNextPage);
    }
}
