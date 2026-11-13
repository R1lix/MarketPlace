namespace Catalog.UnitTests;

public class ProductCreationTests
{
    [Fact]
    public void Constructor_ValidData_CreatesProduct()
    {
        var categoryId = Guid.NewGuid();

        var product = new Product("Телефон", "Описание", 100m, 5, categoryId, "AB-1");

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Телефон", product.Name);
        Assert.Equal("Описание", product.Description);
        Assert.Equal(100m, product.Price);
        Assert.Equal(5, product.Stock);
        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal("AB-1", product.Sku);
    }

    [Fact]
    public void Constructor_NewProduct_HasInitialState()
    {
        var product = TestData.Product();

        Assert.Equal(7, product.Id.Version);
        Assert.Equal(DateTimeKind.Utc, product.CreatedAt.Kind);
        Assert.Null(product.UpdatedAt);
        Assert.False(product.IsDeleted);
        Assert.Null(product.DeletedAt);
        Assert.Empty(product.Images);
    }

    [Fact]
    public void Constructor_NameWithSpaces_TrimsName()
    {
        var product = TestData.Product(name: "  Телефон  ");

        Assert.Equal("Телефон", product.Name);
    }

    [Fact]
    public void Constructor_Sku_TrimsAndConvertsToUpperCase()
    {
        var product = TestData.Product(sku: " ab-1 ");

        Assert.Equal("AB-1", product.Sku);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyDescription_StoresNull(string? description)
    {
        var product = TestData.Product(description: description);

        Assert.Null(product.Description);
    }

    [Fact]
    public void Constructor_ZeroPrice_IsAllowed()
    {
        var product = TestData.Product(price: 0m);

        Assert.Equal(0m, product.Price);
    }

    [Fact]
    public void Constructor_NameOfMaxLength_IsAllowed()
    {
        var name = new string('a', Product.MaxNameLength);

        var product = TestData.Product(name: name);

        Assert.Equal(name, product.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string name)
    {
        Assert.Throws<DomainException>(() => TestData.Product(name: name));
    }

    [Fact]
    public void Constructor_TooLongName_Throws()
    {
        var name = new string('a', Product.MaxNameLength + 1);

        Assert.Throws<DomainException>(() => TestData.Product(name: name));
    }

    [Fact]
    public void Constructor_TooLongDescription_Throws()
    {
        var description = new string('a', Product.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() => TestData.Product(description: description));
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        Assert.Throws<DomainException>(() => TestData.Product(price: -1m));
    }

    [Fact]
    public void Constructor_NegativeStock_Throws()
    {
        Assert.Throws<DomainException>(() => TestData.Product(stock: -1));
    }

    [Fact]
    public void Constructor_EmptyCategory_Throws()
    {
        Assert.Throws<DomainException>(() => TestData.Product(categoryId: Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptySku_Throws(string sku)
    {
        Assert.Throws<DomainException>(() => TestData.Product(sku: sku));
    }

    [Fact]
    public void Constructor_TooLongSku_Throws()
    {
        var sku = new string('a', Product.MaxSkuLength + 1);

        Assert.Throws<DomainException>(() => TestData.Product(sku: sku));
    }
}
