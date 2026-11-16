using Catalog.IntegrationTests.Infrastructure;

namespace Catalog.IntegrationTests;

public class MigrationsTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task AllMigrations_AreApplied()
    {
        var pending = await InDb(db => db.Database.GetPendingMigrationsAsync());

        Assert.Empty(pending);
    }

    // Ловит забытую миграцию: если поменяли конфигурацию или сущность, но не выполнили
    // dotnet ef migrations add, модель разойдётся со снимком, и тест упадёт (в том числе в CI)
    [Fact]
    public async Task Model_HasNoChangesWithoutMigration()
    {
        var hasChanges = await InDb(db => Task.FromResult(db.Database.HasPendingModelChanges()));

        Assert.False(hasChanges, "Модель изменилась, но миграция не создана: выполните dotnet ef migrations add");
    }
}
