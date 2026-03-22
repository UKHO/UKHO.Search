namespace UKHO.Search.Tests.Common
{
    public static class SampleDataFileLocator
    {
        public static string GetPath(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                // Walk up from the test output folder until the repository test/sample-data folder is found.
                var candidate = Path.Combine(directory.FullName, "test", "sample-data", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Unable to locate sample data file '{fileName}' under 'test/sample-data'.", fileName);
        }
    }
}
