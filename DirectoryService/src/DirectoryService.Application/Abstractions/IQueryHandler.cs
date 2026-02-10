using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirectoryService.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}