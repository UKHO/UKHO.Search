namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides typed property access helpers for indexing payloads.
    /// </summary>
    public static class IndexRequestExtensions
    {
        /// <summary>
        /// Tries to read a string property.
        /// </summary>
        /// <param name="request">
        /// The request to inspect.
        /// </param>
        /// <param name="name">
        /// The property name to match.
        /// </param>
        /// <param name="value">
        /// Receives the string value when the lookup succeeds.
        /// </param>
        /// <returns>
        /// <c>true</c> when the property exists with the expected type; otherwise <c>false</c>.
        /// </returns>
        public static bool TryGetString(this IndexRequest request, string name, out string? value)
        {
            return TryGet(request, name, IngestionPropertyType.String, out value);
        }

        /// <summary>
        /// Tries to read an Int64 property.
        /// </summary>
        public static bool TryGetInt64(this IndexRequest request, string name, out long value)
        {
            if (TryGet(request, name, IngestionPropertyType.Integer, out var rawValue) && rawValue is long longValue)
            {
                value = longValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a double property.
        /// </summary>
        public static bool TryGetDouble(this IndexRequest request, string name, out double value)
        {
            if (TryGet(request, name, IngestionPropertyType.Double, out var rawValue) && rawValue is double doubleValue)
            {
                value = doubleValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a decimal property.
        /// </summary>
        public static bool TryGetDecimal(this IndexRequest request, string name, out decimal value)
        {
            if (TryGet(request, name, IngestionPropertyType.Decimal, out var rawValue) && rawValue is decimal decimalValue)
            {
                value = decimalValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a Boolean property.
        /// </summary>
        public static bool TryGetBoolean(this IndexRequest request, string name, out bool value)
        {
            if (TryGet(request, name, IngestionPropertyType.Boolean, out var rawValue) && rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a <see cref="DateTimeOffset" /> property.
        /// </summary>
        public static bool TryGetDateTimeOffset(this IndexRequest request, string name, out DateTimeOffset value)
        {
            if (TryGet(request, name, IngestionPropertyType.DateTime, out var rawValue) && rawValue is DateTimeOffset dateTimeOffsetValue)
            {
                value = dateTimeOffsetValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a <see cref="TimeSpan" /> property.
        /// </summary>
        public static bool TryGetTimeSpan(this IndexRequest request, string name, out TimeSpan value)
        {
            if (TryGet(request, name, IngestionPropertyType.TimeSpan, out var rawValue) && rawValue is TimeSpan timeSpanValue)
            {
                value = timeSpanValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a GUID property.
        /// </summary>
        public static bool TryGetGuid(this IndexRequest request, string name, out Guid value)
        {
            if (TryGet(request, name, IngestionPropertyType.Guid, out var rawValue) && rawValue is Guid guidValue)
            {
                value = guidValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read a URI property.
        /// </summary>
        public static bool TryGetUri(this IndexRequest request, string name, out Uri? value)
        {
            if (TryGet(request, name, IngestionPropertyType.Uri, out var rawValue) && rawValue is Uri uriValue)
            {
                value = uriValue;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Tries to read a string-array property.
        /// </summary>
        public static bool TryGetStringArray(this IndexRequest request, string name, out string[]? value)
        {
            if (TryGet(request, name, IngestionPropertyType.StringArray, out var rawValue) && rawValue is string[] arrayValue)
            {
                value = arrayValue;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Tries to read a typed property value.
        /// </summary>
        private static bool TryGet<T>(IndexRequest request, string name, IngestionPropertyType type, out T? value)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(name);

            var properties = request.Properties ?? throw new InvalidOperationException("IndexRequest.Properties cannot be null.");

            // Property lookups are case-insensitive because the payload contract normalizes names to lower-case.
            var match = properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is null || match.Type != type)
            {
                value = default;
                return false;
            }

            if (match.Value is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to read an untyped property value while still enforcing the declared property type.
        /// </summary>
        private static bool TryGet(IndexRequest request, string name, IngestionPropertyType type, out object? value)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(name);

            var properties = request.Properties ?? throw new InvalidOperationException("IndexRequest.Properties cannot be null.");

            // Property lookups are case-insensitive because the payload contract normalizes names to lower-case.
            var match = properties.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is null || match.Type != type)
            {
                value = null;
                return false;
            }

            value = match.Value;
            return true;
        }
    }
}