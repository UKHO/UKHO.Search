using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Templating
{
    internal sealed class TemplateContext
    {
        public TemplateContext(object payload, IPathResolver pathResolver, IReadOnlyList<string> val)
        {
            Payload = payload;
            PathResolver = pathResolver;
            Val = val;
        }

        public object Payload { get; }

        public IPathResolver PathResolver { get; }

        public IReadOnlyList<string> Val { get; }
    }
}
