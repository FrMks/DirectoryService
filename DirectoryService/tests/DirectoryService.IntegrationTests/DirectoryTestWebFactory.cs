using Microsoft.AspNetCore.Mvc.Testing;
using Program = DirectoryService.Presentation.Program;

namespace DirectoryService.IntegrationTests;

public class DirectoryTestWebFactory : WebApplicationFactory<Program>
{
}