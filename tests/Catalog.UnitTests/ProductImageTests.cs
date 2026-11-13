namespace Catalog.UnitTests;

public class ProductImageTests
{
    [Fact]
    public void AddImage_ValidData_AddsImageToProduct()
    {
        var product = TestData.Product();

        var image = product.AddImage("  photo.png  ", "  /images/photo.png  ");

        Assert.Single(product.Images);
        Assert.Same(image, product.Images.Single());
        Assert.Equal(product.Id, image.ProductId);
        Assert.Equal("photo.png", image.NameImage);
        Assert.Equal("/images/photo.png", image.PathImage);
        Assert.Equal(7, image.Id.Version);
        Assert.NotNull(product.UpdatedAt);
    }

    [Fact]
    public void AddImage_UpToMaxImages_IsAllowed()
    {
        var product = TestData.ProductWithImages(Product.MaxImages);

        Assert.Equal(Product.MaxImages, product.Images.Count);
    }

    [Fact]
    public void AddImage_OverMaxImages_ThrowsAndKeepsCount()
    {
        var product = TestData.ProductWithImages(Product.MaxImages);

        Assert.Throws<DomainException>(() => product.AddImage("extra.png", "/images/extra.png"));

        Assert.Equal(Product.MaxImages, product.Images.Count);
    }

    [Theory]
    [InlineData("", "/images/a.png")]
    [InlineData("   ", "/images/a.png")]
    [InlineData("a.png", "")]
    [InlineData("a.png", "   ")]
    public void AddImage_EmptyNameOrPath_Throws(string name, string path)
    {
        var product = TestData.Product();

        Assert.Throws<DomainException>(() => product.AddImage(name, path));

        Assert.Empty(product.Images);
    }

    [Fact]
    public void AddImage_TooLongPath_Throws()
    {
        var product = TestData.Product();
        var path = new string('x', ProductImage.MaxPathImageLength + 1);

        Assert.Throws<DomainException>(() => product.AddImage("a.png", path));
    }

    [Fact]
    public void AddImage_TooLongName_Throws()
    {
        var product = TestData.Product();
        var name = new string('x', ProductImage.MaxNameImageLength + 1);

        Assert.Throws<DomainException>(() => product.AddImage(name, "/images/a.png"));
    }

    [Fact]
    public void RemoveImage_ExistingImage_RemovesIt()
    {
        var product = TestData.ProductWithImages(3);
        var image = product.Images.First();

        product.RemoveImage(image.Id);

        Assert.Equal(2, product.Images.Count);
        Assert.DoesNotContain(product.Images, i => i.Id == image.Id);
    }

    [Fact]
    public void RemoveImage_UnknownId_ThrowsAndKeepsImages()
    {
        var product = TestData.ProductWithImages(3);

        Assert.Throws<DomainException>(() => product.RemoveImage(Guid.NewGuid()));

        Assert.Equal(3, product.Images.Count);
    }
}
