using Shouldly;
using Xunit;

namespace UKHO.Workbench.Layout.Tests
{
    /// <summary>
    /// Verifies the unified resize notification payload shape used by splitter-enabled Workbench grids.
    /// </summary>
    public class GridResizeNotificationShould
    {
        /// <summary>
        /// Ensures the notification preserves the 1-based indices and adjacent-track payload scope for column activity.
        /// </summary>
        [Fact]
        public void PreserveOneBasedTrackNumbersForColumnResizeActivity()
        {
            // Arrange a notification payload that mirrors a column splitter drag update.
            var notification = new GridResizeNotification(
                GridResizeDirection.Column,
                2,
                1,
                3,
                240,
                680,
                "240px 4px 680px");

            // Assert that the unified payload keeps the authored 1-based track numbers and resolved template string intact.
            notification.Direction.ShouldBe(GridResizeDirection.Column);
            notification.SplitterTrackIndex.ShouldBe(2);
            notification.PreviousTrackIndex.ShouldBe(1);
            notification.NextTrackIndex.ShouldBe(3);
            notification.PreviousTrackSizeInPixels.ShouldBe(240);
            notification.NextTrackSizeInPixels.ShouldBe(680);
            notification.GridTemplate.ShouldBe("240px 4px 680px");
        }

        /// <summary>
        /// Ensures the notification preserves the 1-based indices and adjacent-track payload scope for row activity.
        /// </summary>
        [Fact]
        public void PreserveOneBasedTrackNumbersForRowResizeActivity()
        {
            // Arrange a notification payload that mirrors a row splitter drag update.
            var notification = new GridResizeNotification(
                GridResizeDirection.Row,
                2,
                1,
                3,
                120,
                236,
                "120px 4px 236px");

            // Assert that the unified payload keeps the authored 1-based track numbers and resolved template string intact.
            notification.Direction.ShouldBe(GridResizeDirection.Row);
            notification.SplitterTrackIndex.ShouldBe(2);
            notification.PreviousTrackIndex.ShouldBe(1);
            notification.NextTrackIndex.ShouldBe(3);
            notification.PreviousTrackSizeInPixels.ShouldBe(120);
            notification.NextTrackSizeInPixels.ShouldBe(236);
            notification.GridTemplate.ShouldBe("120px 4px 236px");
        }
    }
}
