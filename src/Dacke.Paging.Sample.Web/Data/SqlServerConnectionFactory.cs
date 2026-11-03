using Microsoft.Data.SqlClient;
using System.Data;

namespace Dacke.Paging.Sample.Web.Data;

public class SqlServerConnectionFactory(IConfiguration configuration) : IDbConnectionFactory {
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public IDbConnection CreateConnection() {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}