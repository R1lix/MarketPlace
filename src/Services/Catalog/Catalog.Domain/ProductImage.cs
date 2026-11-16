namespace Catalog.Domain;

// Картинка товара. Живёт только внутри агрегата Product: создать её может только Product (конструктор internal).
public sealed class ProductImage
{
    public const int MaxNameImageLength = 200;
    public const int MaxPathImageLength = 500;

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string PathImage { get; private set; } = null!;
    public string NameImage { get; private set; } = null!;

    // Порядок показа картинки у товара: 0 — первая. Назначает Product.
    // Порядок по Id не годится: Guid v7 упорядочены только с точностью до миллисекунды,
    // а картинки, добавленные в одну миллисекунду, по Id идут в случайном порядке
    public int Position { get; private set; }

    // Для EF Core
    private ProductImage() { }

    internal ProductImage(Guid productId, string nameImage, string pathImage, int position)
    {
        if (position < 0)
            throw new DomainException("Позиция картинки не может быть отрицательной");
        Id = Guid.CreateVersion7();
        ProductId = productId;
        Position = position;
        SetPath(pathImage);
        SetName(nameImage);
    }

    private void SetPath(string pathImage)
    {
        if (string.IsNullOrWhiteSpace(pathImage))
            throw new DomainException("Путь к изображению не может быть пустым");
        pathImage = pathImage.Trim();
        if (pathImage.Length > MaxPathImageLength)
            throw new DomainException($"Путь к изображению не может быть длиннее {MaxPathImageLength} символов");
        PathImage = pathImage;
    }

    private void SetName(string nameImage)
    {
        if (string.IsNullOrWhiteSpace(nameImage))
            throw new DomainException("Имя изображения не может быть пустым");
        nameImage = nameImage.Trim();
        if (nameImage.Length > MaxNameImageLength)
            throw new DomainException($"Имя изображения не может быть длиннее {MaxNameImageLength} символов");
        NameImage = nameImage;
    }
}
