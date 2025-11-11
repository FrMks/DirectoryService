using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectoryService.Application.Abstractions;
using DirectoryService.Contracts.Locations;

namespace DirectoryService.Application.Locations;

/// <summary>
/// Введен CreateLocationCommand в слое Application
/// для удаления прямой зависимости от CreateLocationRequest из проекта Contracts.
///  Это обеспечивает более строгие границы между слоями и соответствует принципам CQRS.
/// </summary>
/// <param name="LocationRequest"></param>
public record CreateLocationCommand(CreateLocationRequest LocationRequest) : ICommand;