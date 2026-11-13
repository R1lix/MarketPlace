using System.Reflection;

namespace Catalog.UnitTests;

// Архитектурные проверки: сущности нельзя изменить в обход бизнес-методов.
// Если кто-то добавит публичный сеттер или конструктор, тесты упадут.
public class DomainEncapsulationTests
{
    public static TheoryData<Type> Entities => new()
    {
        typeof(Product),
        typeof(ProductImage),
        typeof(Category),
    };

    [Theory]
    [MemberData(nameof(Entities))]
    public void Entity_HasNoPublicSetters(Type entity)
    {
        var publicSetters = entity.GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => p.Name);

        Assert.Empty(publicSetters);
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public void Entity_HasPrivateParameterlessConstructorForEfCore(Type entity)
    {
        Assert.Null(entity.GetConstructor(Type.EmptyTypes));
        Assert.NotNull(entity.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes));
    }

    [Theory]
    [MemberData(nameof(Entities))]
    public void Entity_IsSealed(Type entity)
    {
        Assert.True(entity.IsSealed);
    }

    [Fact]
    public void ProductImage_HasNoPublicConstructors()
    {
        Assert.Empty(typeof(ProductImage).GetConstructors());
    }

    [Fact]
    public void Product_ImagesCannotBeModifiedFromOutside()
    {
        var product = TestData.ProductWithImages(1);

        // Даже приведение к изменяемому интерфейсу не должно позволить добавить картинку в обход AddImage
        Assert.False(product.Images is List<ProductImage>);
        Assert.False(product.Images is IList<ProductImage> { IsReadOnly: false });
        Assert.Throws<NotSupportedException>(() => ((ICollection<ProductImage>)product.Images).Clear());
    }
}
