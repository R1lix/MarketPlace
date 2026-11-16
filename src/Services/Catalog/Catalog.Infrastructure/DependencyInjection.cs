using Catalog.Application.Abstractions;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Interceptors;
using Catalog.Infrastructure.Persistence.Queries;
using Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalog.Infrastructure;

// Единая точка регистрации инфраструктуры: Program.cs вызывает только AddInfrastructure
public static class DependencyInjection
{
    public const string ConnectionStringName = "CatalogDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Источник текущего времени. TryAdd — чтобы тесты могли подменить его своим «замороженным» временем
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<UpdatedAtInterceptor>();

        // DbContext регистрируется как Scoped: один экземпляр на HTTP-запрос.
        // snake_case — принятый в PostgreSQL стиль имён: products, product_images, is_deleted.
        // Строка подключения читается при создании DbContext, а не здесь: к этому моменту конфигурация
        // окончательная, в том числе подменённая в тестах API (WebApplicationFactory)
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) => options
            .UseNpgsql(GetConnectionString(serviceProvider.GetService<IConfiguration>() ?? configuration))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(serviceProvider.GetRequiredService<UpdatedAtInterceptor>()));

        // Репозитории и Unit of Work — Scoped, как и DbContext: внутри одного запроса все они
        // работают с одним и тем же DbContext, поэтому SaveChangesAsync сохраняет изменения всех репозиториев разом
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Чтение для отображения — отдельно от репозиториев (AsNoTracking + проекция в DTO)
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();

        // Проверка «база доступна» для эндпоинта /health
        services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("catalog-db");

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException(
            $"Не задана строка подключения '{ConnectionStringName}' (ConnectionStrings:{ConnectionStringName})");
}
