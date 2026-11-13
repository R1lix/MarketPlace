namespace Catalog.UnitTests;

public class ProductDeleteTests
{
    [Fact]
    public void Delete_ProductWithoutStock_MarksAsDeleted()
    {
        var product = TestData.Product(stock: 0);

        product.Delete();

        Assert.True(product.IsDeleted);
        Assert.NotNull(product.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, product.DeletedAt!.Value.Kind);
    }

    [Fact]
    public void Delete_ProductWithStock_ThrowsAndKeepsProduct()
    {
        var product = TestData.Product(stock: 3);

        Assert.Throws<DomainException>(() => product.Delete());

        Assert.False(product.IsDeleted);
        Assert.Null(product.DeletedAt);
    }

    [Fact]
    public void Delete_AfterRemovingAllStock_Succeeds()
    {
        var product = TestData.Product(stock: 3);

        product.RemoveStock(3);
        product.Delete();

        Assert.True(product.IsDeleted);
    }

    [Fact]
    public void Delete_CalledTwice_DoesNothingSecondTime()
    {
        var product = TestData.DeletedProduct();
        var deletedAt = product.DeletedAt;

        product.Delete();

        Assert.True(product.IsDeleted);
        Assert.Equal(deletedAt, product.DeletedAt);
    }

    // Все изменяющие методы удалённого товара должны бросать DomainException
    public static TheoryData<string, Action<Product>> ChangesOfDeletedProduct => new()
    {
        { nameof(Product.ChangeDetails), p => p.ChangeDetails("Новое имя", null) },
        { nameof(Product.ChangePrice), p => p.ChangePrice(1m) },
        { nameof(Product.ChangeCategory), p => p.ChangeCategory(Guid.NewGuid()) },
        { nameof(Product.AddStock), p => p.AddStock(1) },
        { nameof(Product.RemoveStock), p => p.RemoveStock(1) },
        { nameof(Product.AddImage), p => p.AddImage("a.png", "/images/a.png") },
        { nameof(Product.RemoveImage), p => p.RemoveImage(Guid.NewGuid()) },
    };

    [Theory]
    [MemberData(nameof(ChangesOfDeletedProduct))]
    public void AnyChange_DeletedProduct_Throws(string method, Action<Product> change)
    {
        var product = TestData.DeletedProduct();

        var exception = Assert.Throws<DomainException>(() => change(product));

        Assert.True(exception.Message.Contains("удалён"), $"{method}: ожидалась ошибка про удалённый товар");
    }
}
