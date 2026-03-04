namespace UKHO.Search.AppHost;

public enum RunMode
{
    /// <summary>
    ///     Import a data image created using Export mode
    /// </summary>
    Import,

    /// <summary>
    ///     Create a data image from a File Share service installation
    /// </summary>
    Export,

    /// <summary>
    ///     Run the search services
    /// </summary>
    Services
}