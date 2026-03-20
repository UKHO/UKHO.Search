using RulesWorkbench.Contracts;

namespace RulesWorkbench.Services
{
    public static class BusinessUnitScanLoop
    {
        public static async Task<BusinessUnitScanExecutionResult> ExecuteAsync(
            IReadOnlyList<BatchScanBatchDto> batches,
            string businessUnitName,
            string businessUnitDisplayName,
            bool isUnboundedScan,
            Func<string, string, CancellationToken, Task<RuleCheckerRunResultDto>> checkBatchAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(batches);
            ArgumentException.ThrowIfNullOrWhiteSpace(businessUnitName);
            ArgumentException.ThrowIfNullOrWhiteSpace(businessUnitDisplayName);
            ArgumentNullException.ThrowIfNull(checkBatchAsync);

            if (batches.Count == 0)
            {
                return BusinessUnitScanExecutionResult.Success($"No batches were found for business unit {businessUnitDisplayName}.");
            }

            for (var index = 0; index < batches.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = batches[index];
                var result = await checkBatchAsync(batch.BatchId.ToString("D"), businessUnitName, cancellationToken)
                    .ConfigureAwait(false);

                if (!result.IsSuccess || result.Report is null)
                {
                    return BusinessUnitScanExecutionResult.Failure(result.ErrorMessage ?? $"Checker failed while scanning batch '{batch.BatchId:D}'.");
                }

                if (result.Report.Status is not RuleCheckerStatus.Ok)
                {
                    return BusinessUnitScanExecutionResult.Success(
                        $"Scan stopped at batch {index + 1} of {batches.Count} for business unit {businessUnitDisplayName}.",
                        result.Report);
                }
            }

            var infoMessage = isUnboundedScan
                ? $"Scanned all {batches.Count} batch(es) for business unit {businessUnitDisplayName}. No failing batches were found."
                : $"Checked {batches.Count} batch(es) for business unit {businessUnitDisplayName}. No failing batches were found.";

            return BusinessUnitScanExecutionResult.Success(infoMessage);
        }
    }
}
