using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_images_product_id",
                table: "product_images");

            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "product_images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Добавлено вручную — миграция данных. У уже сохранённых картинок позиция 0, и уникальный индекс
            // ниже не создастся, если у товара их несколько. Нумеруем их 0, 1, 2... внутри каждого товара.
            // Настоящий порядок добавления неизвестен, берём порядок по Id — лучшее, что есть
            migrationBuilder.Sql("""
                UPDATE product_images AS pi
                SET position = numbered.rn
                FROM (
                    SELECT id, ROW_NUMBER() OVER (PARTITION BY product_id ORDER BY id) - 1 AS rn
                    FROM product_images
                ) AS numbered
                WHERE pi.id = numbered.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_position",
                table: "product_images",
                columns: new[] { "product_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_images_product_id_position",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "position",
                table: "product_images");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                table: "product_images",
                column: "product_id");
        }
    }
}
