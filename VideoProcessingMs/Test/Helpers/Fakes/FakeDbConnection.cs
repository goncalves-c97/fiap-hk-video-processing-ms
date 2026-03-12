using Core.Interfaces;

namespace Test.Helpers.Fakes;

public sealed class FakeDbConnection : IDbConnection
{
    public List<(string Table, Dictionary<string, object> Values)> Inserts { get; } = new();
    public List<(string Table, Dictionary<string, object> Values, string IdColumn, int ReturnedId)> InsertAndReturnIds { get; } = new();
    public List<(string Table, Dictionary<string, object> Values, string WhereClause, object? WhereParams)> Updates { get; } = new();
    public List<(string Table, string WhereClause, object? WhereParams)> Deletes { get; } = new();
    public List<string> RawSqlExecutions { get; } = new();

    public Func<string, string, object?, object?>? SearchFirstOrDefaultHandler { get; set; }
    public Func<string, object?, IEnumerable<object>>? SearchByParametersHandler { get; set; }
    public Func<string, string[]?, IEnumerable<object>>? ListAllHandler { get; set; }

    public int NextInsertId { get; set; } = 1;

    public Task<int> InsertAsync(string table, Dictionary<string, object> values)
    {
        Inserts.Add((table, values));
        return Task.FromResult(1);
    }

    public Task<int> InsertAndReturnIdAsync(string tableName, Dictionary<string, object> values, string idColumn = "id")
    {
        var id = NextInsertId++;
        InsertAndReturnIds.Add((tableName, values, idColumn, id));
        return Task.FromResult(id);
    }

    public Task<int> UpdateAsync(string table, Dictionary<string, object> values, string whereClause, object whereParams = null)
    {
        Updates.Add((table, values, whereClause, whereParams));
        return Task.FromResult(1);
    }

    public Task<int> DeleteAsync(string table, string whereClause, object whereParams = null)
    {
        Deletes.Add((table, whereClause, whereParams));
        return Task.FromResult(1);
    }

    public Task<T?> SearchFirstOrDefaultByParametersAsync<T>(string table, string whereClause, object whereParams = null)
    {
        var obj = SearchFirstOrDefaultHandler?.Invoke(table, whereClause, whereParams);
        return Task.FromResult((T?)obj);
    }

    public Task<IEnumerable<T>> SearchByParametersAsync<T>(string table, string whereClause, object whereParams = null)
    {
        var objs = SearchByParametersHandler?.Invoke(whereClause, whereParams) ?? Enumerable.Empty<object>();
        return Task.FromResult(objs.Cast<T>());
    }

    public Task<IEnumerable<T>> ListAllAsync<T>(string table, string[] columns = null)
    {
        var objs = ListAllHandler?.Invoke(table, columns) ?? Enumerable.Empty<object>();
        return Task.FromResult(objs.Cast<T>());
    }

    public Task<int> ExecuteRawSql(string rawSql)
    {
        RawSqlExecutions.Add(rawSql);
        return Task.FromResult(1);
    }

    public Task<IEnumerable<TReturn>> QueryAsync<T1, T2, T3, T4, T5, TReturn>(string sql, Func<T1, T2, T3, T4, T5, TReturn> map, object? param, string splitOn)
    => Task.FromResult(Enumerable.Empty<TReturn>());
}
