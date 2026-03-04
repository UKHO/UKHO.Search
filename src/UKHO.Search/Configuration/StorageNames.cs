namespace UKHO.Search.Configuration
{
    public static class StorageNames
    {
        // This constant is duplicated in `UKHO.Search.FileShareEmulator` so that project can be built in isolation
        // (including via Docker) without taking a cross-project reference. Keep both definitions in sync.
        public const string FileShareEmulatorDatabase = "fileshare-emulator-db";
    }
}