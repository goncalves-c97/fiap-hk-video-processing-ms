using Microsoft.Data.SqlClient;

namespace Infra.Data.SqlServer
{
    public static class DatabaseInitializer
    {
        public static bool EnsureDatabaseExists(string connectionString, string dbName)
        {
            return EnsureDatabaseExists(connectionString, dbName, new DatabaseInitializerRuntime());
        }

        internal static bool EnsureDatabaseExists(string connectionString, string dbName, IDatabaseInitializerRuntime runtime)
        {
            Console.WriteLine("Verificando existência da base de dados...");

            bool dbExists = false;

            var builder = runtime.CreateConnectionStringBuilder(connectionString);
            builder.InitialCatalog = "master";
            builder.CommandTimeout = 60;

            using (var connection = runtime.CreateConnection(builder.ToString()))
            {
                connection.Open();

                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT CASE 
                        WHEN DB_ID('VideoUploadDb') IS NULL THEN 0 
                        ELSE 1 
                    END";

                dbExists = Convert.ToInt32(checkCmd.ExecuteScalar()) == 1;

                if (!dbExists)
                {
                    using var createCmd = connection.CreateCommand();
                    createCmd.CommandText = "CREATE DATABASE VideoUploadDb;";
                    createCmd.ExecuteNonQuery();

                    string assemblyDir = Path.GetDirectoryName(typeof(DatabaseInitializer).Assembly.Location)!;
                    string scriptPath = Path.Combine(assemblyDir, "schema.sql");
                    string script = runtime.ReadAllText(scriptPath);

                    var batches = script.Split(
                        new[] { "\r\nGO\r\n", "\nGO\n", "\rGO\r" },
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using var batchCommand = connection.CreateCommand();
                            batchCommand.CommandText = batch;
                            batchCommand.ExecuteNonQuery();
                        }
                    }
                }
            }

            return dbExists;
        }
    }
}
