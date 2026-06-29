using Shared.Core.Abstractions;

namespace DirectoryService.Application.Locations;

public record RemoveLocationPreviewCommand(Guid LocationId) : ICommand;