using Shared;

namespace DirectoryService.Application.Locations.Errors;

public partial class Errors
{
    public static class Locations
    {
        public static Error IncorrectCreationOfAClassTimezoneInstance(Error error) =>
            Error.Validation(
                "locations.incorrect.timezone.instance",
                $"При создании экземпляра класса Timezone произошла ошибка: {error.Message}");
        
        public static Error IncorrectCreationOfAClassAddressInstance(Error error) =>
            Error.Validation(
                "locations.incorrect.address.instance",
                $"При создании экземпляра класса Address произошла ошибка: {error.Message}");
    }
}