using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace QueryServiceHost.Components
{
    public class LoggedErrorBoundary : ErrorBoundary
    {
        [Inject]
        public ILogger<LoggedErrorBoundary> Logger { get; set; } = default!;

        protected override Task OnErrorAsync(Exception exception)
        {
            Logger.LogError(exception, "Unhandled exception rendering component");
            return Task.CompletedTask;
        }
    }
}
