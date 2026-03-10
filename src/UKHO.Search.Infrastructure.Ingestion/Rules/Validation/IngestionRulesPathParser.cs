namespace UKHO.Search.Infrastructure.Ingestion.Rules.Validation
{
    internal static class IngestionRulesPathParser
    {
        public static bool TryParse(string path, out List<(string Kind, string? Value)> steps, out string? error)
        {
            steps = new List<(string Kind, string? Value)>();
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Path is null/empty.";
                return false;
            }

            var span = path.AsSpan().Trim();
            var i = 0;

            string? lastPropertyName = null;

            while (i < span.Length)
            {
                var start = i;
                while (i < span.Length && span[i] != '.' && span[i] != '[')
                {
                    i++;
                }

                if (i == start)
                {
                    error = $"Invalid path syntax at position {i}.";
                    return false;
                }

                var identifier = span.Slice(start, i - start).ToString();
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    error = "Path contains an empty segment.";
                    return false;
                }

                steps.Add(("property", identifier));
                lastPropertyName = identifier;

                if (i >= span.Length)
                {
                    break;
                }

                if (span[i] == '[')
                {
                    i++;
                    if (i >= span.Length)
                    {
                        error = "Unterminated '[' in path.";
                        return false;
                    }

                    if (span[i] == '*')
                    {
                        i++;
                        if (i >= span.Length || span[i] != ']')
                        {
                            error = "Wildcard selector must be '[*]'.";
                            return false;
                        }

                        i++;
                        steps.Add(("wildcard", null));
                    }
                    else if (span[i] == '"')
                    {
                        if (!string.Equals(lastPropertyName, "properties", StringComparison.OrdinalIgnoreCase))
                        {
                            error = "Bracket selector is only supported for 'properties[\"<name>\"]'.";
                            return false;
                        }

                        i++;
                        var keyStart = i;
                        while (i < span.Length && span[i] != '"')
                        {
                            i++;
                        }

                        if (i >= span.Length)
                        {
                            error = "Unterminated string in bracket selector.";
                            return false;
                        }

                        var key = span.Slice(keyStart, i - keyStart).ToString();
                        i++;

                        if (i >= span.Length || span[i] != ']')
                        {
                            error = "Bracket selector must be 'properties[\"<name>\"]'.";
                            return false;
                        }

                        i++;
                        steps.Add(("propertiesLookup", key));

                        if (i < span.Length)
                        {
                            error = "No further segments are allowed after a properties lookup.";
                            return false;
                        }

                        break;
                    }
                    else
                    {
                        var selectorStart = i;
                        while (i < span.Length && span[i] != ']')
                        {
                            i++;
                        }

                        var selector = span.Slice(selectorStart, Math.Max(0, i - selectorStart)).ToString();
                        error = $"Unsupported selector '[{selector}]'. Only '[*]' is supported.";
                        return false;
                    }

                    if (i < span.Length && span[i] == '.')
                    {
                        i++;
                        continue;
                    }

                    if (i < span.Length)
                    {
                        error = $"Invalid path syntax at position {i}.";
                        return false;
                    }

                    break;
                }

                if (span[i] == '.')
                {
                    if (string.Equals(lastPropertyName, "properties", StringComparison.OrdinalIgnoreCase))
                    {
                        i++;
                        if (i >= span.Length)
                        {
                            error = "properties.<name> requires a name segment.";
                            return false;
                        }

                        var nameStart = i;
                        while (i < span.Length && span[i] != '.' && span[i] != '[')
                        {
                            i++;
                        }

                        var key = span.Slice(nameStart, i - nameStart).ToString();
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            error = "properties.<name> requires a non-empty name.";
                            return false;
                        }

                        steps.Add(("propertiesLookup", key));

                        if (i < span.Length)
                        {
                            error = "No further segments are allowed after a properties lookup.";
                            return false;
                        }

                        break;
                    }

                    i++;
                    continue;
                }

                error = $"Invalid path syntax at position {i}.";
                return false;
            }

            return true;
        }
    }
}
