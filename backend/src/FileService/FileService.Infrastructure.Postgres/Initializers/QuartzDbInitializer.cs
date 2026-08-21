using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FileService.Infrastructure.Postgres.Initializers;

public class QuartzDbInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<QuartzDbInitializer> _logger;

    public QuartzDbInitializer(IConfiguration configuration, ILogger<QuartzDbInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("FileServiceDb")!;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            string sqlScript = await LoadSqlScriptAsync();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            await using var command = new NpgsqlCommand(sqlScript, connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Quartz tables initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Quartz tables");
            throw;
        }
    }

    private static async Task<string> LoadSqlScriptAsync()
    {
        Assembly assembly = typeof(QuartzDbInitializer).Assembly;
        string resourceName = "FileService.Infrastructure.Postgres.Scripts.quartz_tables.sql";

        await using Stream? stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not fount");
        }

        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }
}
