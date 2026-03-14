using Microsoft.Data.SqlClient;

namespace Infra.Data.SqlServer
{
    internal sealed class DatabaseInitializerRuntime : IDatabaseInitializerRuntime
    {
        public SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
        {
            return new SqlConnectionStringBuilder(connectionString);
        }

        public IDatabaseInitializerConnection CreateConnection(string connectionString)
        {
            return new SqlDatabaseInitializerConnection(new SqlConnection(connectionString));
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }

    internal sealed class SqlDatabaseInitializerConnection : IDatabaseInitializerConnection
    {
        private readonly SqlConnection _connection;

        public SqlDatabaseInitializerConnection(SqlConnection connection)
        {
            _connection = connection;
        }

        public void Open()
        {
            _connection.Open();
        }

        public IDatabaseInitializerCommand CreateCommand()
        {
            return new SqlDatabaseInitializerCommand(_connection.CreateCommand());
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    internal sealed class SqlDatabaseInitializerCommand : IDatabaseInitializerCommand
    {
        private readonly SqlCommand _command;

        public SqlDatabaseInitializerCommand(SqlCommand command)
        {
            _command = command;
        }

        public string CommandText
        {
            get => _command.CommandText;
            set => _command.CommandText = value;
        }

        public object? ExecuteScalar()
        {
            return _command.ExecuteScalar();
        }

        public int ExecuteNonQuery()
        {
            return _command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _command.Dispose();
        }
    }
}
