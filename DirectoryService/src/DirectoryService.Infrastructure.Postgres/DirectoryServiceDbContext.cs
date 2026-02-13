using System.Data.Common;
using DirectoryService.Application.Database;
using DirectoryService.Domain;
using DirectoryService.Domain.Department;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Positions;
using Microsoft.EntityFrameworkCore;
using Shared.Database;

namespace DirectoryService.Infrastructure.Postgres;

public class DirectoryServiceDbContext : DbContext, IReadDbContext, IDbConnectionFactory
{
    private readonly string _connectionString;

    public DirectoryServiceDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString); // Используй Npgsql в качестве бд
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("ltree");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DirectoryServiceDbContext).Assembly);
    }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<DepartmentLocation> DepartmentLocations => Set<DepartmentLocation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<DepartmentPosition> DepartmentPositions => Set<DepartmentPosition>();

    public IQueryable<Location> LocationsRead => Set<Location>().AsQueryable().AsNoTracking();

    public IQueryable<DepartmentLocation> DepartmentLocationsRead => Set<DepartmentLocation>().AsQueryable().AsNoTracking();
    public IQueryable<Department> DepartmentsRead => Set<Department>().AsQueryable().AsNoTracking();

    public DbConnection GetDbConnection() => Database.GetDbConnection();
}