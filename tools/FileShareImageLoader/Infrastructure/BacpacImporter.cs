using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace FileShareImageLoader.Infrastructure;

public sealed class BacpacImporter
{
    public async Task EnsureDatabaseSeededAsync(string connectionString, string databaseName, string bacpacPath, CancellationToken cancellationToken)
    {
        Console.WriteLine("[BacpacImporter] Checking for Batch table...");

        if (await BatchTableExistsAsync(connectionString, cancellationToken).ConfigureAwait(false))
        {
            Console.WriteLine("[BacpacImporter] Batch table exists. No import required.");
            await EnsureIndexStatusColumnAsync(connectionString, cancellationToken).ConfigureAwait(false);
            BacpacImportState.MarkCompleted();
            return;
        }

        if (!File.Exists(bacpacPath))
        {
            throw new FileNotFoundException($"Bacpac not found at expected path '{bacpacPath}'.", bacpacPath);
        }

        Console.WriteLine($"[BacpacImporter] Batch table not found. Importing bacpac '{bacpacPath}' into '{databaseName}'...");

        await EnsureDatabaseExistsAsync(connectionString, databaseName, cancellationToken).ConfigureAwait(false);

        await Task.Run(() =>
        {
            using var package = BacPackage.Load(bacpacPath);
            var services = new DacServices(connectionString);
            services.ImportBacpac(package, databaseName);
        }, cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[BacpacImporter] Bacpac import complete.");

        await EnsureIndexStatusColumnAsync(connectionString, cancellationToken).ConfigureAwait(false);
        BacpacImportState.MarkCompleted();
    }

    private static async Task EnsureIndexStatusColumnAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;

        cmd.CommandText = @"
IF COL_LENGTH('[Batch]', 'IndexStatus') IS NULL
BEGIN
    ALTER TABLE [Batch]
        ADD [IndexStatus] INT NOT NULL
            CONSTRAINT [DF_Batch_IndexStatus] DEFAULT (0);

    UPDATE [Batch]
        SET [IndexStatus] = [Status];
END
";

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> BatchTableExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT 1 FROM sys.tables WHERE name = 'Batch';";

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString, string databaseName, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText = $@"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL
BEGIN
    CREATE DATABASE [{databaseName.Replace("]", "]]" )}];
END";

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
