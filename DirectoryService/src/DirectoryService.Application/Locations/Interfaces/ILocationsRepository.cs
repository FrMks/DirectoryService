using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using Shared;

namespace DirectoryService.Application.Locations.Interfaces;

public interface ILocationsRepository
{
    Task<Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken);

    /// <summary>
    /// Проверяем, что внутри БД таблицы Locations есть все location, которые были переданы Id.
    /// </summary>
    /// <param name="locationIds">Идентификаторы, которые должны быть внутри БД таблицы Locations.</param>
    /// <param name="cancellationToken">Токен для отмены операции.</param>
    /// <returns>True если все идентификаторы есть в БД. Error если какого-то идентификатора нету.</returns>
    Task<Result<bool, Error>> AllExistAsync(List<Guid> locationIds, CancellationToken cancellationToken);

    Task<Result<Location, Error>> GetBy(Expression<Func<Location, bool>> predicate, CancellationToken cancellationToken);

    Task<Result<List<Location>, Error>> GetLocationsByIds(
        List<LocationId> locationIds,
        CancellationToken cancellationToken);

    Task<Result<HashSet<LocationId>, Error>> GetLocationIdsWithOtherActiveDepartments(
        List<LocationId> locationIds,
        DepartmentId deletingDepartmentId,
        CancellationToken cancellationToken);
}
