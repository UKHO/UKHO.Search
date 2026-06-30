namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides typed helper entry points for constructing ingestion properties safely.
    /// </summary>
    public static class IngestionPropertyFactory
    {
        /// <summary>
        /// Creates a string property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The string value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty String(string name, string value)
        {
            return Create(name, IngestionPropertyType.String, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// Creates a text property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The text value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Text(string name, string value)
        {
            return Create(name, IngestionPropertyType.Text, value ?? throw new ArgumentNullException(nameof(value)));
        }

        /// <summary>
        /// Creates an integer property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The 64-bit integer value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Integer(string name, long value)
        {
            return Create(name, IngestionPropertyType.Integer, value);
        }

        /// <summary>
        /// Creates a double property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The double-precision value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Double(string name, double value)
        {
            return Create(name, IngestionPropertyType.Double, value);
        }

        /// <summary>
        /// Creates a decimal property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The decimal value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Decimal(string name, decimal value)
        {
            return Create(name, IngestionPropertyType.Decimal, value);
        }

        /// <summary>
        /// Creates a Boolean property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The Boolean value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Boolean(string name, bool value)
        {
            return Create(name, IngestionPropertyType.Boolean, value);
        }

        /// <summary>
        /// Creates a date/time property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The date/time value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty DateTime(string name, DateTimeOffset value)
        {
            return Create(name, IngestionPropertyType.DateTime, value);
        }

        /// <summary>
        /// Creates a time-span property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The duration value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty TimeSpan(string name, TimeSpan value)
        {
            return Create(name, IngestionPropertyType.TimeSpan, value);
        }

        /// <summary>
        /// Creates a GUID property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The GUID value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Guid(string name, Guid value)
        {
            return Create(name, IngestionPropertyType.Guid, value);
        }

        /// <summary>
        /// Creates an absolute URI property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The URI value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty Uri(string name, Uri value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!value.IsAbsoluteUri)
            {
                throw new ArgumentException("The URI value must be absolute.", nameof(value));
            }

            return Create(name, IngestionPropertyType.Uri, value);
        }

        /// <summary>
        /// Creates a string-array property.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// The string-array value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        public static IngestionProperty StringArray(string name, string[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Clone the array so callers cannot mutate the property value after construction.
            return Create(name, IngestionPropertyType.StringArray, value.ToArray());
        }

        /// <summary>
        /// Creates a typed property after validating the shared property name rules.
        /// </summary>
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="type">
        /// The declared contract type.
        /// </param>
        /// <param name="value">
        /// The typed property value.
        /// </param>
        /// <returns>
        /// A typed <see cref="IngestionProperty" /> carrying the supplied value.
        /// </returns>
        private static IngestionProperty Create(string name, IngestionPropertyType type, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            // Keep the helper layer simple: it assigns the canonical type/value pairing and leaves list normalization
            // and duplicate-name enforcement to the existing contract-owned list implementation.
            return new IngestionProperty
            {
                Name = name,
                Type = type,
                Value = value
            };
        }
    }
}