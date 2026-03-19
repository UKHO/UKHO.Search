namespace FileShareEmulator.Services
{
    public sealed record IndexBusinessUnitProgress
    {
        public int BusinessUnitId { get; init; }

        public string BusinessUnitName { get; init; } = string.Empty;

        public int SubmittedCount { get; init; }

        public int TotalCandidateCount { get; init; }

        public string Message => $"Indexing business unit '{BusinessUnitName}' ({BusinessUnitId}): submitted {SubmittedCount:N0} of {TotalCandidateCount:N0} batch(es).";
    }
}
