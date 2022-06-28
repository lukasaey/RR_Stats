using System.Data;
using Dapper;
using MySql.Data.MySqlClient;

namespace DataAccess;

public class DataAccessor : IDataAccess
{
    private readonly string connectionString;

    public DataAccessor(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<List<T>> QueryAsync<T, TU>(string sql, TU parameters)
    {
        using (IDbConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            var rows = await connection.QueryAsync<T>(sql, parameters);
            return rows.ToList();
        }
    }

    public Task Execute<T>(string sql, T parameters)
    {
        using IDbConnection connection = new MySqlConnection(connectionString);
        connection.Open();

        return connection.ExecuteAsync(sql, parameters);
    }

}