namespace Catalog.UnitTests;

public class CategoryTests
{
    [Fact]
    public void Constructor_ValidName_CreatesCategory()
    {
        var category = new Category("  Смартфоны  ");

        Assert.Equal("Смартфоны", category.Name);
        Assert.Equal(7, category.Id.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string name)
    {
        Assert.Throws<DomainException>(() => new Category(name));
    }

    [Fact]
    public void Rename_ValidName_ChangesName()
    {
        var category = new Category("Смартфоны");

        category.Rename("Телефоны");

        Assert.Equal("Телефоны", category.Name);
    }

    [Fact]
    public void Rename_EmptyName_ThrowsAndKeepsOldName()
    {
        var category = new Category("Смартфоны");

        Assert.Throws<DomainException>(() => category.Rename("   "));

        Assert.Equal("Смартфоны", category.Name);
    }

    [Fact]
    public void Rename_NameOfMaxLength_IsAllowed()
    {
        var category = new Category("Смартфоны");
        var name = new string('a', Category.MaxNameLength);

        category.Rename(name);

        Assert.Equal(name, category.Name);
    }

    [Fact]
    public void Rename_TooLongName_Throws()
    {
        var category = new Category("Смартфоны");

        Assert.Throws<DomainException>(() => category.Rename(new string('a', Category.MaxNameLength + 1)));
    }
}
