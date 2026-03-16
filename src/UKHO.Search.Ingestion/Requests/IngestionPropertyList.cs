using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed class IngestionPropertyList : IReadOnlyList<IngestionProperty>
    {
        private readonly List<IngestionProperty> _properties;
        private readonly HashSet<string> _seenNames;

        public IngestionPropertyList()
        {
            _properties = new List<IngestionProperty>();
            _seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        [JsonConstructor]
        public IngestionPropertyList(IEnumerable<IngestionProperty> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            _properties = new List<IngestionProperty>();
            _seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in properties)
            {
                if (p is null)
                {
                    continue;
                }

                Add(p);
            }
        }

        [JsonIgnore]
        public int Count => _properties.Count;

        [JsonIgnore]
        public IngestionProperty this[int index] => _properties[index];

        public IEnumerator<IngestionProperty> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(IngestionProperty property)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (string.IsNullOrWhiteSpace(property.Name))
            {
                throw new JsonException("IngestionProperty.Name is required.");
            }

            var canonicalName = property.Name.Trim().ToLowerInvariant();

            if (!_seenNames.Add(canonicalName))
            {
                throw new JsonException($"IngestionPropertyList contains duplicate Name '{property.Name}'. Names are case-insensitive.");
            }

            if (string.Equals(property.Name, canonicalName, StringComparison.Ordinal))
            {
                _properties.Add(property);
                return;
            }

            _properties.Add(property with { Name = canonicalName });
        }
    }
}
