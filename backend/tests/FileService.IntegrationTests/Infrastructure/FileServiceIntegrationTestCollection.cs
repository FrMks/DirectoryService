namespace FileService.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FileServiceIntegrationTestCollection : ICollectionFixture<FileServiceTestWebFactory>
{
    public const string Name = "FileService integration tests";
}
