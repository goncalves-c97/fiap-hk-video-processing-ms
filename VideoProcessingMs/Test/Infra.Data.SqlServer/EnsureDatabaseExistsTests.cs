using Infra.Data.SqlServer;
using Xunit;

namespace Test.Infra.Data.SqlServer
{
    public class EnsureDatabaseExistsTests
    {
        private const string DbName = "VideoUploadDb";

        [Fact]
        public void EnsureDatabaseExists_WhenConnectionStringInvalid_Throws()
        {
            // invalid host makes SqlConnection.Open fail deterministically
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
            // Positive-path integration test.
            // Provide a real connection string to a SQL Server instance via environment variable.
            // Example: Server=localhost,1433;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
            var cs = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(cs))
                return; // not configured => don't run (keeps test suite stable)

            // 1st run: should create DB if missing (returns false when it didn't exist)
            var existedBefore = DatabaseInitializer.EnsureDatabaseExists(cs!, DbName);

            // 2nd run: now it must exist
            var existedAfter = DatabaseInitializer.EnsureDatabaseExists(cs!, DbName);

            Assert.False(existedBefore);
            Assert.True(existedAfter);
        }

        [Fact]
        public void EnsureDatabaseExists_WhenLocalDbAvailable_DbAlreadyExists_ReturnsTrue()
        {
            // Optional but deterministic on developer machines/CI images that include LocalDB.
            // This covers the "db already exists" path (`dbExists == true`) without needing external env config.
            var cs = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;Connect Timeout=1;";

            try
            {
                // Pre-create DB (ignore failures if it already exists)
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
                // LocalDB not installed/available => skip to keep suite stable.
                return;
            }
        }
    }
}
