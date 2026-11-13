using Catalog.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace Catalog.IntegrationTests.Infrastructure;

// Один временный PostgreSQL на все интеграционные тесты:
// контейнер поднимается перед первым тестом, к нему применяются миграции, после последнего теста он удаляется.
public sealed class PostgresFixture : IAsyncLifetime
{
    // Та же версия, что в docker-compose.yml
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private ServiceProvider _services = null!;

    // «Замороженные» часы: тесты сами решают, который сейчас час
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = _container.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Time);   // регистрируется раньше — AddInfrastructure его не перезапишет
        services.AddInfrastructure(configuration);     // та же регистрация, что в настоящем приложении
        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    }

    // Каждый вызов — отдельный DbContext, как отдельный HTTP-запрос
    public AsyncServiceScope CreateScope() => _services.CreateAsyncScope();

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
