namespace FileShareEmulator.Services
{
    public sealed record IndexBusinessUnitResult
    {
        public bool Succeeded { get; init; }

        public int BusinessUnitId { get; init; }

        public string BusinessUnitName { get; init; } = string.Empty;

        public int TotalCandidateCount { get; init; }

        public int SubmittedCount { get; init; }

        public string? FailureReason { get; init; }

        public bool HasNoBatches => Succeeded && TotalCandidateCount == 0;

        public bool IsPartialFailure => !Succeeded && SubmittedCount > 0;

        public static IndexBusinessUnitResult Success(int businessUnitId, string businessUnitName, int totalCandidateCount, int submittedCount)
        {
            return new IndexBusinessUnitResult
            {
                Succeeded = true,
                BusinessUnitId = businessUnitId,
                BusinessUnitName = businessUnitName,
                TotalCandidateCount = totalCandidateCount,
                SubmittedCount = submittedCount,
            };
        }

        public static IndexBusinessUnitResult ZeroResults(int businessUnitId, string businessUnitName)
        {
            return new IndexBusinessUnitResult
            {
                Succeeded = true,
                BusinessUnitId = businessUnitId,
                BusinessUnitName = businessUnitName,
            };
        }

        public static IndexBusinessUnitResult Failure(int businessUnitId, string businessUnitName, int totalCandidateCount, int submittedCount, string failureReason)
        {
            return new IndexBusinessUnitResult
            {
                BusinessUnitId = businessUnitId,
                BusinessUnitName = businessUnitName,
                TotalCandidateCount = totalCandidateCount,
                SubmittedCount = submittedCount,
                FailureReason = failureReason,
            };
        }
    }
}
