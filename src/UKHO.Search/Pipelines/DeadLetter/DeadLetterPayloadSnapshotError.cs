namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterPayloadSnapshotError
    {
        public required string ExceptionType { get; init; }

        public required string ExceptionMessage { get; init; }
    }
}
