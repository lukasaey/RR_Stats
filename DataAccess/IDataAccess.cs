namespace DataAccess;

public interface IDataAccess
{
    Task<List<T>> QueryAsync<T, TU>(string sql, TU parameters);
    Task Execute<T>(string sql, T parameters);
}