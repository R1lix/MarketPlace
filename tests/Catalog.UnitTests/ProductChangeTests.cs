namespace Catalog.UnitTests;

public class ProductChangeTests
{
    [Fact]
    public void ChangeDetails_ValidData_ChangesNameAndDescription()
    {
        var product = TestData.Product();

        product.ChangeDetails("  Смартфон  ", "  Новое описание  ");

        Assert.Equal("Смартфон", product.Name);
        Assert.Equal("Новое описание", product.Description);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void ChangeDetails_EmptyName_ThrowsAndKeepsOldValues()
    {
        var product = TestData.Product(name: "Телефон");

        Assert.Throws<DomainException>(() => product.ChangeDetails("", "Описание"));

        Assert.Equal("Телефон", product.Name);
        Assert.Null(product.UpdatedAt);
    }

    [Fact]
    public void ChangePrice_ValidPrice_ChangesPrice()
    {
        var product = TestData.Product(price: 100m);

        product.ChangePrice(250m);

        Assert.Equal(250m, product.Price);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void ChangePrice_NegativePrice_ThrowsAndKeepsOldPrice()
    {
        var product = TestData.Product(price: 100m);

        Assert.Throws<DomainException>(() => product.ChangePrice(-1m));

        Assert.Equal(100m, product.Price);
    }

    [Fact]
    public void ChangeCategory_ValidId_ChangesCategory()
    {
        var product = TestData.Product();
        var newCategoryId = Guid.NewGuid();

        product.ChangeCategory(newCategoryId);

        Assert.Equal(newCategoryId, product.CategoryId);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void ChangeCategory_EmptyId_Throws()
    {
        var product = TestData.Product();

        Assert.Throws<DomainException>(() => product.ChangeCategory(Guid.Empty));
    }
}
