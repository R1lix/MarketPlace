using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);
        // Без этого картинка, добавленная через product.AddImage к уже сохранённому товару,
        // была бы принята за существующую строку, и SaveChanges упал бы на UPDATE
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.NameImage).HasMaxLength(ProductImage.MaxNameImageLength).IsRequired();
        builder.Property(i => i.PathImage).HasMaxLength(ProductImage.MaxPathImageLength).IsRequired();

        // Связь с Product (внешний ключ, каскадное удаление) настроена в ProductConfiguration
    }
}
