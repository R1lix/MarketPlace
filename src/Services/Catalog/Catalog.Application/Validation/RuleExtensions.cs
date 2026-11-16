using FluentValidation;

namespace Catalog.Application.Validation;

// Общие правила для текстовых полей. Длина считается после Trim — так же, как в домене:
// «  Телефон  » длиной 200 с пробелами по краям допустимо, если без них укладывается в лимит
internal static class RuleExtensions
{
    public static IRuleBuilderOptions<T, string> RequiredText<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Поле обязательно")
            .Must(value => value is null || value.Trim().Length <= maxLength)
            .WithMessage($"Не длиннее {maxLength} символов");

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule
            .Must(value => value is null || value.Trim().Length <= maxLength)
            .WithMessage($"Не длиннее {maxLength} символов");
}
