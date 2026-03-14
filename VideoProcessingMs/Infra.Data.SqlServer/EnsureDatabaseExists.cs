using Microsoft.Data.SqlClient;

namespace Infra.Data.SqlServer
{
    public static class DatabaseInitializer
    {
        public static bool EnsureDatabaseExists(string connectionString, string dbName)
        {
            Console.WriteLine("Verificando existência da base de dados...");

            if (string.IsNullOrWhiteSpace(dbName))
                throw new ArgumentException("Nome do banco de dados não informado.", nameof(dbName));

            bool dbExists = false;

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master",
                CommandTimeout = 60
            };

            using (var connection = new SqlConnection(builder.ToString()))
            {
                connection.Open();

                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = @"
                    SELECT CASE
                        WHEN DB_ID(@DbName) IS NULL THEN 0
                        ELSE 1
                    END";
                checkCmd.Parameters.AddWithValue("@DbName", dbName);

                dbExists = (int)checkCmd.ExecuteScalar() == 1;

                if (!dbExists)
                {
                    using var createCmd = connection.CreateCommand();
                    createCmd.CommandText = $"CREATE DATABASE [{dbName}];";
                    createCmd.ExecuteNonQuery();
                }

                connection.ChangeDatabase(dbName);

                string assemblyDir = Path.GetDirectoryName(typeof(DatabaseInitializer).Assembly.Location)!;
                string scriptPath = Path.Combine(assemblyDir, "schema.sql");
                string script = File.ReadAllText(scriptPath);

                var batches = script.Split(
                    new[] { "\r\nGO\r\n", "\nGO\n", "\rGO\r" },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var batch in batches)
                {
                    if (!string.IsNullOrWhiteSpace(batch))
                    {
                        var batchCommand = connection.CreateCommand();
                        batchCommand.CommandText = batch;
                        batchCommand.ExecuteNonQuery();
                    }
                }
            }

            return dbExists;
        }
    }
}
