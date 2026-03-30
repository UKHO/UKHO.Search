namespace UKHO.Workbench.Services.Shell
{
    /// <summary>
    /// Carries a user-safe shell notification raised by a hosted tool or by Workbench command handling.
    /// </summary>
    public class WorkbenchNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="severity">The shell notification severity expressed as a simple string value such as <c>info</c> or <c>warning</c>.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer explanatory detail shown to the user.</param>
        public WorkbenchNotificationEventArgs(string severity, string summary, string detail)
        {
            // Notification payloads stay intentionally simple because the host owns how they are presented to the user.
            ArgumentException.ThrowIfNullOrWhiteSpace(severity);
            ArgumentException.ThrowIfNullOrWhiteSpace(summary);
            ArgumentException.ThrowIfNullOrWhiteSpace(detail);

            Severity = severity;
            Summary = summary;
            Detail = detail;
        }

        /// <summary>
        /// Gets the shell notification severity expressed as a simple string value such as <c>info</c> or <c>warning</c>.
        /// </summary>
        public string Severity { get; }

        /// <summary>
        /// Gets the short summary shown to the user.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Gets the longer explanatory detail shown to the user.
        /// </summary>
        public string Detail { get; }
    }
}
