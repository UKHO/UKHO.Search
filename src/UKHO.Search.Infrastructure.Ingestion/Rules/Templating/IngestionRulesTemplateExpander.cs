using System.Linq;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Templating
{
    internal sealed class IngestionRulesTemplateExpander
    {
        public IReadOnlyList<string> Expand(string? template, TemplateContext context)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return Array.Empty<string>();
            }

            var outputs = new List<string> { template };

            while (true)
            {
                var didReplace = false;
                var nextOutputs = new List<string>();

                foreach (var output in outputs)
                {
                    if (!TryFindVariable(output, out var startIndex, out var length, out var variableKind, out var argument))
                    {
                        nextOutputs.Add(output);
                        continue;
                    }

                    didReplace = true;
                    var values = ResolveVariable(variableKind, argument, context);
                    if (values.Count == 0)
                    {
                        continue;
                    }

                    var prefix = output.Substring(0, startIndex);
                    var suffix = output.Substring(startIndex + length);

                    foreach (var value in values)
                    {
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        nextOutputs.Add(string.Concat(prefix, value, suffix));
                    }
                }

                outputs = nextOutputs;
                if (!didReplace)
                {
                    break;
                }

                if (outputs.Count == 0)
                {
                    break;
                }
            }

            return outputs.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
        }

        private static IReadOnlyList<string> ResolveVariable(string variableKind, string? argument, TemplateContext context)
        {
            if (string.Equals(variableKind, "val", StringComparison.Ordinal))
            {
                return context.Val;
            }

            if (string.Equals(variableKind, "path", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    return Array.Empty<string>();
                }

                return context.PathResolver.Resolve(context.Payload, argument);
            }

            return Array.Empty<string>();
        }

        private static bool TryFindVariable(string text, out int startIndex, out int length, out string variableKind, out string? argument)
        {
            startIndex = -1;
            length = 0;
            variableKind = string.Empty;
            argument = null;

            var i = text.IndexOf('$', StringComparison.Ordinal);
            if (i < 0)
            {
                return false;
            }

            startIndex = i;

            if (text.AsSpan(i).StartsWith("$val".AsSpan(), StringComparison.Ordinal))
            {
                length = 4;
                variableKind = "val";
                return true;
            }

            if (text.AsSpan(i).StartsWith("$path:".AsSpan(), StringComparison.Ordinal))
            {
                variableKind = "path";

                var argStart = i + 6;
                var argEnd = argStart;

                while (argEnd < text.Length)
                {
                    var c = text[argEnd];
                    if (char.IsWhiteSpace(c) || c == '$')
                    {
                        break;
                    }

                    argEnd++;
                }

                argument = text.Substring(argStart, argEnd - argStart);
                length = argEnd - i;
                return true;
            }

            // Unknown variable: treat as missing and remove it.
            variableKind = "unknown";
            length = 1;
            return true;
        }
    }
}
