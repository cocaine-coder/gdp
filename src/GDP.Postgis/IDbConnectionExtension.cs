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
    public static async Task<IEnumerable<Schemata>> QuerySchemata(this IDbConnection connection)
        => await connection.QueryAsync<Schemata>("select * from information_schema.schemata;");

    /// <summary>
    /// 创建 schema
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema">schema名，内部自动小写化</param>
    /// <returns></returns>
    public static async Task CreateSchema(this IDbConnection connection, string schema)
        => await connection.ExecuteAsync("create schema [if not exists] @schema", new { schema = schema.ToLower() });

    /// <summary>
    /// 获取数据库中所有的表
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Table>> QueryTables(this IDbConnection connection, string? schema = default)
        => await connection.QueryAsync<Table>(
            "select * from information_schema.tables " +
            "where @schema is null or table_schema = @schema;",
            new { schema });

    /// <summary>
    /// 获取数据库中所有带有空间数据列的table
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Table>> QueryGeoTables(this IDbConnection connection, string? schema = default)
        => await connection.QueryAsync<Table>(
            "select * from information_schema.tables tb " +
            "where (@schema is null or table_schema = @schema) and " +
                  "(select count(*) from information_schema.columns col where col.table_name = tb.table_name and col.udt_name = 'geometry') > 0;",
            new { schema });

    /// <summary>
    /// 获取表结构
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns></returns>
    public static async Task<IEnumerable<Column>> QueryColumns(this IDbConnection connection, string schema, string table)
        => await connection.QueryAsync<Column>(
            "select * from information_schema.columns " +
            "where table_schema = @schema and table_name = @table",
            new { schema, table });
}