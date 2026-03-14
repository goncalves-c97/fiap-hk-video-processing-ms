using Microsoft.Data.SqlClient;

namespace Infra.Data.SqlServer
{
    internal interface IDatabaseInitializerRuntime
    {
        SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString);
        IDatabaseInitializerConnection CreateConnection(string connectionString);
        string ReadAllText(string path);
    }

    internal interface IDatabaseInitializerConnection : IDisposable
    {
        void Open();
        IDatabaseInitializerCommand CreateCommand();
    }

    internal interface IDatabaseInitializerCommand : IDisposable
    {
        string CommandText { get; set; }
        object? ExecuteScalar();
        int ExecuteNonQuery();
    }
}
