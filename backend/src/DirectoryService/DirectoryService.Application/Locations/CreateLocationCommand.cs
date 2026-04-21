using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Core.Abstractions;
using DirectoryService.Contracts.Locations;

namespace DirectoryService.Application.Locations;

public record CreateLocationCommand(CreateLocationRequest LocationRequest) : ICommand;