using Dapper;
using GDP.Postgis.Model;
using System.Data;

namespace GDP.Postgis.Query;

public class DBFramework
{
    private readonly IDbConnection dbConnection;

    public DBFramework(IDbConnection dbConnection)
    {
        this.dbConnection = dbConnection;
    }

    public async Task<IEnumerable<Schemata>> QuerySchemata() => await dbConnection.QueryAsync<Schemata>("select * from information_schema.schemata;");
}