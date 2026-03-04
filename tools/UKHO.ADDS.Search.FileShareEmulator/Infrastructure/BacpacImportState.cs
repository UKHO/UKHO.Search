namespace UKHO.ADDS.Search.FileShareEmulator.Infrastructure;

public static class BacpacImportState
{
    private static volatile bool _completed;

    public static bool Completed => _completed;

    public static void MarkCompleted()
    {
        _completed = true;
    }
}
