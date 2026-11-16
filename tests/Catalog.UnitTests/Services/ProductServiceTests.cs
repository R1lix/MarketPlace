using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Catalog.Application.Products;
using NSubstitute;

namespace Catalog.UnitTests.Services;

// Сценарии ProductService без базы: репозитории, запросы и Unit of Work подменены заглушками (NSubstitute).
// Проверяем оркестрацию: что загружено, какой метод домена вызван, сохранено ли — и что НЕ сохранено при ошибке
public class ProductServiceTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IProductQueries _queries = Substitute.For<IProductQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProductService _service;

    private readonly Category _category = new("Смартфоны");

    public ProductServiceTests()
    {
        _service = new ProductService(_products, _categories, _queries, _unitOfWork);
        _categories.GetByIdAsync(_category.Id, Arg.Any<CancellationToken>()).Returns(_category);
        _categories.ExistsAsync(_category.Id, Arg.Any<CancellationToken>()).Returns(true);
    }

    private CreateProductRequest ValidCreate(string sku = "AB-1") =>
        new("Телефон", "Описание", 100m, 5, _category.Id, sku);

    // Товар уже «в базе»: репозиторий его возвращает
    private Product ExistingProduct(int stock = 5, int images = 0)
    {
        var product = TestData.Product(stock: stock, categoryId: _category.Id);
        for (var i = 0; i < images; i++)
            product.AddImage($"img{i}.png", $"/images/{i}.png");
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    private Task AssertNotSaved() => _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

    // ----- Create -----

    [Fact]
    public async Task Create_ValidRequest_AddsSavesAndReturnsDto()
    {
        var dto = await _service.CreateAsync(ValidCreate());

        _products.Received(1).Add(Arg.Is<Product>(p => p.Id == dto.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Телефон", dto.Name);
        Assert.Equal(_category.Name, dto.CategoryName);
        Assert.Equal(5, dto.Stock);
    }

    [Fact]
    public async Task Create_UnknownCategory_ThrowsValidationAndSavesNothing()
    {
        var request = ValidCreate() with { CategoryId = Guid.NewGuid() };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.True(exception.Errors.ContainsKey(nameof(CreateProductRequest.CategoryId)));
        _products.DidNotReceive().Add(Arg.Any<Product>());
        await AssertNotSaved();
    }

    // Уникальность проверяется по нормализованному артикулу: « ab-1 » и «AB-1» — один артикул
    [Fact]
    public async Task Create_DuplicateSku_ChecksNormalizedSkuAndThrowsConflict()
    {
        _products.SkuExistsAsync("AB-1", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(ValidCreate(sku: " ab-1 ")));

        _products.DidNotReceive().Add(Arg.Any<Product>());
        await AssertNotSaved();
    }

    [Fact]
    public async Task Create_InvalidData_DomainThrowsAndNothingIsSaved()
    {
        var request = ValidCreate() with { Price = -1m };

        await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(request));

        await AssertNotSaved();
    }

    // ----- Get -----

    [Fact]
    public async Task GetById_Unknown_ThrowsNotFound()
    {
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));

        Assert.Equal(id, exception.Id);
    }

    [Fact]
    public async Task GetPage_DelegatesToQueries()
    {
        var query = new ProductListQuery(Page: 2, CategoryId: _category.Id);

        await _service.GetPageAsync(query);

        await _queries.Received(1).GetPageAsync(query, Arg.Any<CancellationToken>());
    }

    // ----- Update -----

    [Fact]
    public async Task Update_ValidRequest_ChangesProductAndSaves()
    {
        var product = ExistingProduct();
        var newCategory = Guid.NewGuid();
        _categories.ExistsAsync(newCategory, Arg.Any<CancellationToken>()).Returns(true);

        await _service.UpdateAsync(product.Id, new UpdateProductRequest("Смартфон", "Новое", 250m, newCategory));

        Assert.Equal("Смартфон", product.Name);
        Assert.Equal(250m, product.Price);
        Assert.Equal(newCategory, product.CategoryId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_SameCategory_DoesNotCheckCategoryExistence()
    {
        var product = ExistingProduct();

        await _service.UpdateAsync(product.Id, new UpdateProductRequest("Смартфон", null, 1m, product.CategoryId));

        await _categories.DidNotReceive().ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownNewCategory_ThrowsValidationAndKeepsProduct()
    {
        var product = ExistingProduct();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(product.Id, new UpdateProductRequest("Смартфон", null, 1m, Guid.NewGuid())));

        Assert.Equal("Телефон", product.Name);
        await AssertNotSaved();
    }

    [Fact]
    public async Task Update_UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new UpdateProductRequest("Х", null, 1m, _category.Id)));

        await AssertNotSaved();
    }

    // ----- Stock -----

    [Fact]
    public async Task AddStock_IncreasesStockAndSaves()
    {
        var product = ExistingProduct(stock: 5);

        await _service.AddStockAsync(product.Id, new ChangeStockRequest(3));

        Assert.Equal(8, product.Stock);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveStock_MoreThanAvailable_DomainThrowsAndNothingIsSaved()
    {
        var product = ExistingProduct(stock: 2);

        await Assert.ThrowsAsync<DomainException>(() => _service.RemoveStockAsync(product.Id, new ChangeStockRequest(5)));

        Assert.Equal(2, product.Stock);
        await AssertNotSaved();
    }

    [Fact]
    public async Task RemoveStock_UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveStockAsync(Guid.NewGuid(), new ChangeStockRequest(1)));
    }

    // ----- Images -----

    [Fact]
    public async Task AddImage_AddsSavesAndReturnsImageDto()
    {
        var product = ExistingProduct(images: 1);

        var dto = await _service.AddImageAsync(product.Id, new AddProductImageRequest("photo.png", "/images/photo.png"));

        Assert.Equal(2, product.Images.Count);
        Assert.Equal("photo.png", dto.Name);
        Assert.Contains(product.Images, i => i.Id == dto.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveImage_Existing_RemovesAndSaves()
    {
        var product = ExistingProduct(images: 2);
        var imageId = product.Images.First().Id;

        await _service.RemoveImageAsync(product.Id, imageId);

        Assert.DoesNotContain(product.Images, i => i.Id == imageId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Id картинки приходит в адресе запроса — её отсутствие это 404, а не ошибка бизнес-правила (400)
    [Fact]
    public async Task RemoveImage_UnknownImage_ThrowsNotFound()
    {
        var product = ExistingProduct(images: 1);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveImageAsync(product.Id, Guid.NewGuid()));

        Assert.Single(product.Images);
        await AssertNotSaved();
    }

    // ----- Delete -----

    [Fact]
    public async Task Delete_ProductWithoutStock_MarksDeletedAndSaves()
    {
        var product = ExistingProduct(stock: 0);

        await _service.DeleteAsync(product.Id);

        Assert.True(product.IsDeleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ProductWithStock_DomainThrowsAndNothingIsSaved()
    {
        var product = ExistingProduct(stock: 3);

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(product.Id));

        Assert.False(product.IsDeleted);
        await AssertNotSaved();
    }

    [Fact]
    public async Task Delete_UnknownProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    // CancellationToken должен доходить до репозиториев и сохранения — иначе отмена запроса не остановит работу с базой
    [Fact]
    public async Task CancellationToken_IsPassedThrough()
    {
        var product = ExistingProduct(stock: 1);
        using var cts = new CancellationTokenSource();

        await _service.AddStockAsync(product.Id, new ChangeStockRequest(1), cts.Token);

        await _products.Received(1).GetByIdAsync(product.Id, cts.Token);
        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }
}
