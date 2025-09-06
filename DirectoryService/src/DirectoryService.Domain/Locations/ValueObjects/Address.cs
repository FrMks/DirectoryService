using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Locations.ValueObjects;

public class Address
{
    private Address(string street, string city, string country)
    {
        Street = street.Trim();
        City = city.Trim();
        Country = country.Trim();
    }

    public string Street { get; private set; }

    public string City { get; private set; }

    public string Country { get; private set; }

    public static Result<Address> Create(string street, string city, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>("Street cannot be empty");
        
        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure<Address>("City cannot be empty");
        
        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<Address>("Country cannot be empty");
        
        Address address = new(street, city, country);
        
        return Result.Success(address);
    }

}