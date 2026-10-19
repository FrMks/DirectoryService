using FileService.Contracts.IntegrationEvents;
using FileService.Core.Event;
using FileService.Domain.IntegrationEvents;
using FileService.Infrastructure.Postgres;
using FileService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.IntegrationTests.Events;

public sealed class FileDeletedRabbitMqSmokeTests : FileServiceBaseTests
{
    public FileDeletedRabbitMqSmokeTests(FileServiceTestWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task PublishingFileDeleted_DeliversItToConsumerAndRegistersEvent()
    {
        Guid eventId = Guid.NewGuid();
        FileDeleted message = new(
            EventId: eventId,
            FileId: Guid.NewGuid(),
            Status: "DELETED",
            TargetEntityType: "lesson",
            TargetEntityId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow);

        await using (AsyncServiceScope scope = Services.CreateAsyncScope())
        {
            IEventPublisher publisher =
                scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
        }

        await WaitUntilEventIsRegisteredAsync(eventId);

        ProcessedFileEvent? processedEvent = await ExecuteInDb(dbContext =>
            dbContext.ProcessedFileEvent
                .SingleOrDefaultAsync(processed => processed.EventId == eventId));

        processedEvent.Should().NotBeNull();
        processedEvent!.EventType.Should().Be(nameof(FileDeleted));
    }

    private async Task WaitUntilEventIsRegisteredAsync(Guid eventId)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            bool registered = await ExecuteInDb(dbContext =>
                dbContext.ProcessedFileEvent.AnyAsync(processed => processed.EventId == eventId));

            if (registered)
                return;

            await Task.Delay(250);
        }

        throw new TimeoutException($"FileDeleted event {eventId} was not processed within 15 seconds.");
    }
}
