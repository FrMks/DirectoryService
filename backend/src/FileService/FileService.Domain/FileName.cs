using System.Reflection;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Shared;

namespace FileService.Domain;

/// <summary>
/// Чтобы проверить, что имя не пустое и у файла есть расширение.
/// Потом VideoAsset и PreviewAsset исопльзуют уже готовое Extension
/// </summary>
public sealed record FileName
{
    public string Name { get; }

    public string Extension { get; }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
    }

    public static Result<FileName, Error> Create(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Error.Validation("value.is.null", nameof(fileName));

        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
            return Error.Validation("file.dont.have.extension", "File must have extension");

        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();
        return new FileName(fileName, extension);
    }
}
