using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Stores ingestion properties while enforcing case-insensitive uniqueness and canonical lower-case names.
    /// </summary>
    public sealed class IngestionPropertyList : IReadOnlyList<IngestionProperty>
    {
        private readonly List<IngestionProperty> _properties;
        private readonly HashSet<string> _seenNames;

        /// <summary>
        /// Initializes an empty property list.
        /// </summary>
        public IngestionPropertyList()
        {
            _properties = new List<IngestionProperty>();
            _seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes the property list from an existing sequence.
        /// </summary>
        /// <param name="properties">
        /// The properties to add to the list.
        /// </param>
        [JsonConstructor]
        public IngestionPropertyList(IEnumerable<IngestionProperty> properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            _properties = new List<IngestionProperty>();
            _seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Normalize and validate each incoming property as it enters the list.
            foreach (var property in properties)
            {
                if (property is null)
                {
                    continue;
                }

                Add(property);
            }
        }

        /// <summary>
        /// Gets the number of properties in the list.
        /// </summary>
        [JsonIgnore]
        public int Count => _properties.Count;

        /// <summary>
        /// Gets the property at the specified zero-based position.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the property to return.
        /// </param>
        /// <returns>
        /// The property at the specified index.
        /// </returns>
        [JsonIgnore]
        public IngestionProperty this[int index] => _properties[index];

        /// <summary>
        /// Returns a generic enumerator over the stored properties.
        /// </summary>
        /// <returns>
        /// An enumerator over the property list.
        /// </returns>
        public IEnumerator<IngestionProperty> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        /// <summary>
        /// Returns a non-generic enumerator over the stored properties.
        /// </summary>
        /// <returns>
        /// A non-generic enumerator over the property list.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a property to the list after validating its name and canonical form.
        /// </summary>
        /// <param name="property">
        /// The property to add.
        /// </param>
        public void Add(IngestionProperty property)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (string.IsNullOrWhiteSpace(property.Name))
            {
                throw new JsonException("IngestionProperty.Name is required.");
            }

            // The queue contract treats property names case-insensitively and persists them in canonical lower-case form.
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