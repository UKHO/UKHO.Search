namespace AppHost.Elastic
{
    public class ElasticsearchSetupAnnotation : IResourceAnnotation
    {
        public required string KibanaAdminUsername { get; init; }
        public required string[] KibanaAdminRoles { get; init; }
        public required string KibanaAdminFullName { get; init; }
    }
}