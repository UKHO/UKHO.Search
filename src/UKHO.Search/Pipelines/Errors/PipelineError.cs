namespace UKHO.Search.Pipelines.Errors
{
    public sealed class PipelineError
    {
        public required PipelineErrorCategory Category { get; init; }

        public required string Code { get; init; }

        public required string Message { get; init; }

        public string? ExceptionType { get; init; }

        public string? ExceptionMessage { get; init; }

        public string? StackTrace { get; init; }

        public required bool IsTransient { get; init; }

        public required DateTimeOffset OccurredAtUtc { get; init; }

        public required string NodeName { get; init; }

        public required IReadOnlyDictionary<string, string> Details { get; init; }
    }
}