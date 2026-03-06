namespace UKHO.Aspire.Configuration.Remote
{
    public class ExternalEndpoint : IExternalEndpoint
    {
        public string ClientId { get; init; } = string.Empty;
        public required string Tag { get; init; }
        public EndpointHostSubstitution Host { get; init; }
        public required Uri Uri { get; init; }

        public string GetDefaultScope()
        {
            return $"{ClientId}/.default";
        }
    }
}