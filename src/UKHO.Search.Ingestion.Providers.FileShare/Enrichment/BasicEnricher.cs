using System.Globalization;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public sealed class BasicEnricher : IIngestionEnricher
    {
        public int Ordinal => 10;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            var properties = request.AddItem?.Properties ?? request.UpdateItem?.Properties;
            if (properties is null || properties.Count == 0)
            {
                return Task.CompletedTask;
            }

            foreach (var property in properties)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (property is null)
                {
                    continue;
                }

                var values = GetValues(property.Value);

                foreach (var value in values)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    document.AddKeyword(value);
                    document.AddFacetValue(property.Name, value);
                }
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<string?> GetValues(object? raw)
        {
            if (raw is null)
            {
                yield break;
            }

            if (raw is string s)
            {
                yield return s;
                yield break;
            }

            if (raw is string[] arr)
            {
                foreach (var item in arr)
                {
                    yield return item;
                }

                yield break;
            }

            if (raw is Array anyArray)
            {
                foreach (var item in anyArray)
                {
                    yield return ConvertToString(item);
                }

                yield break;
            }

            yield return ConvertToString(raw);
        }

        private static string? ConvertToString(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s)
            {
                return s;
            }

            if (value is Uri uri)
            {
                return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
            }

            if (value is DateTimeOffset dto)
            {
                return dto.ToString("O", CultureInfo.InvariantCulture);
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}