namespace FileShareImageBuilder;

public sealed class BinCleaner
{
    public Task CleanAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var binDirectory = Path.Combine(dataImagePath, "bin");

        if (!Directory.Exists(binDirectory)) return Task.CompletedTask;

        Console.WriteLine($"[BinCleaner] Deleting bin directory: {binDirectory}");
        Directory.Delete(binDirectory, true);
        Console.WriteLine("[BinCleaner] Bin directory deleted.");

        return Task.CompletedTask;
    }
}