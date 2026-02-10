using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirectoryService.Contracts.Locations.GetLocations;

public record GetLocationsResult(
    List<GetLocationsResponse> Locations,
    long TotalCount
);