namespace DataAccess;

public class TestDataAccessor : IDataAccess
{
    public Task Execute<T>(string sql, T parameters)
    {
        Console.WriteLine($"executed: {sql} with params {parameters}");
        return Task.CompletedTask;
    }

    public Task<List<T>> QueryAsync<T, TU>(string sql, TU parameters)
    {
        Console.WriteLine($"queried: {sql} with params {parameters}");
        return Task.FromResult(new List<T>());
    }
}
