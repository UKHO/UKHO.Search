using Radzen;

namespace WorkbenchHost.Services
{
    /// <summary>
    /// Stores startup notifications until the interactive Workbench shell is ready to present them to the user.
    /// </summary>
    public class WorkbenchStartupNotificationStore
    {
        private readonly List<WorkbenchStartupNotification> _notifications = [];

        /// <summary>
        /// Adds a new startup notification to the pending collection.
        /// </summary>
        /// <param name="severity">The Radzen severity that determines how the notification should be presented.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer safe detail shown to the user.</param>
        public void Add(NotificationSeverity severity, string summary, string detail)
        {
            // Notifications are buffered during startup because the interactive shell is not yet available to render them immediately.
            _notifications.Add(new WorkbenchStartupNotification(severity, summary, detail));
        }

        /// <summary>
        /// Returns the pending notifications and clears the internal store.
        /// </summary>
        /// <returns>The startup notifications that have not yet been presented to the user.</returns>
        public IReadOnlyList<WorkbenchStartupNotification> DequeueAll()
        {
            // The first shell render drains the queue so the same startup failure is not shown repeatedly.
            var notifications = _notifications.ToArray();
            _notifications.Clear();
            return notifications;
        }
    }
}
