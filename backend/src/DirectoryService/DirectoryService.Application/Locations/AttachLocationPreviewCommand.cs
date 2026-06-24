using DirectoryService.Contracts.Locations;
using Shared.Core.Abstractions;

namespace DirectoryService.Application.Locations;

public record AttachLocationPreviewCommand(
    Guid LocationId,
    AttachLocationPreviewRequest Request) : ICommand;