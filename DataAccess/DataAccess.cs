using System.Data;
using Dapper;
using MySql.Data.MySqlClient;

namespace DataAccess;

public class DataAccess : IDataAccess
{
    public List<T> Query<T, TU>(string sql, TU parameters, string connectionString)
    {
        using (IDbConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            var rows = connection.Query<T>(sql, parameters);
            return rows.ToList();
        }
    }
   
    public async Task<List<T>> QueryAsync<T, TU>(string sql, TU parameters, string connectionString)
    {
        using (IDbConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            var rows = await connection.QueryAsync<T>(sql, parameters);
            return rows.ToList();
        }
    }

    public Task Execute<T>(string sql, T parameters, string connectionString)
    {
        using IDbConnection connection = new MySqlConnection(connectionString);
        connection.Open();

        return connection.ExecuteAsync(sql, parameters);
    }

}