using System.Data;
using Infra.Data.SqlServer;

namespace Test.Infra.Data.SqlServer;

public class SqlServerConnectionTests
{
    private const string DummyConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void SqlServerConnection_Implements_CoreDbConnection_And_SystemDataDbConnection()
    {
        var sut = new SqlServerConnection(DummyConnectionString);

        Assert.IsAssignableFrom<global::Core.Interfaces.IDbConnection>(sut);
        Assert.IsAssignableFrom<IDbConnection>(sut);
    }

    [Fact]
    public void SystemData_IDbConnection_Properties_AreAccessible()
    {
        using var sut = new SqlServerConnection(DummyConnectionString);

        _ = sut.ConnectionString;
        _ = sut.ConnectionTimeout;
        _ = sut.Database;
        _ = sut.State;

        Assert.True(sut.State is ConnectionState.Closed or ConnectionState.Open);
    }

    [Fact]
    public void SystemData_IDbConnection_CreateCommand_DoesNotThrow()
    {
        using var sut = new SqlServerConnection(DummyConnectionString);

        using var cmd = sut.CreateCommand();
        Assert.NotNull(cmd);
    }

    [Fact]
    public void SystemData_IDbConnection_ChangeDatabase_WhenClosed_Throws()
    {
        using var sut = new SqlServerConnection(DummyConnectionString);

        // SqlConnection throws InvalidOperationException if closed
        Assert.Throws<InvalidOperationException>(() => sut.ChangeDatabase("tempdb"));
    }

    [Fact]
    public void SystemData_IDbConnection_BeginTransaction_WhenClosed_Throws()
    {
        using var sut = new SqlServerConnection(DummyConnectionString);

        Assert.Throws<InvalidOperationException>(() => sut.BeginTransaction());
        Assert.Throws<InvalidOperationException>(() => sut.BeginTransaction(IsolationLevel.ReadCommitted));
    }

    [Fact]
    public async Task CoreDbConnection_InsertAsync_WithUnreachableDb_Throws()
    {
        // invalid server => deterministic failure without needing SQL Server running
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.InsertAsync("t", new Dictionary<string, object> { { "a",1 } }));
    }

    [Fact]
    public async Task CoreDbConnection_InsertAndReturnIdAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.InsertAndReturnIdAsync("t", new Dictionary<string, object> { { "a",1 } }));
    }

    [Fact]
    public async Task CoreDbConnection_UpdateAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.UpdateAsync("t", new Dictionary<string, object> { { "a",1 } }, "1=1"));
    }

    [Fact]
    public async Task CoreDbConnection_DeleteAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.DeleteAsync("t", "1=1"));
    }

    [Fact]
    public async Task CoreDbConnection_SearchFirstOrDefaultByParametersAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.SearchFirstOrDefaultByParametersAsync<object>("t", "1=1"));
    }

    [Fact]
    public async Task CoreDbConnection_SearchByParametersAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.SearchByParametersAsync<object>("t", "1=1"));
    }

    [Fact]
    public async Task CoreDbConnection_ListAllAsync_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.ListAllAsync<object>("t"));
    }

    [Fact]
    public async Task CoreDbConnection_ExecuteRawSql_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.ExecuteRawSql("select1"));
    }

    [Fact]
    public async Task CoreDbConnection_QueryAsync_MultiMap_WithUnreachableDb_Throws()
    {
        var sut = new SqlServerConnection("Server=invalid-host;Database=master;User Id=sa;Password=bad;TrustServerCertificate=True;");

        await Assert.ThrowsAnyAsync<Exception>(() => sut.QueryAsync<int, int, int, int, int, int>(
            "select1",
            (a, b, c, d, e) => a,
            param: null,
            splitOn: "id"));
    }
}
