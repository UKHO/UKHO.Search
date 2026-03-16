using System.Collections;
using System.Reflection;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation
{
    internal sealed class IngestionRulesPathResolver : IPathResolver
    {
        public IReadOnlyList<string> Resolve(object payload, string path)
        {
            if (payload is null)
            {
                return Array.Empty<string>();
            }

            if (!IngestionRulesPathParser.TryParse(path, out var steps, out var _))
            {
                // Should be prevented by startup validation. Treat as non-match.
                return Array.Empty<string>();
            }

            var current = new List<object?> { payload };

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];

                if (string.Equals(step.Kind, "propertiesLookup", StringComparison.Ordinal))
                {
                    return ResolvePropertiesLookup(payload, step.Value ?? string.Empty);
                }

                if (string.Equals(step.Kind, "property", StringComparison.Ordinal))
                {
                    var name = step.Value ?? string.Empty;

                    // Special-case: properties.<name> and properties["<name>"] are represented as
                    // property("properties") + propertiesLookup("<name>") by the parser.
                    if (string.Equals(name, "properties", StringComparison.OrdinalIgnoreCase) && i + 1 < steps.Count && string.Equals(steps[i + 1].Kind, "propertiesLookup", StringComparison.Ordinal))
                    {
                        return ResolvePropertiesLookup(payload, steps[i + 1].Value ?? string.Empty);
                    }

                    current = ResolveProperty(current, name);
                    continue;
                }

                if (string.Equals(step.Kind, "wildcard", StringComparison.Ordinal))
                {
                    current = FlattenEnumerable(current);
                    continue;
                }

                return Array.Empty<string>();
            }

            return current.Select(CoerceToString)
                          .Where(s => !string.IsNullOrEmpty(s))
                          .Select(s => s!)
                          .ToArray();
        }

        private static List<object?> ResolveProperty(List<object?> current, string name)
        {
            var next = new List<object?>();

            foreach (var obj in current)
            {
                if (obj is null)
                {
                    continue;
                }

                var type = obj.GetType();
                var prop = FindProperty(type, name);
                if (prop is null)
                {
                    continue;
                }

                var value = prop.GetValue(obj);
                if (value is null)
                {
                    continue;
                }

                next.Add(value);
            }

            return next;
        }

        private static List<object?> FlattenEnumerable(List<object?> current)
        {
            var flattened = new List<object?>();

            foreach (var obj in current)
            {
                if (obj is null || obj is string)
                {
                    continue;
                }

                if (obj is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        flattened.Add(item);
                    }
                }
            }

            return flattened;
        }

        private static IReadOnlyList<string> ResolvePropertiesLookup(object payload, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Array.Empty<string>();
            }

            key = key.Trim().ToLowerInvariant();

            var propertiesProp = FindProperty(payload.GetType(), "properties");
            if (propertiesProp is null)
            {
                return Array.Empty<string>();
            }

            var propertiesValue = propertiesProp.GetValue(payload);
            if (propertiesValue is not IEnumerable enumerable)
            {
                return Array.Empty<string>();
            }

            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                var nameProp = FindProperty(item.GetType(), "name");
                if (nameProp?.GetValue(item) is not string name)
                {
                    continue;
                }

                if (!string.Equals(name, key, StringComparison.Ordinal))
                {
                    continue;
                }

                var valueProp = FindProperty(item.GetType(), "value");
                var val = valueProp?.GetValue(item);
                var s = CoerceToString(val);

                return string.IsNullOrEmpty(s) ? Array.Empty<string>() : new[] { s! };
            }

            return Array.Empty<string>();
        }

        private static PropertyInfo? FindProperty(Type type, string name)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CoerceToString(object? value)
        {
            return value switch
            {
                null => null,
                string s => s,
                var _ => value.ToString()
            };
        }
    }
}