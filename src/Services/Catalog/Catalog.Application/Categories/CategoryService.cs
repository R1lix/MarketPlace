using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Catalog.Domain;

namespace Catalog.Application.Categories;

internal sealed class CategoryService(
    ICategoryRepository categories,
    ICategoryQueries queries,
    IUnitOfWork unitOfWork) : ICategoryService
{
    private const string CategoryEntity = "Категория";

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category(request.Name);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name);
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await queries.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(CategoryEntity, id);

    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        queries.GetAllAsync(cancellationToken);

    public async Task RenameAsync(Guid id, RenameCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryAsync(id, cancellationToken);
        category.Rename(request.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryAsync(id, cancellationToken);

        // Понятная ошибка заранее. Если товар добавят между проверкой и сохранением,
        // база отклонит удаление (RESTRICT), и UnitOfWork тоже вернёт ConflictException
        if (await categories.HasProductsAsync(id, cancellationToken))
            throw new ConflictException("Нельзя удалить категорию, в которой есть товары (в том числе удалённые)");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        await categories.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(CategoryEntity, id);
}
