using Shared.Core.Abstractions;
using DirectoryService.Contracts.Positions;

namespace DirectoryService.Application.Positions;

public record CreatePositionCommand(CreatePositionRequest PositionRequest) : ICommand;