using RulesWorkbench.Contracts;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class BusinessUnitScanLoopTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenNonOkBatchIsEncountered_ReturnsThatReportAndStopMessage()
        {
            var batches = new[]
            {
                CreateBatch("11111111-1111-1111-1111-111111111111"),
                CreateBatch("22222222-2222-2222-2222-222222222222"),
                CreateBatch("33333333-3333-3333-3333-333333333333"),
            };
            var callCount = 0;

            var result = await BusinessUnitScanLoop.ExecuteAsync(
                batches,
                "Admiralty",
                "Admiralty (12)",
                isUnboundedScan: false,
                async (batchId, selectedBusinessUnitName, cancellationToken) =>
                {
                    callCount++;
                    await Task.CompletedTask;

                    return callCount switch
                    {
                        1 => CreateRunResult(batchId, selectedBusinessUnitName, RuleCheckerStatus.Ok),
                        2 => CreateRunResult(batchId, selectedBusinessUnitName, RuleCheckerStatus.Warning),
                        _ => throw new InvalidOperationException("The scan should stop at the first non-OK batch."),
                    };
                },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.Batch.BatchId.ShouldBe("22222222-2222-2222-2222-222222222222");
            result.InfoMessage.ShouldBe("Scan stopped at batch 2 of 3 for business unit Admiralty (12).");
            callCount.ShouldBe(2);
        }

        [Fact]
        public async Task ExecuteAsync_WhenUnboundedScanCompletesWithoutFailures_ReturnsSuccessMessage()
        {
            var batches = new[]
            {
                CreateBatch("11111111-1111-1111-1111-111111111111"),
                CreateBatch("22222222-2222-2222-2222-222222222222"),
            };
            var callCount = 0;

            var result = await BusinessUnitScanLoop.ExecuteAsync(
                batches,
                "Admiralty",
                "Admiralty (12)",
                isUnboundedScan: true,
                async (batchId, selectedBusinessUnitName, cancellationToken) =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    return CreateRunResult(batchId, selectedBusinessUnitName, RuleCheckerStatus.Ok);
                },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldBeNull();
            result.InfoMessage.ShouldBe("Scanned all 2 batch(es) for business unit Admiralty (12). No failing batches were found.");
            callCount.ShouldBe(2);
        }

        [Fact]
        public async Task ExecuteAsync_WhenBoundedScanCompletesWithoutFailures_ReturnsBoundedSuccessMessage()
        {
            var batches = new[]
            {
                CreateBatch("11111111-1111-1111-1111-111111111111"),
                CreateBatch("22222222-2222-2222-2222-222222222222"),
            };

            var result = await BusinessUnitScanLoop.ExecuteAsync(
                batches,
                "Admiralty",
                "Admiralty (12)",
                isUnboundedScan: false,
                (batchId, selectedBusinessUnitName, cancellationToken) => Task.FromResult(CreateRunResult(batchId, selectedBusinessUnitName, RuleCheckerStatus.Ok)),
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldBeNull();
            result.InfoMessage.ShouldBe("Checked 2 batch(es) for business unit Admiralty (12). No failing batches were found.");
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoBatchesAreAvailable_ReturnsNoBatchesMessage()
        {
            var result = await BusinessUnitScanLoop.ExecuteAsync(
                Array.Empty<BatchScanBatchDto>(),
                "Admiralty",
                "Admiralty (12)",
                isUnboundedScan: true,
                (batchId, selectedBusinessUnitName, cancellationToken) => Task.FromResult(CreateRunResult(batchId, selectedBusinessUnitName, RuleCheckerStatus.Ok)),
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldBeNull();
            result.InfoMessage.ShouldBe("No batches were found for business unit Admiralty (12).");
        }

        [Fact]
        public async Task ExecuteAsync_WhenBatchCheckFails_ReturnsFailure()
        {
            var batches = new[]
            {
                CreateBatch("11111111-1111-1111-1111-111111111111"),
            };

            var result = await BusinessUnitScanLoop.ExecuteAsync(
                batches,
                "Admiralty",
                "Admiralty (12)",
                isUnboundedScan: false,
                (batchId, selectedBusinessUnitName, cancellationToken) => Task.FromResult(RuleCheckerRunResultDto.Failure("Checker failed.")),
                CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Checker failed.");
        }

        private static BatchScanBatchDto CreateBatch(string batchId)
        {
            return new BatchScanBatchDto
            {
                BatchId = Guid.Parse(batchId),
                CreatedOn = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            };
        }

        private static RuleCheckerRunResultDto CreateRunResult(string batchId, string businessUnitName, RuleCheckerStatus status)
        {
            return RuleCheckerRunResultDto.Success(new RuleCheckerReportDto
            {
                Batch = new RuleCheckerBatchSummaryDto
                {
                    BatchId = batchId,
                    BusinessUnitName = businessUnitName,
                },
                Status = status,
            });
        }
    }
}
