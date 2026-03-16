using System.Diagnostics;

namespace FileShareEmulator.Common
{
    public static class SecurityTokenPolicy
    {
        private const string BatchCreateToken = "batchcreate";
        private const string PublicToken = "public";

        public static string[] CreateTokens(string? activeBusinessUnitName)
        {
            var tokens = new List<string>(3)
            {
                BatchCreateToken,
                PublicToken
            };

            var normalizedBusinessUnit = Normalize(activeBusinessUnitName);
            if (normalizedBusinessUnit is not null)
            {
                tokens.Insert(1, $"batchcreate_{normalizedBusinessUnit}");
            }

            Debug.Assert(tokens.All(t => t == t.ToLowerInvariant()), "All tokens must be lowercase.");

            return tokens.ToArray();
        }

        private static string? Normalize(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var normalized = token.Trim().ToLowerInvariant();
            return normalized.Length == 0 ? null : normalized;
        }
    }
}
