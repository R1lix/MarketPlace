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

    // Для EF Core
    private ProductImage() { }

    internal ProductImage(Guid productId, string nameImage, string pathImage)
    {
        Id = Guid.CreateVersion7();
        ProductId = productId;
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
