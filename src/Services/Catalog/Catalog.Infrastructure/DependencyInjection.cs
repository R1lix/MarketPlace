using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence.Interceptors;
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
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Не задана строка подключения '{ConnectionStringName}' (ConnectionStrings:{ConnectionStringName})");

        // Источник текущего времени. TryAdd — чтобы тесты могли подменить его своим «замороженным» временем
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<UpdatedAtInterceptor>();

        // DbContext регистрируется как Scoped: один экземпляр на HTTP-запрос.
        // snake_case — принятый в PostgreSQL стиль имён: products, product_images, is_deleted
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(serviceProvider.GetRequiredService<UpdatedAtInterceptor>()));

        // Проверка «база доступна» для эндпоинта /health
        services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("catalog-db");

        return services;
    }
}
