namespace FileShareImageLoader.Configuration
{
    public static class StorageNames
    {
        // Duplicated from `UKHO.Search.Configuration.StorageNames` so this project can be built in isolation
        // (e.g. via `Dockerfile`) without taking a cross-project reference.
        public const string FileShareEmulatorDatabase = "fileshare-emulator-db";
    }
}