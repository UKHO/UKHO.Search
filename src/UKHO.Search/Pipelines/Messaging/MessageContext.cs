namespace UKHO.Search.Pipelines.Messaging
{
    public sealed class MessageContext
    {
        private readonly List<string> _breadcrumbs = new();
        private readonly Dictionary<string, object?> _items = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _timingsUtc = new();

        public IReadOnlyList<string> Breadcrumbs => _breadcrumbs;

        public IReadOnlyDictionary<string, object?> Items => _items;

        public IReadOnlyDictionary<string, DateTimeOffset> TimingsUtc => _timingsUtc;

        public void SetItem(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _items[key] = value;
        }

        public bool TryGetItem<T>(string key, out T? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = default;
                return false;
            }

            if (!_items.TryGetValue(key, out var raw) || raw is not T typed)
            {
                value = default;
                return false;
            }

            value = typed;
            return true;
        }

        public void AddBreadcrumb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _breadcrumbs.Add(value);
        }

        public void MarkTimeUtc(string name, DateTimeOffset timeUtc)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            _timingsUtc[name] = timeUtc;
        }

        public MessageContext Clone()
        {
            var clone = new MessageContext();

            clone._breadcrumbs.AddRange(_breadcrumbs);

            foreach (var kvp in _items)
            {
                clone._items.Add(kvp.Key, kvp.Value);
            }

            foreach (var kvp in _timingsUtc)
            {
                clone._timingsUtc.Add(kvp.Key, kvp.Value);
            }

            return clone;
        }
    }
}