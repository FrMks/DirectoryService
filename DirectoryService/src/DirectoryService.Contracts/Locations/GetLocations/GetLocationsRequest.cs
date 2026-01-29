namespace DirectoryService.Contracts.Locations.GetLocations;

public record GetLocationsRequest(
    List<Guid?> DepartmentIds = null, // Если список указан, возвращаются только локации связанные с этим подразделением 
    string? Search = null, // Поиск по названию локации (частичное совпадение без регистра)
    bool? IsActive = null, // Фильтр по активности: true - только активные, false - только неактивные
    PaginationRequest? Pagination = null
);

public record PaginationRequest(int? Page = 1, int? PageSize = 20);