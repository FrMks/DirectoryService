using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Locations.GetLocations;

namespace DirectoryService.Application.Locations;

public record GetLocationsCommand(GetLocationsRequest LocationsRequest) : ICommand;