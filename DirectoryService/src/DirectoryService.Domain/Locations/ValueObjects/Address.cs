using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Locations.ValueObjects;

public record Address
{
    private Address(string street, string city, string country)
    {
        Street = street.Trim();
        City = city.Trim();
        Country = country.Trim();
    }

    public string Street { get; init; }

    public string City { get; init; }

    public string Country { get; init; }

    public static Result<Address> Create(string street, string city, string country)
    {
        #region street

        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>("Street cannot be empty");
        
        string trimmedStreet = street.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedStreet))
            return Result.Failure<Address>("Street cannot be empty");
        
        if (trimmedStreet.Length > LengthConstants.LENGTH100)
            return Result.Failure<Address>("Street cannot be more than 100 characters");

        #endregion

        #region city

        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure<Address>("City cannot be empty");
        
        string trimmedCity = city.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedCity))
            return Result.Failure<Address>("City cannot be empty");
        
        if (trimmedCity.Length > LengthConstants.LENGTH60)
            return Result.Failure<Address>("City cannot be more than 60 characters");

        #endregion

        #region country

        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<Address>("Country cannot be empty");
        
        string trimmedCountry = country.Trim();
        
        if (string.IsNullOrWhiteSpace(trimmedCountry))
            return Result.Failure<Address>("Country cannot be empty");
        
        if (trimmedCountry.Length > LengthConstants.LENGTH60)
            return Result.Failure<Address>("Country cannot be more than 60 characters");

        #endregion
        
        Address address = new(street, city, country);
        
        return Result.Success(address);
    }
}