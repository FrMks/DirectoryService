using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Locations.ValueObjects;

public partial record Timezone
{
    private Timezone(string value)
    {
        Value = value;
    }

    public string Value { get; init; }

    [GeneratedRegex(@"^[A-Za-z_]+\/[A-Za-z_]+$")]
    private static partial Regex ValidFormatRegex();
    
    public static Result<Timezone> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            Result.Failure("Input cannot be empty");
        
        string trimmed = input.Trim();
        
        if (!ValidFormatRegex().IsMatch(trimmed))
            Result.Failure("Input is not valid");

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
        
        if (!timeZone.HasIanaId)
            Result.Failure("Time zone is not found");
        
        Timezone timezone = new Timezone(input);

        return timezone;
    }
}