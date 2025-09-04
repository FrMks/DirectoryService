using CSharpFunctionalExtensions;
using DirectoryService.Domain.Department.ValueObject;

namespace DirectoryService.Domain.Department;

public class Department
{
    private Department(Name name, Identifier identifier)
    {
        Id = Guid.NewGuid();
        Name = name;
        Identifier = identifier;
    }
    
    public Guid Id { get; private set; }
    
    public Name Name { get; private set; }
    
    public Identifier Identifier { get; private set; }
    
    /// <summary>
    /// Компания "Рога и копыта"
    /// ├── IT отдел (ParentId = null - это корень)
    ///     │   ├── Backend команда (ParentId = ID of "IT отдел")
    /// │   └── Frontend команда (ParentId = ID of "IT отдел")
    /// ├── HR отдел (ParentId = null - это тоже корень)
    ///     │   ├── Найм (ParentId = ID of "HR отдел")
    /// │   └── Обучение (ParentId = ID of "HR отдел")
    /// └── Продажи (ParentId = null - корень)
    ///     └── B2B продажи (ParentId = ID of "Продажи")
    /// </summary>
    public Guid? ParentId { get; private set; }
}