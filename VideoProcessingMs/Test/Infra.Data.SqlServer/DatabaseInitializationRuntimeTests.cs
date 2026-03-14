using Infra.Data.SqlServer;
using Microsoft.Data.SqlClient;

namespace Test.Infra.Data.SqlServer;

public class DatabaseInitializationRuntimeTests
{
    [Fact]
    public void CreateConnectionStringBuilder_ShouldPopulateBuilderFromConnectionString()
    {
        var runtime = new DatabaseInitializerRuntime();

        var builder = runtime.CreateConnectionStringBuilder("Server=my-server;Database=my-db;User Id=sa;Password=pass;");

        Assert.Equal("my-server", builder.DataSource);
        Assert.Equal("my-db", builder.InitialCatalog);
    }

    [Fact]
    public void ReadAllText_ShouldReturnFileContents()
    {
        var runtime = new DatabaseInitializerRuntime();
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "schema-content");

            var content = runtime.ReadAllText(path);

            Assert.Equal("schema-content", content);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CreateConnection_CreateCommand_AndDispose_ShouldWorkWithWrappedSqlTypes()
    {
        var runtime = new DatabaseInitializerRuntime();

        using var connection = runtime.CreateConnection("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;");
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT 1";

        Assert.Equal("SELECT 1", command.CommandText);
    }

    [Fact]
    public void WrappedCommand_ExecuteMethods_OnClosedConnection_ShouldReachUnderlyingSqlCommand()
    {
        using var sqlConnection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;");
        using var wrappedConnection = new SqlDatabaseInitializerConnection(sqlConnection);
        using var command = wrappedConnection.CreateCommand();

        command.CommandText = "SELECT 1";

        Assert.ThrowsAny<InvalidOperationException>(() => command.ExecuteScalar());
        Assert.ThrowsAny<InvalidOperationException>(() => command.ExecuteNonQuery());
    }
}
