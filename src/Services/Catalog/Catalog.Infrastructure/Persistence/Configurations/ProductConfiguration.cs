using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        // Id генерирует сам домен (Guid v7). ValueGeneratedNever важен: иначе EF посчитал бы
        // новую сущность с заполненным Id уже существующей и попытался сделать UPDATE вместо INSERT
        builder.Property(p => p.Id).ValueGeneratedNever();

        // Длины берутся из констант домена — одно число в одном месте
        builder.Property(p => p.Name).HasMaxLength(Product.MaxNameLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.MaxDescriptionLength);
        builder.Property(p => p.Sku).HasMaxLength(Product.MaxSkuLength).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, Product.PriceDecimalPlaces);

        // Артикул уникален только среди неудалённых товаров: удалённый товар не «занимает» артикул навсегда
        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("is_deleted = false");

        // Ссылка на категорию только по Id (отдельный агрегат). Restrict: категорию с товарами удалить нельзя
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Картинки — часть агрегата: EF работает напрямую с полем _images, удаляются вместе с товаром
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Мягкое удаление: удалённые товары автоматически исключаются из всех запросов.
        // Для админки фильтр можно отключить через IgnoreQueryFilters()
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
