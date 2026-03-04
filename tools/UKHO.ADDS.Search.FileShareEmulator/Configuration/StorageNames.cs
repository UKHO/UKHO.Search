namespace UKHO.ADDS.Search.FileShareEmulator.Configuration
{
    public static class StorageNames
    {
        // Duplicated from `UKHO.ADDS.Search.Configuration.StorageNames` so this project can be built in isolation
        // (e.g. via `Dockerfile`) without taking a cross-project reference.
        public const string FileShareEmulatorDatabase = "fileshare-emulator-db";
    }
}
