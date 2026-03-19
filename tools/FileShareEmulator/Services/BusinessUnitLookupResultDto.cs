namespace FileShareEmulator.Services
{
    public sealed record BusinessUnitLookupResultDto
    {
        public bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public IReadOnlyList<BusinessUnitOptionDto> BusinessUnits { get; init; } = Array.Empty<BusinessUnitOptionDto>();

        public static BusinessUnitLookupResultDto Success(IReadOnlyList<BusinessUnitOptionDto> businessUnits)
        {
            return new BusinessUnitLookupResultDto
            {
                IsSuccess = true,
                BusinessUnits = businessUnits,
            };
        }

        public static BusinessUnitLookupResultDto Failure(string errorMessage)
        {
            return new BusinessUnitLookupResultDto
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
            };
        }
    }
}
