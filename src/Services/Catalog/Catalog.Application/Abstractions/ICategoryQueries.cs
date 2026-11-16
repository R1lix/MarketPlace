using Catalog.Application.Categories;

namespace Catalog.Application.Abstractions;

public interface ICategoryQueries
{
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Категорий немного, поэтому список без пагинации, по алфавиту
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
