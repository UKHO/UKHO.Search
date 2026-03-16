using System.Text.Json;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class IntAddActionDto
    {
        public JsonElement[]? Add { get; set; }

        public IEnumerable<int> GetAddValues()
        {
            if (Add is null)
            {
                return Array.Empty<int>();
            }

            var list = new List<int>(Add.Length);
            foreach (var e in Add)
            {
                if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var val))
                {
                    list.Add(val);
                }
            }

            return list;
        }

        public IEnumerable<string> GetAddTemplates()
        {
            if (Add is null)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>(Add.Length);
            foreach (var e in Add)
            {
                if (e.ValueKind == JsonValueKind.String)
                {
                    var s = e.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }
                }
            }

            return list;
        }
    }
}
