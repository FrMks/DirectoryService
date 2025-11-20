using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Shared;

namespace DirectoryService.Domain.Locations.ValueObjects;

public partial record Timezone
{
    private Timezone(string value)
    {
        Value = value;
    }

    public string Value { get; }

    [GeneratedRegex(@"^[A-Za-z_]+\/[A-Za-z_]+$")]
    private static partial Regex ValidFormatRegex();
    
    public static Result<Timezone, Error> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Error.Validation(null, "Timezone input cannot be empty");
        
        string trimmed = input.Trim();
        
        if (!ValidFormatRegex().IsMatch(trimmed))
            return Error.Validation(null, "Timezone input is not valid");
        
        Timezone timezone = new(input);

        return Result.Success<Timezone, Error>(timezone);
    }
}