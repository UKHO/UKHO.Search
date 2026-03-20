namespace RulesWorkbench.Contracts
{
    public sealed record BusinessUnitScanExecutionResult
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public string? InfoMessage { get; init; }

        public RuleCheckerReportDto? Report { get; init; }

        public static BusinessUnitScanExecutionResult Success(string infoMessage, RuleCheckerReportDto? report = null)
        {
            return new BusinessUnitScanExecutionResult
            {
                IsSuccess = true,
                InfoMessage = infoMessage,
                Report = report,
            };
        }

        public static BusinessUnitScanExecutionResult Failure(string errorMessage)
        {
            return new BusinessUnitScanExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
