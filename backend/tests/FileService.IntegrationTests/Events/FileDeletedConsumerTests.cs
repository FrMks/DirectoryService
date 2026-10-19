using FileService.Contracts.IntegrationEvents;
using FileService.Core.Event;
using FileService.Domain.IntegrationEvents;
using FileService.Infrastructure.Postgres.Repositories;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileService.IntegrationTests.Events;

public sealed class FileDeletedConsumerTests : FileServiceBaseTests
{
    public FileDeletedConsumerTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Handle_WhenEventIsReceivedFirstTime_RegistersIt()
    {
        Guid eventId = Guid.NewGuid();
        FileDeleted message = CreateMessage(eventId);

        await Handle(message);

        List<ProcessedFileEvent> processedEvents = await ExecuteInDb(dbContext =>
            dbContext.ProcessedFileEvent
                .Where(processedEvent => processedEvent.EventId == eventId)
                .ToListAsync());

        processedEvents.Should().ContainSingle();
        processedEvents[0].EventType.Should().Be(nameof(FileDeleted));
    }

    [Fact]
    public async Task Handle_WhenSameEventIsReceivedTwice_RegistersItOnlyOnce()
    {
        Guid eventId = Guid.NewGuid();
        FileDeleted message = CreateMessage(eventId);

        await Handle(message);
        await Handle(message);

        int processedEventCount = await ExecuteInDb(dbContext =>
            dbContext.ProcessedFileEvent
                .CountAsync(processedEvent => processedEvent.EventId == eventId));

        processedEventCount.Should().Be(1);
    }

    private async Task Handle(FileDeleted message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        IProcessedFileEventRepository repository =
            scope.ServiceProvider.GetRequiredService<IProcessedFileEventRepository>();

        FileDeletedConsumer consumer = new(
            repository,
            NullLogger<FileDeletedConsumer>.Instance);

        (await consumer.Handle(message, CancellationToken.None)).IsSuccess.Should().BeTrue();
    }

    private static FileDeleted CreateMessage(Guid eventId) => new(
        EventId: eventId,
        FileId: Guid.NewGuid(),
        Status: "DELETED",
        TargetEntityType: "lesson",
        TargetEntityId: Guid.NewGuid(),
        OccurredAt: DateTimeOffset.UtcNow);
}
