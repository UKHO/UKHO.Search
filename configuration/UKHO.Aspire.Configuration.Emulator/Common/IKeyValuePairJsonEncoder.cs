using System.Text.Json;

namespace UKHO.Aspire.Configuration.Emulator.Common
{
    public interface IKeyValuePairJsonEncoder
    {
        JsonDocument Encode(
            IEnumerable<KeyValuePair<string, string?>> pairs,
            string? prefix = null,
            string? separator = null);
    }
}