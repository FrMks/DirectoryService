using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public sealed record MediaOwner
{
    private static readonly HashSet<string> AllowedContexts =
    [
        "lesson",
        "course",
        "user",
        "department",
    ];

    public string Context { get; }

    public Guid EntityId { get; }

    private MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

    public static Result<MediaOwner, Error> Create(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context))
            return Error.Validation(null, "context is null or whitespace");

        if (context.Length > 50)
            return Error.Validation(null, "context more than 50");

        string normalizedContext = context.Trim().ToLowerInvariant();
        if (!AllowedContexts.Contains(normalizedContext))
            return Error.Validation(null, nameof(context));

        if (entityId == Guid.Empty)
            return Error.Validation(null, nameof(entityId));

        return new MediaOwner(normalizedContext, entityId);
    }

    public static Result<MediaOwner, Error> ForLesson(Guid lessonId) => Create("lesson", lessonId);

    public static Result<MediaOwner, Error> ForCourse(Guid courseId) => Create("course", courseId);

    public static Result<MediaOwner, Error> ForUser(Guid userId) => Create("user", userId);

    public static Result<MediaOwner, Error> ForDepartment(Guid departmentId) => Create("department", departmentId);
}
