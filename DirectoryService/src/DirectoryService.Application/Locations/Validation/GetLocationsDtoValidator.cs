using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace DirectoryService.Application.Locations.Validation;

public class GetLocationsDtoValidator : AbstractValidator<GetLocationsRequest>
{
    public GetLocationsDtoValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null or "Name" or "Street" or "City" or "Country" or "IsActive" or "CreatedAt" or "UpdatedAt")
            .WithMessage("SortBy must be one of the following values: Name, Street, City, Country, IsActive, CreatedAt, UpdatedAt.");

        RuleFor(x => x.SortDirection)
            .Must(sortDirection => sortDirection is null or "asc" or "desc")
            .WithMessage("SortDirection must be either 'asc' or 'desc'.");

        RuleFor(x => x.Pagination)
            .Must(pagination => pagination is null || (pagination.Page.HasValue && pagination.Page > 0 && pagination.PageSize.HasValue && pagination.PageSize > 0))
            .WithMessage("Pagination Page and PageSize must be greater than 0.");
    }
}