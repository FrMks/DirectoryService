using CSharpFunctionalExtensions;

namespace DirectoryService.Domain.Positions.ValueObject;

// TODO: написано сделать уникальным, но на сколько я понимаю,
// то уникальность проверяется по сравнению с чем-то
// (хотя бы есть массив, в котором лежат другие имена позиций)
public class Name
{
    private Name(string value)
    {
        Value = value;
    }
    
    public string Value { get; private set; }
    
    public Result<Name> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            Result.Failure("Name cannot be null or empty");
            
        string trimmedValue = value.Trim();
        
        if (trimmedValue.Length < 3 || trimmedValue.Length > 100)
            return Result.Failure<Name>("Name cannot be less than 3 characters and more than 100 characters");
        
        Name name = new(trimmedValue);

        return Result.Success(name);
    }
    
    public void SetValue(string value) => Value = value;
}