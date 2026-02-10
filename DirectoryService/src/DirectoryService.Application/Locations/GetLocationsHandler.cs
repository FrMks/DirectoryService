using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Application.Abstractions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Extensions;
using DirectoryService.Contracts.Locations.GetLocations;
using DirectoryService.Domain.Locations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace DirectoryService.Application.Locations;

public class GetLocationsHandler(
    IReadDbContext readDbContext,
    IValidator<GetLocationsRequest> validator,
    ILogger<GetLocationsHandler> logger)
    : IQueryHandler<GetLocationsQuery, Result<GetLocationsResult, Errors>>
{
    public async Task<Result<GetLocationsResult, Errors>> Handle(
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

        // Фильтруем в зависимости от переданных данных в LocationsRequest
        if (locationsQuery.LocationsRequest.DepartmentIds is not null
            && locationsQuery.LocationsRequest.DepartmentIds.Count != 0)
        {
            var locationIdsQuery = readDbContext.DepartmentLocationsRead
             .Where(dl => locationsQuery.LocationsRequest.DepartmentIds.Contains(dl.DepartmentId))
             .Select(dl => dl.LocationId)
             .Distinct();

            locationsQueryResponse = locationsQueryResponse
                 .Where(l => locationIdsQuery.Contains(l.Id));
        }

        if (!string.IsNullOrWhiteSpace(locationsQuery.LocationsRequest.Search))
        {
            locationsQueryResponse = locationsQueryResponse
                .Where(l => EF.Functions.Like(((string)(object)l.Name).ToLower(), $"%{locationsQuery.LocationsRequest.Search.ToLower()}%"));
        }

        if (locationsQuery.LocationsRequest.IsActive.HasValue)
        {
            locationsQueryResponse = locationsQueryResponse
                .Where(lr => lr.IsActive == locationsQuery.LocationsRequest.IsActive);
        }

        // Сортировка
        Expression<Func<Location, object>> keySelector = locationsQuery.LocationsRequest.SortBy?.ToLower() switch
        {
            "name" => l => l.Name,
            "street" => l => l.Address.Street,
            "city" => l => l.Address.City,
            "country" => l => l.Address.Country,
            "isactive" => l => l.IsActive,
            "createdat" => l => l.CreatedAt,
            "updatedat" => l => l.UpdatedAt,
            _ => l => l.Name,
        };

        locationsQueryResponse = locationsQuery.LocationsRequest.SortDirection == "asc"
            ? locationsQueryResponse.OrderBy(keySelector)
            : locationsQueryResponse.OrderByDescending(keySelector);

        long totalCount = await locationsQueryResponse.CountAsync(cancellationToken);

        // Пагинация
        if (locationsQuery.LocationsRequest.Pagination is not null
            && locationsQuery.LocationsRequest.Pagination.PageSize.HasValue
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
                .Skip(skipCount)
                .Take((int)locationsQuery.LocationsRequest.Pagination.PageSize);
        }

        // Проекция в DTO
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

        return new GetLocationsResult(locations, totalCount);
    }
}