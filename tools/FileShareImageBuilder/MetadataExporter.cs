using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using UKHO.Search.Configuration;

namespace FileShareImageBuilder;

public sealed class MetadataExporter
{
    public async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        var env = ConfigurationReader.GetEnvironmentName();
        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var binDirectory = Path.Combine(dataImagePath, "bin");

        Directory.CreateDirectory(binDirectory);

        var targetConnectionString =
            ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);
        var targetDbName = await GetDatabaseNameAsync(targetConnectionString, cancellationToken).ConfigureAwait(false);

        if (!string.Equals(targetDbName, StorageNames.FileShareEmulatorDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to export: connected to unexpected target database '{targetDbName}'. Expected '{StorageNames.FileShareEmulatorDatabase}'.");

        var bacpacPath = Path.Combine(binDirectory, $"{env}.bacpac");

        if (File.Exists(bacpacPath)) File.Delete(bacpacPath);

        Console.WriteLine($"[MetadataExporter] Exporting bacpac to: {bacpacPath}");
        // DacFx export is synchronous, so run on a background thread to keep async flow.
        var exportService = new DacServices(targetConnectionString);

        await Task.Run(() => exportService.ExportBacpac(bacpacPath, targetDbName!), cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"[MetadataExporter] Export complete: {bacpacPath}");
    }

    private static async Task<string?> GetDatabaseNameAsync(string sqlConnectionString,
        CancellationToken cancellationToken)
    {
        await using var sqlConnection = new SqlConnection(sqlConnectionString);
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var dbNameCmd = sqlConnection.CreateCommand();

        dbNameCmd.CommandType = CommandType.Text;
        dbNameCmd.CommandTimeout = 30;
        dbNameCmd.CommandText = "SELECT DB_NAME();";

        return (await dbNameCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))?.ToString();
    }
}