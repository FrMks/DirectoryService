using FluentValidation;
using Shared;

namespace DirectoryService.Application.Extensions;

public static class ValidatorExtensions
{
    public static async Task<Errors?> GetValidationErrorsAsync<T>(
        this IValidator<T> validator,
        T model,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(model, cancellationToken);

        if (validationResult.IsValid)
            return null;

        return validationResult.ToList();
    }
}
