namespace DirectoryService.Contracts.Locations;

public record GetLocationsRequest(
    List<Guid?> DepartmentIds = null, // Если список указан, возвращаются только локации связанные с этим подразделением 
    string? Search = null, // Поиск по названию локации (частичное совпадение без регистра)
    bool? IsActive = null, // Фильтр по активности: true - только активные, false - только неактивные
    int? Page = 1, // Номер страницы (по умолчанию 1)
    int? PageSize = 20); // Количество элементов по странице (по умолчанию 20)