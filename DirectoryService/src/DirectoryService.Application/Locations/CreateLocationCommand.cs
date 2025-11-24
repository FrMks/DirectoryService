using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Locations;

namespace DirectoryService.Application.Locations;

public record CreateLocationCommand(CreateLocationRequest LocationRequest) : ICommand;