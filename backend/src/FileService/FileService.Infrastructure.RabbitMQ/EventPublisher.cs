using FileService.Core.Event;
using Wolverine;

namespace FileService.Infrastructure.RabbitMQ;

public class EventPublisher : IEventPublisher
{
    private readonly IMessageBus _messageBus;

    public EventPublisher(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : class
    {
        await _messageBus.PublishAsync(@event);
    }
}