using Shared;

namespace DirectoryService.Application.Locations.Errors;

public partial class Errors
{
    public static class Locations
    {
        public static Error IncorrectCreationOfAClassTimezoneInstance(Error error) =>
            Error.Failure("locations.incorrect.timezone.instance",
                $"При создании экземпляра класса Timezone произошла ошибка: {error.Message}");
    }
}