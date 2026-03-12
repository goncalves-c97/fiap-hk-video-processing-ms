using System;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient; // or Microsoft.Data.SqlClient

namespace Infra.Data.SqlServer
{
    public class SqlServerConnection : IDbConnection, Core.Interfaces.IDbConnection
    {
        private readonly SqlConnection _sqlConnection;

        public SqlServerConnection(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
        }

        public string ConnectionString { get => _sqlConnection.ConnectionString; set => _sqlConnection.ConnectionString = value; }
        public int ConnectionTimeout => _sqlConnection.ConnectionTimeout;
        public string Database => _sqlConnection.Database;
        public ConnectionState State => _sqlConnection.State;

        public IDbTransaction BeginTransaction() => _sqlConnection.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _sqlConnection.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _sqlConnection.ChangeDatabase(databaseName);
        public void Close() => _sqlConnection.Close();
        public IDbCommand CreateCommand() => _sqlConnection.CreateCommand();
        public void Dispose() => _sqlConnection.Dispose();
        public void Open() => _sqlConnection.Open();

        // ------------------------------
        // Private Helper
        // ------------------------------
        private async Task<T> WithConnectionAsync<T>(Func<SqlConnection, Task<T>> action)
        {
            bool shouldClose = false;
            try
            {
                if (_sqlConnection.State != ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync();
                    shouldClose = true;
                }

                return await action(_sqlConnection);
            }
            finally
            {
                if (shouldClose)
                {
                    _sqlConnection.Close();
                }
            }
        }

        private async Task<int> ExecuteAsync(string sql, object? param = null)
            => await WithConnectionAsync(conn => conn.ExecuteAsync(sql, param));

        private async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
            => await WithConnectionAsync(conn => conn.QueryAsync<T>(sql, param));

        // ------------------------------
        // Async Generic Data Access Methods
        // ------------------------------

        public async Task<int> InsertAsync(string table, Dictionary<string, object> values)
        {
            var columns = string.Join(", ", values.Keys);
            var paramNames = string.Join(", ", values.Keys.Select(k => "@" + k));
            string query = $"INSERT INTO {table} ({columns}) VALUES ({paramNames})";
            return await ExecuteAsync(query, values);
        }

        public async Task<int> InsertAndReturnIdAsync(string table, Dictionary<string, object> values, string idColumn = "id")
        {
            var columnNames = string.Join(", ", values.Keys);
            var paramNames = string.Join(", ", values.Keys.Select(k => "@" + k));

            var sql = $@"
            INSERT INTO {table} ({columnNames})
            OUTPUT INSERTED.{idColumn}
            VALUES ({paramNames});
        ";

            return await _sqlConnection.ExecuteScalarAsync<int>(sql, values);
        }

        public async Task<int> UpdateAsync(string table, Dictionary<string, object> values, string whereClause, object whereParams = null)
        {
            var setClause = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));
            string query = $"UPDATE {table} SET {setClause} WHERE {whereClause}";

            var parameters = new DynamicParameters(values);
            if (whereParams != null)
                parameters.AddDynamicParams(whereParams);

            return await ExecuteAsync(query, parameters);
        }

        public async Task<int> DeleteAsync(string table, string whereClause, object whereParams = null)
        {
            string query = $"DELETE FROM {table} WHERE {whereClause}";
            return await ExecuteAsync(query, whereParams);
        }

        public async Task<T?> SearchFirstOrDefaultByParametersAsync<T>(string table, string whereClause, object whereParams = null)
        {
            string query = $"SELECT TOP(1) * FROM {table} WHERE {whereClause}";
            return (await QueryAsync<T>(query, whereParams)).FirstOrDefault();
        }

        public async Task<IEnumerable<T>> SearchByParametersAsync<T>(string table, string whereClause, object whereParams = null)
        {
            string query = $"SELECT * FROM {table} WHERE {whereClause}";
            return await QueryAsync<T>(query, whereParams);
        }

        public async Task<IEnumerable<T>> ListAllAsync<T>(string table, string[] columns = null)
        {
            var columnList = columns != null && columns.Length > 0 ? string.Join(", ", columns) : "*";
            string query = $"SELECT {columnList} FROM {table}";
            return await QueryAsync<T>(query);
        }

        /// <summary>
        /// Executa uma instrução SQL bruta e retorna o número de linhas afetadas.
        /// </summary>
        /// <param name="rawSql"></param>
        /// <returns></returns>
        public async Task<int> ExecuteRawSql(string rawSql)
        {
            return await ExecuteAsync(rawSql);
        }

        public async Task<IEnumerable<TReturn>> QueryAsync<T1, T2, T3, T4, T5, TReturn>(string sql, Func<T1, T2, T3, T4, T5, TReturn> map, object? param, string splitOn)
        {
            return await WithConnectionAsync(conn =>
                conn.QueryAsync(sql, map, param, splitOn: splitOn)
            );
        }
    }
}