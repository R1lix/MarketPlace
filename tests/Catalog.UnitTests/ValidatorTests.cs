using Catalog.Application.Categories;
using Catalog.Application.Exceptions;
using Catalog.Application.Products;
using Catalog.UnitTests.Services;

namespace Catalog.UnitTests;

// Правила валидаторов через IRequestValidator — так же, как их вызывают сервисы
public class ValidatorTests
{
    private static async Task<IReadOnlyDictionary<string, string[]>> Errors<T>(T request)
    {
        try
        {
            await TestValidation.Validator.ValidateAsync(request);
            return new Dictionary<string, string[]>();
        }
        catch (ValidationException exception)
        {
            return exception.Errors;
        }
    }

    private static CreateProductRequest ValidProduct() => new("Телефон", "Описание", 99.90m, 5, Guid.NewGuid(), "AB-1");

    [Fact]
    public async Task CreateProduct_Valid_HasNoErrors()
    {
        Assert.Empty(await Errors(ValidProduct()));
    }

    [Fact]
    public async Task CreateProduct_EverythingWrong_ReportsEachFieldOnce()
    {
        var errors = await Errors(new CreateProductRequest(" ", new string('a', Product.MaxDescriptionLength + 1), 1.001m, -1, Guid.Empty, ""));

        Assert.Equal(["CategoryId", "Description", "Name", "Price", "Sku", "Stock"], errors.Keys.Order());
    }

    // Длина считается после Trim, как в домене
    [Fact]
    public async Task CreateProduct_NameWithSpacesAroundMaxLength_IsValid()
    {
        var name = "  " + new string('a', Product.MaxNameLength) + "  ";

        Assert.Empty(await Errors(ValidProduct() with { Name = name }));
    }

    [Fact]
    public async Task CreateProduct_TooLongName_IsInvalid()
    {
        var errors = await Errors(ValidProduct() with { Name = new string('a', Product.MaxNameLength + 1) });

        Assert.Equal(["Name"], errors.Keys);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("99.99")]
    [InlineData("100.5")]
    public async Task CreateProduct_ValidPrices(string price)
    {
        var request = ValidProduct() with { Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) };

        Assert.Empty(await Errors(request));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("99.999")]
    public async Task CreateProduct_InvalidPrices(string price)
    {
        var request = ValidProduct() with { Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture) };

        Assert.Equal(["Price"], (await Errors(request)).Keys);
    }

    [Fact]
    public async Task UpdateProduct_RequiresNameAndCategory()
    {
        var errors = await Errors(new UpdateProductRequest("", null, 1m, Guid.Empty));

        Assert.Equal(["CategoryId", "Name"], errors.Keys.Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ChangeStock_NotPositive_IsInvalid(int quantity)
    {
        Assert.Equal(["Quantity"], (await Errors(new ChangeStockRequest(quantity))).Keys);
    }

    [Fact]
    public async Task AddImage_EmptyAndTooLong_AreInvalid()
    {
        var errors = await Errors(new AddProductImageRequest("", new string('x', ProductImage.MaxPathImageLength + 1)));

        Assert.Equal(["Name", "Path"], errors.Keys.Order());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, ProductListQuery.MaxPageSize)]
    public async Task ProductList_ValidPaging(int page, int pageSize)
    {
        Assert.Empty(await Errors(new ProductListQuery(page, pageSize)));
    }

    [Theory]
    [InlineData(0, 20, "Page")]
    [InlineData(1, 0, "PageSize")]
    [InlineData(1, ProductListQuery.MaxPageSize + 1, "PageSize")]
    public async Task ProductList_InvalidPaging(int page, int pageSize, string field)
    {
        Assert.Equal([field], (await Errors(new ProductListQuery(page, pageSize))).Keys);
    }

    [Fact]
    public async Task ProductList_TooLongSearch_IsInvalid()
    {
        var query = new ProductListQuery(Search: new string('a', ProductListQuery.MaxSearchLength + 1));

        Assert.Equal(["Search"], (await Errors(query)).Keys);
    }

    [Fact]
    public async Task Category_NameRules()
    {
        Assert.Empty(await Errors(new CreateCategoryRequest("Смартфоны")));
        Assert.Equal(["Name"], (await Errors(new CreateCategoryRequest(" "))).Keys);
        Assert.Equal(["Name"], (await Errors(new RenameCategoryRequest(new string('a', Category.MaxNameLength + 1)))).Keys);
    }
}
