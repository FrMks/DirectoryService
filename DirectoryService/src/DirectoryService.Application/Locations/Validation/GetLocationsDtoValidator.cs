using DirectoryService.Application.Validation;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Shared;

namespace DirectoryService.Application.Locations.Validation;

public class GetLocationsDtoValidator : AbstractValidator<GetLocationsRequest>
{
    public GetLocationsDtoValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null or "Name" or "Street" or "City" or "Country" or "IsActive" or "CreatedAt" or "UpdatedAt")
            .WithError(Error.Validation(
                "invalid.sort.by",
                "SortBy must be one of the following values: Name, Street, City, Country, IsActive, CreatedAt, UpdatedAt."));

        RuleFor(x => x.SortDirection)
            .Must(sortDirection => sortDirection is null or "asc" or "desc")
            .WithError(Error.Validation(
                "invalid.sort.direction",
                "SortDirection must be either 'asc' or 'desc'."));

        RuleFor(x => x.Pagination)
            .Must(pagination => pagination is null || (pagination.Page > 0 && pagination.PageSize > 0))
            .WithError(Error.Validation(
                "invalid.pagination",
                "If Pagination is provided, both Page and PageSize must be greater than 0."));
    }
}