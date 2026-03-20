namespace RulesWorkbench.Services
{
    public static class BatchScanQueries
    {
        public static string GetBoundedQuery()
        {
            return @"SELECT TOP (@maxRows) [Id], [CreatedOn]
FROM [Batch]
WHERE [BusinessUnitId] = @businessUnitId
ORDER BY [CreatedOn] ASC, [Id] ASC;";
        }

        public static string GetUnboundedQuery()
        {
            return @"SELECT [Id], [CreatedOn]
FROM [Batch]
WHERE [BusinessUnitId] = @businessUnitId
ORDER BY [CreatedOn] ASC, [Id] ASC;";
        }
    }
}
