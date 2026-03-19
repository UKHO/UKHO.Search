namespace FileShareEmulator.Services
{
    public sealed record BusinessUnitOptionDto
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string DisplayName => $"{Name} ({Id})";
    }
}
