namespace FileShareImageBuilder;

public sealed class ImageBuilder
{
    private readonly BinCleaner _binCleaner;
    private readonly ContentImporter _contentImporter;
    private readonly DataCleaner _dataCleaner;
    private readonly ImageExporter _imageExporter;
    private readonly ImageLoader _imageLoader;
    private readonly MetadataExporter _metadataExporter;
    private readonly MetadataImporter _metadataImporter;

    public ImageBuilder(
        MetadataImporter metadataImporter,
        ContentImporter contentImporter,
        DataCleaner dataCleaner,
        MetadataExporter metadataExporter,
        ImageExporter imageExporter,
        ImageLoader imageLoader,
        BinCleaner binCleaner)
    {
        _metadataImporter = metadataImporter;
        _contentImporter = contentImporter;
        _dataCleaner = dataCleaner;
        _metadataExporter = metadataExporter;
        _imageExporter = imageExporter;
        _imageLoader = imageLoader;
        _binCleaner = binCleaner;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return RunInternalAsync(cancellationToken);
    }

    private async Task RunInternalAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[ImageBuilder] Importing metadata...");
        await _metadataImporter.ImportAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Importing content...");
        await _contentImporter.ImportAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Cleaning local database...");
        await _dataCleaner.CleanAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Exporting metadata (bacpac)...");
        await _metadataExporter.ExportAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Exporting data image (docker)...");
        await _imageExporter.ExportAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Loading data image into local docker...");
        await _imageLoader.LoadAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Cleaning bin directory...");
        await _binCleaner.CleanAsync(cancellationToken).ConfigureAwait(false);

        Console.WriteLine("[ImageBuilder] Done.");
    }
}