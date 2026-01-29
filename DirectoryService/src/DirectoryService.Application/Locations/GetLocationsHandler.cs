using CSharpFunctionalExtensions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Locations.GetLocations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    IReadDbContext readDbContext,
    IValidator<GetLocationsRequest> validator,
    ILogger<CreateLocationHandler> logger)
{
    public async Task<Result<(List<GetLocationsResponse>, long TotalCount), Errors>> Handle(
        GetLocationsQuery locationsQuery,
        CancellationToken cancellationToken)
    {
        // Валидация DTO
        var validationResult = await validator.ValidateAsync(locationsQuery.LocationsRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.ToList())
            {
                logger.LogInformation("Error when creating location, error: {error}", error.Message);
            }

            return validationResult.ToList();
        }

        var locationsQueryResponse = readDbContext.LocationsRead;

        if (locationsQuery.LocationsRequest.DepartmentIds is not null && locationsQuery.LocationsRequest.DepartmentIds.Count != 0)
        {
            locationsQueryResponse = readDbContext.DepartmentLocationsRead
                .Where(dl => locationsQuery.LocationsRequest.DepartmentIds.Contains(dl.DepartmentId))
            .Join(readDbContext.LocationsRead,
                dl => dl.LocationId,
                l => l.Id,
                (dl, l) => l)
            .Distinct(); // Без дубликатов
        }

        if (!string.IsNullOrWhiteSpace(locationsQuery.LocationsRequest.Search))
        {
            locationsQueryResponse = locationsQueryResponse
                .Where(l => EF.Functions.ILike(l.Name.Value, $"%{locationsQuery.LocationsRequest.Search}%"));
        }

        if (locationsQuery.LocationsRequest.IsActive.HasValue)
        {
            locationsQueryResponse = locationsQueryResponse
                .Where(lr => lr.IsActive == locationsQuery.LocationsRequest.IsActive);
        }

        long totalCount = await locationsQueryResponse.CountAsync(cancellationToken);

        if (locationsQuery.LocationsRequest.Pagination is not null && locationsQuery.LocationsRequest.Pagination.PageSize.HasValue
            && locationsQuery.LocationsRequest.Pagination.Page.HasValue)
        {
            if (locationsQuery.LocationsRequest.Pagination.PageSize < 1)
            {
                logger.LogError("Request with page size < 1 cannot be executed. Page size must be >= 1");
                return Error.Validation("pagination.pageSize", "PageSize must be >= 1").ToErrors();
            }

            if (locationsQuery.LocationsRequest.Pagination.Page < 1)
            {
                logger.LogError("Request with page number < 1 cannot be executed. Page number must be >= 1");
                return Error.Validation("pagination.page", "Page must be >= 1").ToErrors();
            }

            int skipCount = (int)((locationsQuery.LocationsRequest.Pagination.Page - 1) * locationsQuery.LocationsRequest.Pagination.PageSize);
            locationsQueryResponse = locationsQueryResponse
                .OrderBy(l => l.CreatedAt)
                .Skip(skipCount)
                .Take((int)locationsQuery.LocationsRequest.Pagination.PageSize);
        }

        List<GetLocationsResponse> locations = await locationsQueryResponse
            .Select(l => new GetLocationsResponse
            {
                Id = l.Id.Value,
                Name = l.Name.Value,
                Street = l.Address.Street,
                City = l.Address.City,
                Country = l.Address.Country,
                Timezone = l.Timezone.Value,
                IsActive = l.IsActive,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return (locations, totalCount);
    }
}