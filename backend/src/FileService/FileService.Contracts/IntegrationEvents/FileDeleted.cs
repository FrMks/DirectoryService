namespace FileService.Contracts.IntegrationEvents;

public sealed record FileDeleted(
    Guid EventId,
    Guid FileId,
    string Status,
    string TargetEntityType, // Соответствует Owner.Context
    Guid TargetEntityId, // Соответствует Owner.EntityId
    DateTimeOffset OccurredAt);