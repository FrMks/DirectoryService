using System.Data.Common;

namespace Shared.Database;

public interface IDbConnectionFactory
{
    DbConnection GetDbConnection();
}