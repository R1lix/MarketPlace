using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // DbContext регистрируется как Scoped: один экземпляр на HTTP-запрос
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));

        // Проверка «база доступна» для эндпоинта /health
        services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("catalog-db");

        return services;
    }
}
