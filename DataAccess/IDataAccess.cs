namespace DataAccess;

public interface IDataAccess
{
    List<T> Query<T, TU>(string sql, TU parameters, string connectionString);
    Task<List<T>> QueryAsync<T, TU>(string sql, TU parameters, string connectionString);
    Task Execute<T>(string sql, T parameters, string connectionString);
}