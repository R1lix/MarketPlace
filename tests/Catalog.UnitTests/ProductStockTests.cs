namespace Catalog.UnitTests;

public class ProductStockTests
{
    [Fact]
    public void AddStock_PositiveQuantity_IncreasesStock()
    {
        var product = TestData.Product(stock: 10);

        product.AddStock(5);

        Assert.Equal(15, product.Stock);
        Assert.NotNull(product.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddStock_NotPositiveQuantity_Throws(int quantity)
    {
        var product = TestData.Product(stock: 10);

        Assert.Throws<DomainException>(() => product.AddStock(quantity));

        Assert.Equal(10, product.Stock);
    }

    [Fact]
    public void RemoveStock_LessThanStock_DecreasesStock()
    {
        var product = TestData.Product(stock: 10);

        product.RemoveStock(3);

        Assert.Equal(7, product.Stock);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void RemoveStock_WholeStock_LeavesZero()
    {
        var product = TestData.Product(stock: 10);

        product.RemoveStock(10);

        Assert.Equal(0, product.Stock);
    }

    [Fact]
    public void RemoveStock_MoreThanStock_ThrowsAndKeepsStock()
    {
        var product = TestData.Product(stock: 10);

        var exception = Assert.Throws<DomainException>(() => product.RemoveStock(11));

        Assert.Contains("Недостаточно товара", exception.Message);
        Assert.Equal(10, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RemoveStock_NotPositiveQuantity_Throws(int quantity)
    {
        var product = TestData.Product(stock: 10);

        Assert.Throws<DomainException>(() => product.RemoveStock(quantity));

        Assert.Equal(10, product.Stock);
    }
}
