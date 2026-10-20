namespace FileService.Contracts.IntegrationEvents;

public sealed record VideoProcessingFailed(
    Guid EventId,
    Guid FileId,
    string Status,
    string TargetEntityType, // Соответствует Owner.Context
    Guid TargetEntityId, // Соответствует Owner.EntityId
    string ErrorMessage,
    DateTimeOffset OccurredAt);