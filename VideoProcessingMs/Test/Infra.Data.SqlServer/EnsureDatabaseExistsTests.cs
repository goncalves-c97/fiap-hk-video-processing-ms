using Infra.Data.SqlServer;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Test.Infra.Data.SqlServer
{
    public class EnsureDatabaseExistsTests
    {
        private const string DbName = "VideoUploadDb";

        [Fact]
        public void EnsureDatabaseExists_WhenConnectionStringInvalid_Throws()
        {
            Assert.ThrowsAny<Exception>(() => DatabaseInitializer.EnsureDatabaseExists(
                "Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;Connect Timeout=1;",
                DbName));
        }

        [Fact]
        public void EnsureDatabaseExists_WhenConnectionStringMalformed_Throws()
        {
            Assert.ThrowsAny<Exception>(() => DatabaseInitializer.EnsureDatabaseExists(
                "not-a-connection-string",
                DbName));
        }

        [Fact]
        public void EnsureDatabaseExists_WhenSqlServerAvailable_CreatesDbAndReturnsFalseThenTrue()
        {
            var cs = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(cs))
                return;

            var existedBefore = DatabaseInitializer.EnsureDatabaseExists(cs!, DbName);
            var existedAfter = DatabaseInitializer.EnsureDatabaseExists(cs!, DbName);

            Assert.False(existedBefore);
            Assert.True(existedAfter);
        }

        [Fact]
        public void EnsureDatabaseExists_WhenLocalDbAvailable_DbAlreadyExists_ReturnsTrue()
        {
            var cs = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;Connect Timeout=1;";

            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(cs))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "IF DB_ID('VideoUploadDb') IS NULL CREATE DATABASE VideoUploadDb;";
                    cmd.ExecuteNonQuery();
                }

                var existed = DatabaseInitializer.EnsureDatabaseExists(cs, DbName);
                Assert.True(existed);
            }
            catch
            {
                return;
            }
        }

        [Fact]
        public void EnsureDatabaseExists_WhenDatabaseDoesNotExist_CreatesDatabaseAndExecutesSchemaBatches()
        {
            var runtime = new FakeDatabaseInitializerRuntime(0, "CREATE TABLE one;\nGO\n   \nGO\nCREATE TABLE two;");

            var existed = DatabaseInitializer.EnsureDatabaseExists("Server=fake;Database=app;", DbName, runtime);

            Assert.False(existed);
            Assert.True(runtime.Connection.OpenCalled);
            Assert.Equal("master", runtime.Builder.InitialCatalog);
            Assert.Equal(60, runtime.Builder.CommandTimeout);
            Assert.Equal(4, runtime.Connection.ExecutedCommands.Count);
            Assert.Contains(runtime.Connection.ExecutedCommands, command => command.Contains("DB_ID('VideoUploadDb')"));
            Assert.Contains("CREATE DATABASE VideoUploadDb;", runtime.Connection.ExecutedCommands);
            Assert.Contains("CREATE TABLE one;", runtime.Connection.ExecutedCommands);
            Assert.Contains("CREATE TABLE two;", runtime.Connection.ExecutedCommands);
            Assert.NotNull(runtime.ReadPath);
            Assert.EndsWith("schema.sql", runtime.ReadPath);
        }

        [Fact]
        public void EnsureDatabaseExists_WhenDatabaseAlreadyExists_ReturnsTrueWithoutCreatingDatabase()
        {
            var runtime = new FakeDatabaseInitializerRuntime(1, "CREATE TABLE ignored;");

            var existed = DatabaseInitializer.EnsureDatabaseExists("Server=fake;Database=app;", DbName, runtime);

            Assert.True(existed);
            Assert.True(runtime.Connection.OpenCalled);
            Assert.Single(runtime.Connection.ExecutedCommands);
            Assert.Contains("DB_ID('VideoUploadDb')", runtime.Connection.ExecutedCommands[0]);
            Assert.Null(runtime.ReadPath);
        }

        private sealed class FakeDatabaseInitializerRuntime : IDatabaseInitializerRuntime
        {
            public FakeDatabaseInitializerRuntime(int scalarResult, string scriptContent)
            {
                Connection = new FakeDatabaseInitializerConnection(scalarResult);
                ScriptContent = scriptContent;
            }

            public FakeDatabaseInitializerConnection Connection { get; }
            public SqlConnectionStringBuilder Builder { get; private set; } = new();
            public string ScriptContent { get; }
            public string? ReadPath { get; private set; }

            public SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            {
                Builder = new SqlConnectionStringBuilder(connectionString);
                return Builder;
            }

            public IDatabaseInitializerConnection CreateConnection(string connectionString)
            {
                Connection.ConnectionString = connectionString;
                return Connection;
            }

            public string ReadAllText(string path)
            {
                ReadPath = path;
                return ScriptContent;
            }
        }

        private sealed class FakeDatabaseInitializerConnection : IDatabaseInitializerConnection
        {
            private readonly int _scalarResult;
            private bool _isFirstCommand = true;

            public FakeDatabaseInitializerConnection(int scalarResult)
            {
                _scalarResult = scalarResult;
            }

            public string? ConnectionString { get; set; }
            public bool OpenCalled { get; private set; }
            public List<string> ExecutedCommands { get; } = new();

            public void Open()
            {
                OpenCalled = true;
            }

            public IDatabaseInitializerCommand CreateCommand()
            {
                var command = new FakeDatabaseInitializerCommand(ExecutedCommands, _isFirstCommand ? _scalarResult : null);
                _isFirstCommand = false;
                return command;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeDatabaseInitializerCommand : IDatabaseInitializerCommand
        {
            private readonly List<string> _executedCommands;
            private readonly int? _scalarResult;

            public FakeDatabaseInitializerCommand(List<string> executedCommands, int? scalarResult)
            {
                _executedCommands = executedCommands;
                _scalarResult = scalarResult;
            }

            public string CommandText { get; set; } = string.Empty;

            public object? ExecuteScalar()
            {
                _executedCommands.Add(CommandText);
                return _scalarResult ?? 0;
            }

            public int ExecuteNonQuery()
            {
                _executedCommands.Add(CommandText);
                return 1;
            }

            public void Dispose()
            {
            }
        }
    }
}
