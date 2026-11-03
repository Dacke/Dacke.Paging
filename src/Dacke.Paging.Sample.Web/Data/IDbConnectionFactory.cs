using System.Data;

namespace Dacke.Paging.Sample.Web.Data;

public interface IDbConnectionFactory {
    IDbConnection CreateConnection();
}