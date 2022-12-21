using Dapper;
using GDP.Postgis.Model;
using System.Data;

namespace GDP.Postgis;

public static class IDbConnectionExtension
{
    /// <summary>
    /// 获取数据库中所有的Schema信息
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Schemata>> QuerySchemataAsync(this IDbConnection connection, IDbTransaction? transaction = null)
        => await connection.QueryAsync<Schemata>("select * from information_schema.schemata;", transaction: transaction);

    /// <summary>
    /// 创建 schema
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema">模式名，内部自动小写化</param>
    /// <returns></returns>
    public static async Task CreateSchemaAsync(this IDbConnection connection, string schema, IDbTransaction? transaction = null)
        => await connection.ExecuteAsync("create schema if not exists @schema", new { schema = schema.ToLower() }, transaction: transaction);

    /// <summary>
    /// 判断schema是否存在
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static async Task<bool> ExistSchemaAsync(this IDbConnection connection, string schema, IDbTransaction? transaction = null)
      => (await connection.QuerySingleAsync<int>("SELECT count(schema_name) FROM information_schema.schemata WHERE schema_name = @schema", new { schema }, transaction: transaction)) > 0;

    /// <summary>
    /// 获取数据库中所有的表
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Table>> QueryTablesAsync(this IDbConnection connection, string? schema = default, IDbTransaction? transaction = null)
        => await connection.QueryAsync<Table>(
            "select * from information_schema.tables " +
            "where @schema is null or table_schema = @schema;",
            new { schema }, transaction: transaction);

    /// <summary>
    /// 获取数据库中所有带有空间数据列的table
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Table>> QueryGeoTablesAsync(this IDbConnection connection, string? schema = default, IDbTransaction? transaction = null)
        => await connection.QueryAsync<Table>(
            "select * from information_schema.tables tb " +
            "where (@schema is null or table_schema = @schema) and " +
                  "(select count(*) from information_schema.columns col where col.table_name = tb.table_name and col.udt_name = 'geometry') > 0;",
            new { schema }, transaction: transaction);

    /// <summary>
    /// 创建一个空间数据表
    /// 将所有的属性全部json化
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="table">表名</param>
    /// <returns></returns>
    public static async Task CreateGeoTableAsync(this IDbConnection connection, string table, IDbTransaction? transaction = null)
        => await connection.ExecuteAsync(
            "create table @table (" +
                "id serial primary key," +
                "geom geometry not null," +
                "properties jsonb not null default '{}'::jsonb)", new { table = table.ToLower() }, transaction: transaction);

    /// <summary>
    /// 判断表是否存在
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="table">表名</param>
    /// <param name="schema">模式名 默认public</param>
    /// <returns></returns>
    public static async Task<bool> ExistTableAsync(this IDbConnection connection, string table, string schema = "public", IDbTransaction? transaction = null)
        => await connection.QuerySingleAsync<bool>($"select to_regclass('${schema}.${table}') is not null", transaction: transaction);

    /// <summary>
    /// 获取表结构
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="table">表名</param>
    /// <param name="schema">模式名 默认public</param>
    /// <returns></returns>
    public static async Task<IEnumerable<Column>> QueryColumnsAsync(this IDbConnection connection, string table, string schema = "public", IDbTransaction? transaction = null)
        => await connection.QueryAsync<Column>(
            "select * from information_schema.columns " +
            "where table_schema = @schema and table_name = @table",
            new { schema, table }, transaction: transaction);

    /// <summary>
    /// 获取mvt数据
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <param name="geomCol">空间数据列名</param>
    /// <param name="idCol">id 列名</param>
    /// <param name="z"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="columns">其他列 eg: name,property</param>
    /// <param name="filter">筛选: where xxx </param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static async Task<byte[]> QueryMvtBufferAsync(this IDbConnection connection, string schema, string table, string geomCol, string idCol, int z, int x, int y, string? columns = null, string? filter = null, IDbTransaction? transaction = null)
    {
        var sql = $@"
            WITH mvt_geom as (
              SELECT
                ST_AsMVTGeom (
                  ST_Transform({geomCol}, 3857),
                  ST_TileEnvelope({z}, {x}, {y})
                ) as geom,
                ${idCol},
                {(columns != null ? $",{columns}" : "")}
              FROM
                {schema}.{table}
              {(filter != null ? $"WHERE {filter}" : "")}
            )
            SELECT ST_AsMVT(mvt_geom.*, '{table}', 4096, 'geom',${idCol}) AS mvt from mvt_geom;";

        return await connection.QuerySingleAsync<byte[]>(sql, transaction: transaction);
    }

    /// <summary>
    /// 获取空间数据(整表) 并使用Geobuf进行编码
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <param name="geomCol">空间数据列名</param>
    /// <param name="idCol">id 列名</param>
    /// <param name="columns">其他列 eg: name,property</param>
    /// <param name="filter">筛选: where xxx </param>
    /// <param name="srid"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static async Task<byte[]> QueryGeoBufferAsync(this IDbConnection connection, string schema, string table, string geomCol, string idCol, string? columns = null, string? filter = null, int srid = 4326, IDbTransaction? transaction = null)
    {
        var sql = $@"SELECT ST_AsGeobuf(q, 'geom')
                          FROM (SELECT
                                  ST_Transform({geomCol}, {srid}) as geom,
                                  {idCol},
                                  {(columns != null ? $", {columns}" : "")}
                                FROM
                                  {schema}.{table}
                                {(filter != null ? $"WHERE {filter}" : "")}
                          ) as q;";

        return await connection.QuerySingleAsync<byte[]>(sql, transaction: transaction);
    }
}