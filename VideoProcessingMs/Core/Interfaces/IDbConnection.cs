
namespace Core.Interfaces
{
    public interface IDbConnection
    {
        Task<int> InsertAsync(string table, Dictionary<string, object> values);
        Task<int> InsertAndReturnIdAsync(string tableName, Dictionary<string, object> values, string idColumn = "id");
        Task<int> UpdateAsync(string table, Dictionary<string, object> values, string whereClause, object whereParams = null);
        Task<int> DeleteAsync(string table, string whereClause, object whereParams = null);
        Task<T?> SearchFirstOrDefaultByParametersAsync<T>(string table, string whereClause, object whereParams = null);
        Task<IEnumerable<T>> SearchByParametersAsync<T>(string table, string whereClause, object whereParams = null);
        Task<IEnumerable<T>> ListAllAsync<T>(string table, string[] columns = null);
        Task<int> ExecuteRawSql(string rawSql);
        Task<IEnumerable<TReturn>> QueryAsync<T1, T2, T3, T4, T5, TReturn>(string sql, Func<T1, T2, T3, T4, T5, TReturn> map, object? param, string splitOn);
    }
}
