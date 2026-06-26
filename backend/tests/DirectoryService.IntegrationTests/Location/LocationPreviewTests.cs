using CSharpFunctionalExtensions;
using DirectoryService.Application.Database;
using DirectoryService.Application.Locations;
using DirectoryService.Application.Locations.Interfaces;
using DirectoryService.Contracts.Locations.GetLocations;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Locations.ValueObjects;
using DirectoryService.IntegrationTests.Infrastructure;
using FileService.Communication;
using FileService.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Core.Database;
using Address = DirectoryService.Domain.Locations.ValueObjects.Address;
using AttachLocationPreviewRequest = DirectoryService.Contracts.Locations.AttachLocationPreviewRequest;

namespace DirectoryService.IntegrationTests.Locations;

public class LocationPreviewTests : DirectoryBaseTests
{
    public LocationPreviewTests(DirectoryTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SuccessfulAttachPreviewToLocation()
    {
        LocationId locationId = await CreateLocation();
        Guid mediaAssetId = Guid.NewGuid();

        FileResponse fileResponse = new(
            mediaAssetId,
            "office.jpg",
            "image/jpeg",
            123456,
            "READY",
            "PREVIEW",
            "location",
            locationId.Value,
            "https://example.com/office.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow);

        var fileCommunicationService = new FakeFileCommunicationService(fileResponse);
        CancellationToken cancellationToken = CancellationToken.None;

        Result<Guid, Errors> result = await ExecuteHandler(
            fileCommunicationService,
            async setLocationPrevieHandler =>
            {
                var command = new SetLocationPreviewCommand(
                    locationId.Value,
                    new AttachLocationPreviewRequest(mediaAssetId));

                return await setLocationPrevieHandler.Handle(command, cancellationToken);
            });

        Assert.True(result.IsSuccess);
        Assert.Equal(locationId.Value, result.Value);

        await ExecuteInDb(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(l => l.Id == locationId, cancellationToken);

            Assert.NotNull(location.PreviewMetadata);
            Assert.Equal(mediaAssetId, location.PreviewMetadata.AssetId.Value);
            Assert.Equal("office.jpg", location.PreviewMetadata.FileName);
            Assert.Equal("image/jpeg", location.PreviewMetadata.ContentType);
            Assert.Equal(123456, location.PreviewMetadata.Size);
        });
    }

    [Fact]
    public async Task SuccessfulReplacePreviewToLocation()
    {
        LocationId locationId = await CreateLocation();
        Guid mediaAssetId1 = Guid.NewGuid();

        FileResponse fileResponse1 = new(
            mediaAssetId1,
            "old-office.jpg",
            "image/jpeg",
            123456,
            "READY",
            "PREVIEW",
            "location",
            locationId.Value,
            "https://example.com/old-office.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow);

        var fileCommunicationService1 = new FakeFileCommunicationService(fileResponse1);
        CancellationToken cancellationToken = CancellationToken.None;

        Result<Guid, Errors> oldResult = await ExecuteHandler(
            fileCommunicationService1,
            async setLocationPreviewHandler =>
            {
                var command = new SetLocationPreviewCommand(
                    locationId.Value,
                    new AttachLocationPreviewRequest(mediaAssetId1));

                return await setLocationPreviewHandler.Handle(command, cancellationToken);
            });

        Assert.True(oldResult.IsSuccess);
        Assert.Equal(locationId.Value, oldResult.Value);

        await ExecuteInDb(async dbContext =>
            {
                var location = await dbContext.Locations
                    .FirstAsync(l => l.Id == locationId, cancellationToken);

                Assert.NotNull(location.PreviewMetadata);
                Assert.Equal(mediaAssetId1, location.PreviewMetadata.AssetId.Value);
                Assert.Equal("old-office.jpg", location.PreviewMetadata.FileName);
                Assert.Equal("image/jpeg", location.PreviewMetadata.ContentType);
                Assert.Equal(123456, location.PreviewMetadata.Size);
            });

        Guid mediaAssetId2 = Guid.NewGuid();

        FileResponse fileResponse2 = new(
            mediaAssetId2,
            "new-office.jpg",
            "image/jpeg",
            654321,
            "READY",
            "PREVIEW",
            "location",
            locationId.Value,
            "https://example.com/new-office.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow);

        var fileCommunicationService2 = new FakeFileCommunicationService(fileResponse2);

        Result<Guid, Errors> newResult = await ExecuteHandler(
            fileCommunicationService2,
            async setLocationPreviewHandler =>
            {
                var command = new SetLocationPreviewCommand(
                    locationId.Value,
                    new AttachLocationPreviewRequest(mediaAssetId2));

                return await setLocationPreviewHandler.Handle(command, cancellationToken);
            });

        Assert.True(newResult.IsSuccess);
        Assert.Equal(locationId.Value, newResult.Value);

        await ExecuteInDb(async dbContext =>
           {
               var location = await dbContext.Locations
                   .FirstAsync(l => l.Id == locationId, cancellationToken);

               Assert.NotNull(location.PreviewMetadata);
               Assert.Equal(mediaAssetId2, location.PreviewMetadata.AssetId.Value);
               Assert.Equal("new-office.jpg", location.PreviewMetadata.FileName);
               Assert.Equal("image/jpeg", location.PreviewMetadata.ContentType);
               Assert.Equal(654321, location.PreviewMetadata.Size);
           });
    }

    [Fact]
    public async Task SuccessRemovePreviewFromLocation()
    {
        LocationId locationId = await CreateLocation();
        Guid mediaAssetId = Guid.NewGuid();

        FileResponse fileResponse = new(
            mediaAssetId,
            "office.jpg",
            "image/jpeg",
            123456,
            "READY",
            "PREVIEW",
            "location",
            locationId.Value,
            "https://example.com/office.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow);

        var fileCommunicationService = new FakeFileCommunicationService(fileResponse);
        CancellationToken cancellationToken = CancellationToken.None;

        Result<Guid, Errors> setPreviewResult = await ExecuteHandler(
            fileCommunicationService,
            async setLocationPrevieHandler =>
            {
                var command = new SetLocationPreviewCommand(
                    locationId.Value,
                    new AttachLocationPreviewRequest(mediaAssetId));

                return await setLocationPrevieHandler.Handle(command, cancellationToken);
            });

        Assert.True(setPreviewResult.IsSuccess);
        Assert.Equal(locationId.Value, setPreviewResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(l => l.Id == locationId, cancellationToken);

            Assert.NotNull(location.PreviewMetadata);
            Assert.Equal(mediaAssetId, location.PreviewMetadata.AssetId.Value);
            Assert.Equal("office.jpg", location.PreviewMetadata.FileName);
            Assert.Equal("image/jpeg", location.PreviewMetadata.ContentType);
            Assert.Equal(123456, location.PreviewMetadata.Size);
        });

        Result<Guid, Errors> removePreviewResult = await ExecuteRemoveHandler(
            async removeLocationPreviewHandler =>
            {
                RemoveLocationPreviewCommand command = new RemoveLocationPreviewCommand(locationId);

                return await removeLocationPreviewHandler.Handle(command, cancellationToken);
            });

        Assert.True(removePreviewResult.IsSuccess);
        Assert.Equal(locationId.Value, removePreviewResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(l => l.Id == locationId, cancellationToken);

            Assert.Null(location.PreviewMetadata);
        });
    }

    [Fact]
    public async Task DegradedRead()
    {
        LocationId locationId = await CreateLocation();
        Guid mediaAssetId = Guid.NewGuid();

        FileResponse fileResponse = new(
            mediaAssetId,
            "office.jpg",
            "image/jpeg",
            123456,
            "READY",
            "PREVIEW",
            "location",
            locationId.Value,
            "https://example.com/office.jpg",
            DateTime.UtcNow,
            DateTime.UtcNow);

        var fileCommunicationService = new FakeFileCommunicationService(fileResponse);
        CancellationToken cancellationToken = CancellationToken.None;

        Result<Guid, Errors> setPreviewResult = await ExecuteHandler(
            fileCommunicationService,
            async setLocationPrevieHandler =>
            {
                var command = new SetLocationPreviewCommand(
                    locationId.Value,
                    new AttachLocationPreviewRequest(mediaAssetId));

                return await setLocationPrevieHandler.Handle(command, cancellationToken);
            });

        Assert.True(setPreviewResult.IsSuccess);
        Assert.Equal(locationId.Value, setPreviewResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            var location = await dbContext.Locations
                .FirstAsync(l => l.Id == locationId, cancellationToken);

            Assert.NotNull(location.PreviewMetadata);
            Assert.Equal(mediaAssetId, location.PreviewMetadata.AssetId.Value);
            Assert.Equal("office.jpg", location.PreviewMetadata.FileName);
            Assert.Equal("image/jpeg", location.PreviewMetadata.ContentType);
            Assert.Equal(123456, location.PreviewMetadata.Size);
        });

        var unavailableFileCommunicationService = new FailingFileCommunicationService();

        Result<GetLocationResponse, Errors> getLocationByIdResult = await ExecuteGetLocationByIdHandler(
            unavailableFileCommunicationService,
            async getLocationByIdHandler =>
            {
                var query = new GetLocationByIdQuery(locationId);

                return await getLocationByIdHandler.Handle(query, cancellationToken);
            });

        Assert.True(getLocationByIdResult.IsSuccess);
        Assert.Equal(locationId.Value, getLocationByIdResult.Value.Id);
        Assert.Equal(mediaAssetId, getLocationByIdResult.Value.Preview.AssetId);
        Assert.Equal("TemporarilyUnavailable", getLocationByIdResult.Value.Preview.Status);
        Assert.Equal("office.jpg", getLocationByIdResult.Value.Preview.FileName);
        Assert.Equal("image/jpeg", getLocationByIdResult.Value.Preview.ContentType);
        Assert.Equal(123456, getLocationByIdResult.Value.Preview.Size);
        Assert.Null(getLocationByIdResult.Value.Preview.ContentUrl);
        Assert.Equal(
            "File Service is temporarily unavailable",
            getLocationByIdResult.Value.Preview.Message);
    }

    private async Task<T> ExecuteGetLocationByIdHandler<T>(
        IFileCommunicationService fileCommunicationService,
        Func<GetLocationByIdHandler, Task<T>> action)
    {
        AsyncServiceScope scope = Services.CreateAsyncScope();

        IReadDbContext dbContext = scope.ServiceProvider.GetRequiredService<IReadDbContext>();
        ILogger<GetLocationByIdHandler> logger = scope.ServiceProvider.GetRequiredService<ILogger<GetLocationByIdHandler>>();

        var handler = new GetLocationByIdHandler(
            dbContext,
            fileCommunicationService,
            logger);

        return await action(handler);
    }

    private async Task<T> ExecuteHandler<T>(
        IFileCommunicationService fileCommunicationService,
        Func<SetLocationPreviewHandler, Task<T>> action) // функция которая принимает SetLocationPreviewHandler и возвращаем Task<T>
    {
        AsyncServiceScope scope = Services.CreateAsyncScope();

        ILocationsRepository locationsRepository = scope.ServiceProvider.GetRequiredService<ILocationsRepository>();
        ITransactionManager transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        ILogger<SetLocationPreviewHandler> logger = scope.ServiceProvider.GetRequiredService<ILogger<SetLocationPreviewHandler>>();

        var handler = new SetLocationPreviewHandler(
            locationsRepository,
            fileCommunicationService,
            transactionManager,
            logger);

        return await action(handler);
    }

    private async Task<T> ExecuteRemoveHandler<T>(Func<RemoveLocationPreviewHandler, Task<T>> action)
    {
        AsyncServiceScope scope = Services.CreateAsyncScope();

        ILocationsRepository locationsRepository = scope.ServiceProvider.GetRequiredService<ILocationsRepository>();
        ITransactionManager transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        ILogger<RemoveLocationPreviewHandler> logger = scope.ServiceProvider.GetRequiredService<ILogger<RemoveLocationPreviewHandler>>();

        var handler = new RemoveLocationPreviewHandler(locationsRepository, transactionManager, logger);

        return await action(handler);
    }

    private sealed class FakeFileCommunicationService : IFileCommunicationService
    {
        private readonly FileResponse _response;

        public FakeFileCommunicationService(FileResponse response)
        {
            _response = response;
        }

        public Task<Result<FileResponse, Errors>> GetMediaAssetByIdAsync(
            Guid mediaAssetId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Result<FileResponse, Errors>>(_response);
        }

        public Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(
            string context,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(
            Guid fileId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FailingFileCommunicationService : IFileCommunicationService
    {
        public Task<Result<FileResponse, Errors>> GetMediaAssetByIdAsync(
            Guid mediaAssetId,
            CancellationToken cancellationToken)
        {
            Errors errors = Error.Failure(
                "file-service.unavailable",
                "File Service is unavailable").ToErrors();

            return Task.FromResult<Result<FileResponse, Errors>>(errors);
        }

        public Task<Result<IReadOnlyList<FileResponse>, Errors>> GetFilesByOwnerAsync(
            string context,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<GetContentUrlResponse, Errors>> GetContentUrlAsync(
            Guid fileId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private async Task<LocationId> CreateLocation()
    {
        return await ExecuteInDb(async dbContext =>
        {
            LocationId locationId = LocationId.NewLocationId();

            var location = Domain.Locations.Location.Create(
                locationId,
                Name.Create("Location").Value,
                Address.Create("Street", "City", "Country").Value,
                Timezone.Create("Europe/London").Value,
                new List<DepartmentLocation>())
                .Value;

            dbContext.Locations.Add(location);
            await dbContext.SaveChangesAsync();

            return locationId;
        });
    }
}
