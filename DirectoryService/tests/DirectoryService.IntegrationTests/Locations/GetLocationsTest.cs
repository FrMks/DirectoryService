using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using Xunit;

namespace DirectoryService.IntegrationTests.Location
{
    public class GetLocationsTest : DirectoryBaseTests
    {
        public GetLocationsTest(DirectoryTestWebFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public void GetLocationsWithValidDepartmentIds()
        {
            // Arrange

        }

        private async Task<LocationId> CreateLocation()
        {
            return await ExecuteInDb(async dbContext =>
            {
                LocationId locationId = LocationId.NewLocationId();

                var location = Domain.Locations.Location.Create(
                    locationId,
                    Name.Create("Локация").Value,
                    Address.Create("Улица", "Город", "Страна").Value,
                    Timezone.Create("Europe/London").Value,
                    new List<DepartmentLocation>()
                ).Value;

                dbContext.Locations.Add(location);
                await dbContext.SaveChangesAsync();

                return locationId;
            });
        }
    }
}