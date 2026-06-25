using Shared.Core.Abstractions;

namespace DirectoryService.Application.Locations;

public record GetLocationByIdQuery(Guid LocationId) : ICommand;