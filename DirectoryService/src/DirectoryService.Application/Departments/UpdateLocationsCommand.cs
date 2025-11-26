using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Application.Departments;

public record UpdateLocationsCommand(UpdateLocationsRequest LocationsRequest) : ICommand;