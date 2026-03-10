using System.Collections;
using System.Linq;
using System.Reflection;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Validation
{
    internal sealed class IngestionRulesPathValidator
    {
        private static readonly Type[] _payloadTypes =
        [
            typeof(AddItemRequest),
            typeof(UpdateItemRequest)
        ];

        public bool TryValidate(string path, out string? error)
        {
            error = null;

            if (!IngestionRulesPathParser.TryParse(path, out var steps, out var parseError))
            {
                error = parseError;
                return false;
            }

            foreach (var payloadType in _payloadTypes)
            {
                if (TryValidateAgainstType(payloadType, steps, out _))
                {
                    return true;
                }
            }

            if (!TryValidateAgainstType(_payloadTypes[0], steps, out var typeError))
            {
                error = typeError;
            }
            else
            {
                error = $"Path '{path}' does not resolve on supported payload types.";
            }

            return false;
        }

        private static bool TryValidateAgainstType(Type rootType, List<(string Kind, string? Value)> steps, out string? error)
        {
            error = null;
            var currentType = rootType;

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];

                if (string.Equals(step.Kind, "property", StringComparison.Ordinal))
                {
                    var name = step.Value ?? string.Empty;
                    var prop = FindProperty(currentType, name);
                    if (prop is null)
                    {
                        error = $"Path segment '{name}' does not exist on type '{currentType.Name}'.";
                        return false;
                    }

                    var propType = prop.PropertyType;

                    var hasNext = index + 1 < steps.Count;
                    if (hasNext && IsEnumerableButNotString(propType))
                    {
                        var next = steps[index + 1];
                        if (string.Equals(next.Kind, "propertiesLookup", StringComparison.Ordinal) && string.Equals(name, "properties", StringComparison.OrdinalIgnoreCase))
                        {
                            // Special-case: properties.<name> and properties["<name>"] are allowed and terminal.
                            return true;
                        }

                        if (!string.Equals(next.Kind, "wildcard", StringComparison.Ordinal))
                        {
                            error = $"Collection access must be explicit using [*] when traversing '{name}'.";
                            return false;
                        }
                    }

                    currentType = propType;
                    continue;
                }

                if (string.Equals(step.Kind, "wildcard", StringComparison.Ordinal))
                {
                    if (!IsEnumerableButNotString(currentType))
                    {
                        error = $"Wildcard [*] cannot be applied to non-collection type '{currentType.Name}'.";
                        return false;
                    }

                    currentType = GetEnumerableElementType(currentType);
                    continue;
                }

                if (string.Equals(step.Kind, "propertiesLookup", StringComparison.Ordinal))
                {
                    // The presence of this step is validated syntactically by the parser.
                    return true;
                }

                error = $"Unknown step kind '{step.Kind}'.";
                return false;
            }

            return true;
        }

        private static PropertyInfo? FindProperty(Type type, string name)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsEnumerableButNotString(Type type)
        {
            return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static Type GetEnumerableElementType(Type sequenceType)
        {
            if (sequenceType.IsArray)
            {
                return sequenceType.GetElementType() ?? typeof(object);
            }

            if (sequenceType.IsGenericType && sequenceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return sequenceType.GetGenericArguments()[0];
            }

            var enumerableInterface = sequenceType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface is not null)
            {
                return enumerableInterface.GetGenericArguments()[0];
            }

            return typeof(object);
        }
    }
}
