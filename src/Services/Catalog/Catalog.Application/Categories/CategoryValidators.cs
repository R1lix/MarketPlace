using Catalog.Application.Validation;
using Catalog.Domain;
using FluentValidation;

namespace Catalog.Application.Categories;

internal sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(r => r.Name).RequiredText(Category.MaxNameLength);
    }
}

internal sealed class RenameCategoryRequestValidator : AbstractValidator<RenameCategoryRequest>
{
    public RenameCategoryRequestValidator()
    {
        RuleFor(r => r.Name).RequiredText(Category.MaxNameLength);
    }
}
