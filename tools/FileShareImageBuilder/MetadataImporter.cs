using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using UKHO.Search.Configuration;

namespace FileShareImageBuilder;

public class MetadataImporter
{
    private readonly string _sourceConnectionString;

    public MetadataImporter(SqlConnection targetDatabase)
    {
        _sourceConnectionString = ConfigurationReader.GetSourceDatabaseConnectionString();
        _ = targetDatabase;
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var targetConnectionString =
                ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

            var bacpacDirectory = ConfigurationReader.GetDataImagePath();
            Directory.CreateDirectory(bacpacDirectory);
            var bacpacPath = Path.Combine(bacpacDirectory, "metadata.bacpac");
            await ExportAndImportBacpacAsync(_sourceConnectionString, targetConnectionString, bacpacPath,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    private static async Task ExportAndImportBacpacAsync(
        string sourceConnectionString,
        string targetConnectionString,
        string bacpacPath,
        CancellationToken cancellationToken)
    {
        if (File.Exists(bacpacPath)) File.Delete(bacpacPath);

        var sourceDbName = await GetDatabaseNameAsync(sourceConnectionString, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(sourceDbName))
            throw new InvalidOperationException("Could not determine source database name.");

        var targetDbName = await GetDatabaseNameAsync(targetConnectionString, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(targetDbName, StorageNames.FileShareEmulatorDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to import: connected to unexpected target database '{targetDbName}'. Expected '{StorageNames.FileShareEmulatorDatabase}'.");

        var exportService = new DacServices(sourceConnectionString);
        exportService.ProgressChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Message)) Console.WriteLine($"{args.Message}");
        };
        await Task.Run(() => exportService.ExportBacpac(bacpacPath, sourceDbName), cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"Export complete: {bacpacPath}");

        Console.WriteLine($"Dropping and recreating target database {targetDbName}...");
        await DropAndRecreateDatabaseAsync(targetConnectionString, targetDbName!, cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine("Target database reset.");

        Console.WriteLine($"Importing bacpac into {targetDbName}...");
        var importService = new DacServices(targetConnectionString);
        importService.ProgressChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Message)) Console.WriteLine($"{args.Message}");
        };

        await using (var bacpacStream = File.OpenRead(bacpacPath))
        {
            using var bacpac = BacPackage.Load(bacpacStream);
            await Task.Run(() => importService.ImportBacpac(bacpac, targetDbName!), cancellationToken)
                .ConfigureAwait(false);
        }

        Console.WriteLine("Import complete");

        // Preserve disk space: the source bacpac is no longer needed once the target DB has been populated.
        try
        {
            for (var attempt = 1; attempt <= 5; attempt++)
                try
                {
                    File.Delete(bacpacPath);
                    Console.WriteLine($"[MetadataImporter] Deleted source bacpac: {bacpacPath}");
                    break;
                }
                catch when (attempt < 5)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken).ConfigureAwait(false);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[MetadataImporter] Failed to delete source bacpac '{bacpacPath}': {ex.GetType().Name}: {ex.Message}");
        }
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

    private static async Task DropAndRecreateDatabaseAsync(
        string targetConnectionString,
        string targetDatabaseName,
        CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(targetConnectionString);
        builder.InitialCatalog = "master";

        await using var masterConnection = new SqlConnection(builder.ConnectionString);
        await masterConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var escapedDbName = EscapeSqlIdentifier(targetDatabaseName);
        var dropAndCreateSql = $@"
IF DB_ID(N'{targetDatabaseName.Replace("'", "''", StringComparison.Ordinal)}') IS NOT NULL
BEGIN
    ALTER DATABASE {escapedDbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE {escapedDbName};
END;

CREATE DATABASE {escapedDbName};";

        await using var dropAndCreateCmd = masterConnection.CreateCommand();
        dropAndCreateCmd.CommandType = CommandType.Text;
        dropAndCreateCmd.CommandTimeout = 0;
        dropAndCreateCmd.CommandText = dropAndCreateSql;
        await dropAndCreateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeSqlIdentifier(string identifier)
    {
        // Uses bracket quoting and escapes closing brackets per T-SQL rules.
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private sealed class ProgressDataReader : IDataReader
    {
        private readonly SqlDataReader _inner;
        private readonly Action<long> _onRow;
        private long _sinceLast;

        public ProgressDataReader(SqlDataReader inner, Action<long> onRow)
        {
            _inner = inner;
            _onRow = onRow;
        }

        public bool Read()
        {
            var hasRow = _inner.Read();
            if (hasRow)
            {
                _sinceLast++;
                if (_sinceLast >= 1000)
                {
                    _onRow(_sinceLast);
                    _sinceLast = 0;
                }
            }
            else if (_sinceLast > 0)
            {
                _onRow(_sinceLast);
                _sinceLast = 0;
            }

            return hasRow;
        }

        public int FieldCount => _inner.FieldCount;
        public object this[int i] => _inner[i];
        public object this[string name] => _inner[name];

        public void Close()
        {
            _inner.Close();
        }

        public DataTable? GetSchemaTable()
        {
            return _inner.GetSchemaTable();
        }

        public bool NextResult()
        {
            return _inner.NextResult();
        }

        public int Depth => _inner.Depth;
        public bool IsClosed => _inner.IsClosed;
        public int RecordsAffected => _inner.RecordsAffected;

        public void Dispose()
        {
            _inner.Dispose();
        }

        public bool GetBoolean(int i)
        {
            return _inner.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return _inner.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        {
            return _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return _inner.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        {
            return _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return _inner.GetData(i);
        }

        public string GetDataTypeName(int i)
        {
            return _inner.GetDataTypeName(i);
        }

        public DateTime GetDateTime(int i)
        {
            return _inner.GetDateTime(i);
        }

        public decimal GetDecimal(int i)
        {
            return _inner.GetDecimal(i);
        }

        public double GetDouble(int i)
        {
            return _inner.GetDouble(i);
        }

        public Type GetFieldType(int i)
        {
            return _inner.GetFieldType(i);
        }

        public float GetFloat(int i)
        {
            return _inner.GetFloat(i);
        }

        public Guid GetGuid(int i)
        {
            return _inner.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return _inner.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return _inner.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return _inner.GetInt64(i);
        }

        public string GetName(int i)
        {
            return _inner.GetName(i);
        }

        public int GetOrdinal(string name)
        {
            return _inner.GetOrdinal(name);
        }

        public string GetString(int i)
        {
            return _inner.GetString(i);
        }

        public object GetValue(int i)
        {
            return _inner.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return _inner.GetValues(values);
        }

        public bool IsDBNull(int i)
        {
            return _inner.IsDBNull(i);
        }
    }
}