namespace RulesWorkbench.Builder
{
	public sealed record RuleBuilderMappingResult<T>(
		bool IsSupported,
		T? Value,
		string? Reason)
	{
		public static RuleBuilderMappingResult<T> Supported(T value)
		{
			return new RuleBuilderMappingResult<T>(true, value, null);
		}

		public static RuleBuilderMappingResult<T> Unsupported(string reason)
		{
			return new RuleBuilderMappingResult<T>(false, default, reason);
		}
	}
}
