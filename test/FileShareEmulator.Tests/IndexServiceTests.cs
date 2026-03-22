using FileShareEmulator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace FileShareEmulator.Tests
{
    public sealed class IndexServiceTests
    {
        [Fact]
        public async Task IndexBusinessUnitBatchesAsync_WhenBatchesExist_ReturnsSuccessAndMarksAllSubmittedBatches()
        {
            var businessUnitId = 42;
            const string businessUnitName = "AVCS";
            var batchIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var submittedBatchIds = new List<Guid>();
            var markedBatchIds = new List<Guid>();

            var result = await IndexService.IndexBusinessUnitBatchesAsync(
                businessUnitId,
                businessUnitName,
                batchIds,
                (batchId, _) =>
                {
                    submittedBatchIds.Add(batchId);
                    return Task.CompletedTask;
                },
                (batchId, _) =>
                {
                    markedBatchIds.Add(batchId);
                    return Task.CompletedTask;
                },
                NullLogger<IndexService>.Instance,
                progress: null,
                progressInterval: 25,
                cancellationToken: CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.HasNoBatches.ShouldBeFalse();
            result.BusinessUnitId.ShouldBe(businessUnitId);
            result.BusinessUnitName.ShouldBe(businessUnitName);
            result.TotalCandidateCount.ShouldBe(batchIds.Length);
            result.SubmittedCount.ShouldBe(batchIds.Length);
            result.FailureReason.ShouldBeNull();
            submittedBatchIds.ShouldBe(batchIds);
            markedBatchIds.ShouldBe(batchIds);
        }

        [Fact]
        public async Task IndexBusinessUnitBatchesAsync_WhenNoBatchesExist_ReturnsZeroResults()
        {
            var result = await IndexService.IndexBusinessUnitBatchesAsync(
                42,
                "AVCS",
                Array.Empty<Guid>(),
                static (_, _) => Task.CompletedTask,
                static (_, _) => Task.CompletedTask,
                NullLogger<IndexService>.Instance,
                progress: null,
                progressInterval: 25,
                cancellationToken: CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.HasNoBatches.ShouldBeTrue();
            result.TotalCandidateCount.ShouldBe(0);
            result.SubmittedCount.ShouldBe(0);
        }

        [Fact]
        public async Task IndexBusinessUnitBatchesAsync_WhenBusinessUnitSelectionIsInvalid_ReturnsFailure()
        {
            var result = await IndexService.IndexBusinessUnitBatchesAsync(
                0,
                string.Empty,
                Array.Empty<Guid>(),
                static (_, _) => Task.CompletedTask,
                static (_, _) => Task.CompletedTask,
                NullLogger<IndexService>.Instance,
                progress: null,
                progressInterval: 25,
                cancellationToken: CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.FailureReason.ShouldBe("Business unit selection is required.");
        }

        [Fact]
        public async Task IndexBusinessUnitBatchesAsync_WhenSubmissionFailsAfterPartialSuccess_ReturnsPartialFailure()
        {
            var batchIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var submittedBatchIds = new List<Guid>();
            var markedBatchIds = new List<Guid>();

            var result = await IndexService.IndexBusinessUnitBatchesAsync(
                42,
                "AVCS",
                batchIds,
                (batchId, _) =>
                {
                    submittedBatchIds.Add(batchId);

                    if (submittedBatchIds.Count == 2)
                    {
                        throw new InvalidOperationException("Queue submit failed.");
                    }

                    return Task.CompletedTask;
                },
                (batchId, _) =>
                {
                    markedBatchIds.Add(batchId);
                    return Task.CompletedTask;
                },
                NullLogger<IndexService>.Instance,
                progress: null,
                progressInterval: 25,
                cancellationToken: CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.IsPartialFailure.ShouldBeTrue();
            result.TotalCandidateCount.ShouldBe(3);
            result.SubmittedCount.ShouldBe(1);
            result.FailureReason.ShouldBe("Queue submit failed.");
            submittedBatchIds.ShouldBe(batchIds.Take(2).ToArray());
            markedBatchIds.ShouldBe([batchIds[0]]);
        }

        [Fact]
        public async Task IndexBusinessUnitBatchesAsync_WhenProgressIsRequested_ReportsInitialAndPeriodicProgress()
        {
            var batchIds = Enumerable.Range(0, 5)
                                     .Select(_ => Guid.NewGuid())
                                     .ToArray();
            var progressUpdates = new List<IndexBusinessUnitProgress>();

            var result = await IndexService.IndexBusinessUnitBatchesAsync(
                42,
                "AVCS",
                batchIds,
                static (_, _) => Task.CompletedTask,
                static (_, _) => Task.CompletedTask,
                NullLogger<IndexService>.Instance,
                new SynchronousProgress<IndexBusinessUnitProgress>(progress => progressUpdates.Add(progress)),
                progressInterval: 2,
                cancellationToken: CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            progressUpdates.Count.ShouldBe(3);
            progressUpdates[0].SubmittedCount.ShouldBe(0);
            progressUpdates[0].TotalCandidateCount.ShouldBe(5);
            progressUpdates[0].Message.ShouldContain("AVCS");
            progressUpdates[0].Message.ShouldContain("42");
            progressUpdates[1].SubmittedCount.ShouldBe(2);
            progressUpdates[2].SubmittedCount.ShouldBe(4);
        }

        private sealed class SynchronousProgress<T> : IProgress<T>
        {
            private readonly Action<T> _report;

            public SynchronousProgress(Action<T> report)
            {
                _report = report;
            }

            public void Report(T value)
            {
                _report(value);
            }
        }
    }
}
