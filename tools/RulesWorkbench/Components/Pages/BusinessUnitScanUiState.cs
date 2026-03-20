using RulesWorkbench.Contracts;

namespace RulesWorkbench.Components.Pages
{
    public sealed record BusinessUnitScanUiState
    {
        public bool IsScanButtonDisabled { get; init; }

        public bool IsScanAllButtonDisabled { get; init; }

        public bool IsMaxRowsDisabled { get; init; }

        public string ScanButtonText { get; init; } = "Scan";

        public string ScanAllButtonText { get; init; } = "Scan All";

        public static BusinessUnitScanUiState Create(
            string? selectedBusinessUnitIdText,
            bool isSingleBatchRunning,
            bool isLoadingBusinessUnits,
            BusinessUnitScanMode? activeScanMode)
        {
            var hasSelection = !string.IsNullOrWhiteSpace(selectedBusinessUnitIdText);
            var isAnyBusinessUnitScanRunning = activeScanMode.HasValue;
            var isDisabled = isSingleBatchRunning || isLoadingBusinessUnits || isAnyBusinessUnitScanRunning || !hasSelection;

            return new BusinessUnitScanUiState
            {
                IsScanButtonDisabled = isDisabled,
                IsScanAllButtonDisabled = isDisabled,
                IsMaxRowsDisabled = activeScanMode is BusinessUnitScanMode.Unbounded,
                ScanButtonText = activeScanMode is BusinessUnitScanMode.Bounded ? "Scanning..." : "Scan",
                ScanAllButtonText = activeScanMode is BusinessUnitScanMode.Unbounded ? "Scanning..." : "Scan All",
            };
        }
    }
}
