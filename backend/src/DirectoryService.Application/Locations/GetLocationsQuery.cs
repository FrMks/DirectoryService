using Shared.Core.Abstractions;
using DirectoryService.Contracts.Locations.GetLocations;

namespace DirectoryService.Application.Locations;

public record GetLocationsQuery(GetLocationsRequest LocationsRequest) : ICommand;