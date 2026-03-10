namespace FileShareEmulator.Services
{
    public static class BatchSecurityTokenBuilder
    {
        public static string[] BuildTokens(IEnumerable<string> groupIdentifiers, IEnumerable<string> userIdentifiers, string? businessUnitName)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var results = new List<string>(64);

            static string? Normalize(string? token)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var normalized = token.Trim()
                                      .ToLowerInvariant();
                return normalized.Length == 0 ? null : normalized;
            }

            void AddIfNew(string token)
            {
                if (seen.Add(token))
                {
                    results.Add(token);
                }
            }

            AddIfNew("batchcreate");

            var normalizedBusinessUnitName = Normalize(businessUnitName);
            if (normalizedBusinessUnitName is not null)
            {
                AddIfNew($"batchcreate_{normalizedBusinessUnitName}");
            }

            var normalizedGroups = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var group in groupIdentifiers)
            {
                var normalized = Normalize(group);
                if (normalized is not null)
                {
                    normalizedGroups.Add(normalized);
                }
            }

            var normalizedUsers = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var user in userIdentifiers)
            {
                var normalized = Normalize(user);
                if (normalized is not null)
                {
                    normalizedUsers.Add(normalized);
                }
            }

            foreach (var group in normalizedGroups)
            {
                AddIfNew(group);
            }

            foreach (var user in normalizedUsers)
            {
                AddIfNew(user);
            }

            return results.ToArray();
        }
    }
}