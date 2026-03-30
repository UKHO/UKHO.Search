using Radzen;

namespace WorkbenchHost.Services
{
    /// <summary>
    /// Represents a user-safe startup notification that the Workbench shell should surface after initial render.
    /// </summary>
    public class WorkbenchStartupNotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchStartupNotification"/> class.
        /// </summary>
        /// <param name="severity">The Radzen severity that determines how the notification should be presented.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer safe detail shown to the user.</param>
        public WorkbenchStartupNotification(NotificationSeverity severity, string summary, string detail)
        {
            // Startup notifications carry only safe user-facing text so operational failures can be surfaced without leaking implementation detail.
            ArgumentException.ThrowIfNullOrWhiteSpace(summary);
            ArgumentException.ThrowIfNullOrWhiteSpace(detail);

            Severity = severity;
            Summary = summary;
            Detail = detail;
        }

        /// <summary>
        /// Gets the Radzen severity that determines how the notification should be presented.
        /// </summary>
        public NotificationSeverity Severity { get; }

        /// <summary>
        /// Gets the short summary shown to the user.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Gets the longer safe detail shown to the user.
        /// </summary>
        public string Detail { get; }
    }
}
