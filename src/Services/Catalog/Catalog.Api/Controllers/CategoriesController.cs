using Catalog.Application.Categories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Все категории")]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoryDto>> GetAll(CancellationToken cancellationToken) =>
        categories.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    [EndpointSummary("Категория")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CategoryDto> GetById(Guid id, CancellationToken cancellationToken) =>
        categories.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Создать категорию")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<CreatedAtRoute<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categories.CreateAsync(request, cancellationToken);
        return TypedResults.CreatedAtRoute(category, GetByIdRoute, new { id = category.Id });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Переименовать категорию")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> Rename(Guid id, RenameCategoryRequest request, CancellationToken cancellationToken)
    {
        await categories.RenameAsync(id, request, cancellationToken);
        return TypedResults.NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Удалить категорию")]
    [EndpointDescription("Категорию, в которой есть товары (в том числе удалённые), удалить нельзя — 409.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContent> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categories.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }

    private const string GetByIdRoute = "GetCategoryById";
}
