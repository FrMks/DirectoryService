using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations;
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
    
    Task<Result<List<Location>, Error>> GetLocationsAsync(List<Guid> locationId, CancellationToken cancellationToken);
    
    Task<Result<Location, Error>> GetLocationByName(string name, CancellationToken cancellationToken);
    
    Task<Result<List<Location>, Error>> GetLocationsByIsActive(
        bool isActive,
        CancellationToken cancellationToken);

    Task<Result<List<Location>, Error>> GetLocationsByPagination(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}