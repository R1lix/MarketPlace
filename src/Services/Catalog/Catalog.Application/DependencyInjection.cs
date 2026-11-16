using Catalog.Application.Categories;
using Catalog.Application.Products;
using Catalog.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

// Единая точка регистрации слоя Application: Program.cs вызывает AddApplication и AddInfrastructure
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped — как репозитории и DbContext, от которых сервисы зависят
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Все валидаторы FluentValidation из этой сборки (они internal — отсюда includeInternalTypes)
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IRequestValidator, RequestValidator>();

        return services;
    }
}
