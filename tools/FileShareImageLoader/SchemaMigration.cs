using System.Data;
using Microsoft.Data.SqlClient;

namespace FileShareImageLoader
{
    public sealed class SchemaMigration
    {
        public async Task ApplyAsync(string connectionString, string dataImageName, LocalMetadataImageInfo? imageInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                await EnsureIndexStatusColumnAsync(connectionString, cancellationToken).ConfigureAwait(false);
                await EnsureLocalMetadataTableAsync(connectionString, cancellationToken).ConfigureAwait(false);
                await UpsertLocalMetadataAsync(connectionString, dataImageName, imageInfo, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"[SchemaMigration] Failed: {ex.Message}");
                throw;
            }
        }

        private static async Task EnsureIndexStatusColumnAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"[SchemaMigration] Connected. Database='{connection.Database}' DataSource='{connection.DataSource}'");

            var batchExists = await ObjectExistsAsync(connection, "dbo", "Batch", "U", cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"[SchemaMigration] dbo.Batch exists: {batchExists}");

            var indexStatusExistsBefore = await ColumnExistsAsync(connection, "dbo", "Batch", "IndexStatus", cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"[SchemaMigration] dbo.Batch.IndexStatus exists (before): {indexStatusExistsBefore}");

            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            cmd.CommandText = @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'[dbo].[Batch]')
      AND c.name = N'IndexStatus'
)
BEGIN
    ALTER TABLE [dbo].[Batch]
        ADD [IndexStatus] INT NOT NULL
            CONSTRAINT [DF_Batch_IndexStatus] DEFAULT (0);
END
";

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            var indexStatusExistsAfter = await ColumnExistsAsync(connection, "dbo", "Batch", "IndexStatus", cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"[SchemaMigration] dbo.Batch.IndexStatus exists (after): {indexStatusExistsAfter}");
        }

        private static async Task EnsureLocalMetadataTableAsync(string connectionString, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
IF OBJECT_ID(N'[dbo].[LocalMetadata]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LocalMetadata]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_LocalMetadata] PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Value] NVARCHAR(MAX) NULL
    );

    CREATE UNIQUE INDEX [UX_LocalMetadata_Name] ON [dbo].[LocalMetadata]([Name]);
END
";

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task UpsertLocalMetadataAsync(string connectionString, string dataImageName,
            LocalMetadataImageInfo? imageInfo,
            CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await UpsertAsync(connection, "DataLoadTimeUtc", DateTimeOffset.UtcNow.ToString("O"), cancellationToken)
                .ConfigureAwait(false);
            await UpsertAsync(connection, "DataImageName", dataImageName, cancellationToken).ConfigureAwait(false);

            if (imageInfo is not null)
            {
                if (!string.IsNullOrWhiteSpace(imageInfo.Version))
                {
                    await UpsertAsync(connection, "DataImageVersion", imageInfo.Version, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(imageInfo.Tags))
                {
                    await UpsertAsync(connection, "DataImageTags", imageInfo.Tags, cancellationToken).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(imageInfo.Digest))
                {
                    await UpsertAsync(connection, "DataImageDigest", imageInfo.Digest, cancellationToken).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(imageInfo.SizeBytes))
                {
                    await UpsertAsync(connection, "DataImageSizeBytes", imageInfo.SizeBytes, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(imageInfo.CreatedUtc))
                {
                    await UpsertAsync(connection, "DataImageCreatedUtc", imageInfo.CreatedUtc, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private static async Task UpsertAsync(SqlConnection connection, string name, string value,
            CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
MERGE [dbo].[LocalMetadata] AS target
USING (SELECT @name AS [Name], @value AS [Value]) AS source
ON target.[Name] = source.[Name]
WHEN MATCHED THEN
    UPDATE SET [Value] = source.[Value]
WHEN NOT MATCHED THEN
    INSERT ([Id], [Name], [Value]) VALUES (NEWID(), source.[Name], source.[Value]);";
            cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = name });
            cmd.Parameters.Add(new SqlParameter("@value", SqlDbType.NVarChar) { Value = value });

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> ObjectExistsAsync(SqlConnection connection, string schema, string name, string type,
            CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT 1
FROM sys.objects o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = @schema AND o.name = @name AND o.type = @type;";
            cmd.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema });
            cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = name });
            cmd.Parameters.Add(new SqlParameter("@type", SqlDbType.NChar, 2) { Value = type });

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is not null && result is not DBNull;
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string schema, string table, string column,
            CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT 1
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE s.name = @schema AND o.name = @table AND c.name = @column;";
            cmd.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema });
            cmd.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = table });
            cmd.Parameters.Add(new SqlParameter("@column", SqlDbType.NVarChar, 128) { Value = column });

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is not null && result is not DBNull;
        }
    }
}
