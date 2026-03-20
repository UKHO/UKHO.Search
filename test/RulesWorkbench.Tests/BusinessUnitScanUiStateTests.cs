using RulesWorkbench.Components.Pages;
using RulesWorkbench.Contracts;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class BusinessUnitScanUiStateTests
    {
        [Fact]
        public void Create_WhenNoScanIsRunning_UsesIdleButtonTextAndLeavesMaxRowsEnabled()
        {
            var state = BusinessUnitScanUiState.Create("12", isSingleBatchRunning: false, isLoadingBusinessUnits: false, activeScanMode: null);

            state.IsScanButtonDisabled.ShouldBeFalse();
            state.IsScanAllButtonDisabled.ShouldBeFalse();
            state.IsMaxRowsDisabled.ShouldBeFalse();
            state.ScanButtonText.ShouldBe("Scan");
            state.ScanAllButtonText.ShouldBe("Scan All");
        }

        [Fact]
        public void Create_WhenBoundedScanIsRunning_DisablesBothButtonsButLeavesMaxRowsEnabled()
        {
            var state = BusinessUnitScanUiState.Create("12", isSingleBatchRunning: false, isLoadingBusinessUnits: false, activeScanMode: BusinessUnitScanMode.Bounded);

            state.IsScanButtonDisabled.ShouldBeTrue();
            state.IsScanAllButtonDisabled.ShouldBeTrue();
            state.IsMaxRowsDisabled.ShouldBeFalse();
            state.ScanButtonText.ShouldBe("Scanning...");
            state.ScanAllButtonText.ShouldBe("Scan All");
        }

        [Fact]
        public void Create_WhenUnboundedScanIsRunning_DisablesBothButtonsAndMaxRows()
        {
            var state = BusinessUnitScanUiState.Create("12", isSingleBatchRunning: false, isLoadingBusinessUnits: false, activeScanMode: BusinessUnitScanMode.Unbounded);

            state.IsScanButtonDisabled.ShouldBeTrue();
            state.IsScanAllButtonDisabled.ShouldBeTrue();
            state.IsMaxRowsDisabled.ShouldBeTrue();
            state.ScanButtonText.ShouldBe("Scan");
            state.ScanAllButtonText.ShouldBe("Scanning...");
        }

        [Fact]
        public void Create_WhenBusinessUnitSelectionIsMissing_DisablesBothButtons()
        {
            var state = BusinessUnitScanUiState.Create(string.Empty, isSingleBatchRunning: false, isLoadingBusinessUnits: false, activeScanMode: null);

            state.IsScanButtonDisabled.ShouldBeTrue();
            state.IsScanAllButtonDisabled.ShouldBeTrue();
        }

        [Fact]
        public void Create_WhenSingleBatchCheckIsRunning_DisablesBothButtons()
        {
            var state = BusinessUnitScanUiState.Create("12", isSingleBatchRunning: true, isLoadingBusinessUnits: false, activeScanMode: null);

            state.IsScanButtonDisabled.ShouldBeTrue();
            state.IsScanAllButtonDisabled.ShouldBeTrue();
            state.IsMaxRowsDisabled.ShouldBeFalse();
        }
    }
}
