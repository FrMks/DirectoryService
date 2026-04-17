using Shared;

namespace DirectoryService.Application.Locations.Fails;

public partial class Errors
{
    public static class Locations
    {
        public static Error IncorrectCreationOfAClassTimezoneInstance(Error error) =>
            Error.Validation(
                "locations.incorrect.timezone.instance",
                $"При создании экземпляра класса Timezone произошла ошибка: {error.Message}");
        
        public static Error IncorrectCreationOfAClassTimezoneInstance() =>
            Error.Validation(
                "locations.incorrect.timezone.instance",
                $"При создании экземпляра класса Timezone произошла ошибка");
        
        public static Error IncorrectCreationOfAClassAddressInstance(Error error) =>
            Error.Validation(
                "locations.incorrect.address.instance",
                $"При создании экземпляра класса Address произошла ошибка: {error.Message}");
        
        public static Error IncorrectCreationOfAClassAddressInstance() =>
            Error.Validation(
                "locations.incorrect.address.instance",
                $"При создании экземпляра класса Address произошла ошибка");
        
        public static Error IncorrectCreationOfAClassNameInstance(Error error) =>
            Error.Validation(
                "locations.incorrect.name.instance",
                $"При создании экземпляра класса Name произошла ошибка: {error.Message}");

        public static Error IncorrectCreationOfAClassNameInstance() =>
            Error.Validation(
                "locations.incorrect.name.instance",
                $"При создании экземпляра класса Name произошла ошибка");

        public static Shared.Errors IncorrectDtoValidator(Shared.Errors errors)
        {
            return errors;
        }
    }
}