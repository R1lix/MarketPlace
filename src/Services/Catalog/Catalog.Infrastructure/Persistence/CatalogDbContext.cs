using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

// Единица работы с базой Catalog. Картинки отдельного DbSet не имеют:
// они часть агрегата Product и загружаются/сохраняются только вместе с товаром.
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Подхватывает все IEntityTypeConfiguration<T> из этой сборки (появятся в блоке I4)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
