using Microsoft.Extensions.Logging;
using QueryServiceHost.Models;
using QueryServiceHost.Services;

namespace QueryServiceHost.State
{
    public class QueryUiState
    {
        private readonly IQueryUiSearchClient _searchClient;
        private readonly ILogger<QueryUiState> _logger;

        public QueryUiState(IQueryUiSearchClient searchClient, ILogger<QueryUiState> logger)
        {
            _searchClient = searchClient;
            _logger = logger;
        }

        public string QueryText { get; set; } = string.Empty;

        public bool HasSubmitted { get; private set; }

        public bool IsLoading { get; private set; }

        public string? Error { get; private set; }

        public QueryResponse? LastResponse { get; private set; }

        public Hit? SelectedHit { get; private set; }

        public IReadOnlyDictionary<string, IReadOnlySet<string>> SelectedFacets { get; private set; } =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<FacetGroup> FacetGroups => LastResponse?.Facets ?? Array.Empty<FacetGroup>();

        public event Action? Changed;

        public IReadOnlyList<FilterChip> GetSelectedFacetChips()
        {
            var chips = new List<FilterChip>();

            foreach (var (groupName, values) in SelectedFacets)
            {
                foreach (var value in values)
                {
                    chips.Add(new FilterChip { GroupName = groupName, Value = value });
                }
            }

            return chips;
        }

        public bool IsFacetValueSelected(string groupName, string value)
        {
            return SelectedFacets.TryGetValue(groupName, out var selected) && selected.Contains(value);
        }

        public async Task ToggleFacetValueAsync(string groupName, string value, CancellationToken cancellationToken)
        {
            var next = SelectedFacets.ToDictionary(k => k.Key, v => (IReadOnlySet<string>)new HashSet<string>(v.Value), StringComparer.OrdinalIgnoreCase);

            if (!next.TryGetValue(groupName, out var selected))
            {
                selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                next[groupName] = selected;
            }

            var mutable = (HashSet<string>)selected;

            if (!mutable.Add(value))
            {
                mutable.Remove(value);
            }

            SelectedFacets = next;
            NotifyChanged();

            if (HasSubmitted)
            {
                await ExecuteSearchAsync(cancellationToken);
            }
        }

        public async Task ClearFacetValueAsync(string groupName, string value, CancellationToken cancellationToken)
        {
            if (!SelectedFacets.TryGetValue(groupName, out var selected) || !selected.Contains(value))
            {
                return;
            }

            var next = SelectedFacets.ToDictionary(k => k.Key, v => (IReadOnlySet<string>)new HashSet<string>(v.Value), StringComparer.OrdinalIgnoreCase);
            if (next.TryGetValue(groupName, out var copy))
            {
                var mutable = (HashSet<string>)copy;
                mutable.Remove(value);

                if (mutable.Count == 0)
                {
                    next.Remove(groupName);
                }
            }

            SelectedFacets = next;
            NotifyChanged();

            if (HasSubmitted)
            {
                await ExecuteSearchAsync(cancellationToken);
            }
        }

        public void ToggleFacetGroupCollapsed(string groupName)
        {
            var group = LastResponse?.Facets.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
            if (group is null)
            {
                return;
            }

            group.IsCollapsed = !group.IsCollapsed;
            NotifyChanged();
        }

        public async Task ExecuteSearchAsync(CancellationToken cancellationToken)
        {
            HasSubmitted = true;
            Error = null;
            IsLoading = true;
            NotifyChanged();

            try
            {
                var request = new QueryRequest
                {
                    QueryText = QueryText,
                    SelectedFacets = SelectedFacets
                };

                var response = await _searchClient.SearchAsync(request, cancellationToken);

                LastResponse = response;

                if (SelectedHit is not null && !response.Hits.Contains(SelectedHit))
                {
                    SelectedHit = null;
                }

                _logger.LogInformation(
                    "Query UI search completed. Total={Total} DurationMs={DurationMs}",
                    response.Total,
                    response.Duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query UI search failed");
                Error = "Search failed. Please try again.";
            }
            finally
            {
                IsLoading = false;
                NotifyChanged();
            }
        }

        public void SelectHit(Hit hit)
        {
            SelectedHit = hit;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
