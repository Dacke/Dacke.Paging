using System.Data;

namespace Paging.Sample.Web.Data;

public interface IDbConnectionFactory {
    IDbConnection CreateConnection();
}