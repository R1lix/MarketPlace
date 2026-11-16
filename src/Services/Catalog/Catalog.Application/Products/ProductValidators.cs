using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;

namespace Catalog.Application.Products;

// Формат запросов. Лимиты — из констант домена: одно число в одном месте.
// Домен проверяет те же правила ещё раз (последняя линия защиты), но валидатор
// возвращает все ошибки сразу и по полям, а домен — первую попавшуюся

internal sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.Name).RequiredText(Product.MaxNameLength);
        RuleFor(r => r.Description).OptionalText(Product.MaxDescriptionLength);
        RuleFor(r => r.Price).ValidPrice();
        RuleFor(r => r.Stock).GreaterThanOrEqualTo(0).WithMessage("Остаток не может быть отрицательным");
        RuleFor(r => r.CategoryId).NotEmpty().WithMessage("Категория обязательна");
        RuleFor(r => r.Sku).RequiredText(Product.MaxSkuLength);
    }
}

internal sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(r => r.Name).RequiredText(Product.MaxNameLength);
        RuleFor(r => r.Description).OptionalText(Product.MaxDescriptionLength);
        RuleFor(r => r.Price).ValidPrice();
        RuleFor(r => r.CategoryId).NotEmpty().WithMessage("Категория обязательна");
    }
}

internal sealed class ChangeStockRequestValidator : AbstractValidator<ChangeStockRequest>
{
    public ChangeStockRequestValidator()
    {
        RuleFor(r => r.Quantity).GreaterThan(0).WithMessage("Количество должно быть больше нуля");
    }
}

internal sealed class AddProductImageRequestValidator : AbstractValidator<AddProductImageRequest>
{
    public AddProductImageRequestValidator()
    {
        RuleFor(r => r.Name).RequiredText(ProductImage.MaxNameImageLength);
        RuleFor(r => r.Path).RequiredText(ProductImage.MaxPathImageLength);
    }
}

internal sealed class ProductListQueryValidator : AbstractValidator<ProductListQuery>
{
    public ProductListQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("Номер страницы начинается с 1");
        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, ProductListQuery.MaxPageSize)
            .WithMessage($"Размер страницы — от 1 до {ProductListQuery.MaxPageSize}");
        RuleFor(q => q.Search).OptionalText(ProductListQuery.MaxSearchLength);
    }
}

internal static class PriceRules
{
    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule
            .GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной")
            .Must(price => decimal.Round(price, Product.PriceDecimalPlaces) == price)
            .WithMessage($"Не больше {Product.PriceDecimalPlaces} знаков после запятой");
}
