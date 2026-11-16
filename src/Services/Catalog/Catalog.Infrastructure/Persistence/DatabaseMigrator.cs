using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Persistence;

// Применение миграций при старте приложения. По умолчанию выключено: в разработке миграции
// применяются вручную (dotnet ef database update), чтобы было видно, когда меняется схема.
// Включается настройкой Database:MigrateOnStartup — например, в docker-compose, где API и база
// поднимаются вместе и запускать dotnet ef отдельно неудобно
public static class DatabaseMigrator
{
    public const string MigrateOnStartupSetting = "Database:MigrateOnStartup";

    public static async Task MigrateCatalogDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
