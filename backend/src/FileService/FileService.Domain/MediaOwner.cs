using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

public sealed record MediaOwner
{
    private static readonly HashSet<string> _allowerdContexts =
    [
        "lesson",
        "module",
        "user"
    ];

    public string Context { get; }

    public Guid EntityId { get; }

    private MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

    /// <summary>
    /// Creates a new MediaOwner with validation.
    /// </summary>
    /// <param name="context">The context of the owner (e.g., lesson, module, user).</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>A result containing the MediaOwner or an Error.</returns>
    public static Result<MediaOwner, Error> Create(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context))
            return Error.Validation(null, "context is null or whitespace");

        if (context.Length > 50)
            return Error.Validation(null, "context more than 50");

        string normalizedContext = context.Trim().ToLower();
        if (!_allowerdContexts.Contains(normalizedContext))
            return Error.Validation(null, nameof(context));

        if (entityId == Guid.Empty)
            return Error.Validation(null, nameof(entityId));

        return new MediaOwner(normalizedContext, entityId);
    }

    public static Result<MediaOwner, Error> ForLesson(Guid lessonId) => Create("lesson", lessonId);
    public static Result<MediaOwner, Error> ForModule(Guid moduleId) => Create("module", moduleId);
    public static Result<MediaOwner, Error> ForUser(Guid userId) => Create("user", userId);
}
