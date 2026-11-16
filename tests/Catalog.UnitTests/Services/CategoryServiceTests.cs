using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Application.Exceptions;
using NSubstitute;

namespace Catalog.UnitTests.Services;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ICategoryQueries _queries = Substitute.For<ICategoryQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CategoryService _service;

    public CategoryServiceTests() => _service = new CategoryService(_categories, _queries, _unitOfWork, TestValidation.Validator);

    private Category Existing(string name = "Смартфоны")
    {
        var category = new Category(name);
        _categories.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        return category;
    }

    [Fact]
    public async Task Create_AddsSavesAndReturnsDto()
    {
        var dto = await _service.CreateAsync(new CreateCategoryRequest("  Смартфоны  "));

        Assert.Equal("Смартфоны", dto.Name);
        _categories.Received(1).Add(Arg.Is<Category>(c => c.Id == dto.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_EmptyName_ThrowsValidationAndNothingIsSaved()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(new CreateCategoryRequest(" ")));

        Assert.True(exception.Errors.ContainsKey("Name"));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Rename_Existing_RenamesAndSaves()
    {
        var category = Existing();

        await _service.RenameAsync(category.Id, new RenameCategoryRequest("Телефоны"));

        Assert.Equal("Телефоны", category.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rename_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RenameAsync(Guid.NewGuid(), new RenameCategoryRequest("Телефоны")));
    }

    [Fact]
    public async Task Delete_WithProducts_ThrowsConflictAndRemovesNothing()
    {
        var category = Existing();
        _categories.HasProductsAsync(category.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(category.Id));

        _categories.DidNotReceive().Remove(Arg.Any<Category>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithoutProducts_RemovesAndSaves()
    {
        var category = Existing();

        await _service.DeleteAsync(category.Id);

        _categories.Received(1).Remove(category);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }
}
