using Microsoft.Extensions.Logging;
using System.Text.Json;
using QueryServiceHost.Models;
using QueryServiceHost.Services;
using UKHO.Search.Query.Models;

namespace QueryServiceHost.State
{
    /// <summary>
    /// Coordinates the interactive host state for the single-screen query workspace shell.
    /// </summary>
    public class QueryUiState
    {
        private static readonly JsonSerializerOptions EditedPlanSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IQueryUiSearchClient _searchClient;
        private readonly ILogger<QueryUiState> _logger;

        /// <summary>
        /// Initializes the shared query UI state container.
        /// </summary>
        /// <param name="searchClient">The host-local search client that executes both raw-query and edited-plan workflows.</param>
        /// <param name="logger">The logger used to capture state-transition diagnostics.</param>
        public QueryUiState(IQueryUiSearchClient searchClient, ILogger<QueryUiState> logger)
        {
            // Capture the injected collaborators once so the scoped state container can coordinate the page without re-resolving services.
            _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets or sets the raw query text currently shown in the top command bar.
        /// </summary>
        public string QueryText { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the user has submitted at least one raw query during the current session.
        /// </summary>
        public bool HasSubmitted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any query execution is currently in progress.
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Gets the current user-visible error message when raw-query execution, edited-plan execution, or validation fails.
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Gets the current edited-plan validation errors that should block execution until corrected.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Gets the last successful host response returned by either execution path.
        /// </summary>
        public QueryResponse? LastResponse { get; private set; }

        /// <summary>
        /// Gets the repository-owned query plan retained on the latest successful host response.
        /// </summary>
        public QueryPlan? CurrentPlan => LastResponse?.Plan;

        /// <summary>
        /// Gets the generated plan JSON returned by the most recent successful raw-query execution.
        /// </summary>
        public string GeneratedPlanText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the last successful edited-plan JSON that completed execution through the repository-owned query pipeline.
        /// </summary>
        public string LastSuccessfulEditedPlanText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the editable query plan text currently shown in the Monaco workspace.
        /// </summary>
        public string EditablePlanText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the state currently holds a generated plan to display.
        /// </summary>
        public bool HasGeneratedPlan => !string.IsNullOrWhiteSpace(GeneratedPlanText);

        /// <summary>
        /// Gets a value indicating whether the current editor contents can be reset back to the latest generated raw-query plan.
        /// </summary>
        public bool CanResetToGeneratedPlan =>
            HasGeneratedPlan &&
            !string.Equals(EditablePlanText, GeneratedPlanText, StringComparison.Ordinal);

        /// <summary>
        /// Gets a value indicating whether the latest execution results came from the edited-plan workflow.
        /// </summary>
        public bool LastExecutionUsedEditedPlan { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the state is currently executing the edited-plan workflow.
        /// </summary>
        public bool IsExecutingEditedPlan { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the diagnostics column currently has blocking validation errors to render.
        /// </summary>
        public bool HasValidationErrors => ValidationErrors.Count > 0;

        /// <summary>
        /// Gets the current Elasticsearch request JSON projected for the diagnostics column.
        /// </summary>
        public string CurrentElasticsearchRequestJson => LastResponse?.ElasticsearchRequestJson ?? string.Empty;

        /// <summary>
        /// Gets the current non-blocking warnings projected from the repository-owned query pipeline.
        /// </summary>
        public IReadOnlyList<string> CurrentWarnings => LastResponse?.Warnings ?? Array.Empty<string>();

        /// <summary>
        /// Gets the current search-engine-reported execution duration, when Elasticsearch returned one.
        /// </summary>
        public TimeSpan? CurrentSearchEngineDuration => LastResponse?.SearchEngineDuration;

        /// <summary>
        /// Gets the currently selected result, if the user has chosen one in the flat results list.
        /// </summary>
        public Hit? SelectedHit { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the selected-result explanation is currently open in its dedicated detail mode.
        /// </summary>
        public bool IsResultDrawerOpen { get; private set; }

        /// <summary>
        /// Gets the currently selected facet values keyed by facet-group name.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlySet<string>> SelectedFacets { get; private set; } =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the facet groups returned by the most recent successful response.
        /// </summary>
        public IReadOnlyList<FacetGroup> FacetGroups => LastResponse?.Facets ?? Array.Empty<FacetGroup>();

        /// <summary>
        /// Raised whenever the shared query workspace state changes and UI components should re-render.
        /// </summary>
        public event Action? Changed;

        /// <summary>
        /// Gets the currently selected facet chips shown beneath the raw-query command bar.
        /// </summary>
        /// <returns>The flattened chip list projected from the selected facet dictionary.</returns>
        public IReadOnlyList<FilterChip> GetSelectedFacetChips()
        {
            // Project the selected-facet dictionary into a stable flat list so the command bar can render removable chips easily.
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

        /// <summary>
        /// Determines whether a specific facet value is currently selected.
        /// </summary>
        /// <param name="groupName">The name of the facet group to inspect.</param>
        /// <param name="value">The facet value to look for within the group.</param>
        /// <returns><see langword="true"/> when the facet value is selected; otherwise, <see langword="false"/>.</returns>
        public bool IsFacetValueSelected(string groupName, string value)
        {
            // Read the current immutable selection snapshot without mutating shared state.
            return SelectedFacets.TryGetValue(groupName, out var selected) && selected.Contains(value);
        }

        /// <summary>
        /// Toggles a facet value and optionally re-executes the raw-query search when the page already has a submitted query.
        /// </summary>
        /// <param name="groupName">The facet group containing the value to toggle.</param>
        /// <param name="value">The facet value to add or remove.</param>
        /// <param name="cancellationToken">The cancellation token that stops any follow-up search execution.</param>
        public async Task ToggleFacetValueAsync(string groupName, string value, CancellationToken cancellationToken)
        {
            // Clone the current selection snapshot so the new state remains isolated from any existing read-only sets.
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
                // Re-run the raw-query path so the facet toggle immediately refreshes the workspace after the user has started searching.
                await ExecuteSearchAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Clears a selected facet value and optionally re-executes the raw-query search when the page already has a submitted query.
        /// </summary>
        /// <param name="groupName">The facet group containing the value to remove.</param>
        /// <param name="value">The facet value to remove.</param>
        /// <param name="cancellationToken">The cancellation token that stops any follow-up search execution.</param>
        public async Task ClearFacetValueAsync(string groupName, string value, CancellationToken cancellationToken)
        {
            // Exit early when the requested facet value is not selected because there is nothing to clear.
            if (!SelectedFacets.TryGetValue(groupName, out var selected) || !selected.Contains(value))
            {
                return;
            }

            // Clone the current selection snapshot before mutating it so components always observe a fresh immutable dictionary instance.
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
                // Keep the result workspace in sync with the current facet selection after the initial query has been submitted.
                await ExecuteSearchAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Toggles the collapsed state of a facet group returned in the last response.
        /// </summary>
        /// <param name="groupName">The facet-group name to expand or collapse.</param>
        public void ToggleFacetGroupCollapsed(string groupName)
        {
            // Locate the projected group in the current response so only visible facet groups are mutated.
            var group = LastResponse?.Facets.FirstOrDefault(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
            if (group is null)
            {
                return;
            }

            group.IsCollapsed = !group.IsCollapsed;
            NotifyChanged();
        }

        /// <summary>
        /// Executes the raw-query workflow and refreshes the generated-plan, results, and diagnostics state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that stops the search when the caller no longer needs the result.</param>
        public async Task ExecuteSearchAsync(CancellationToken cancellationToken)
        {
            // Mark the search as submitted and raise an immediate notification so the shell can switch into its loading state.
            HasSubmitted = true;
            Error = null;
            ValidationErrors = Array.Empty<string>();
            IsLoading = true;
            IsExecutingEditedPlan = false;
            NotifyChanged();

            try
            {
                // Build the host-local request from the current raw query text and selected facets.
                var request = new QueryRequest
                {
                    QueryText = QueryText,
                    SelectedFacets = SelectedFacets
                };

                // Execute the raw-query search through the host adapter.
                var response = await _searchClient.SearchAsync(request, cancellationToken);
                var previousSelectedHit = SelectedHit;

                // Store the newest response and reset the editor working copy to the generated plan from this raw-query run.
                LastResponse = response;
                GeneratedPlanText = response.GeneratedPlanJson ?? string.Empty;
                EditablePlanText = GeneratedPlanText;
                SelectedHit = previousSelectedHit is null ? null : FindMatchingHit(previousSelectedHit, response.Hits);
                IsResultDrawerOpen = SelectedHit is not null && IsResultDrawerOpen;
                LastExecutionUsedEditedPlan = false;

                // Emit a structured success log so developers can correlate the visible workspace state with the host execution outcome.
                _logger.LogInformation(
                    "Query UI raw-query search completed. Total={Total} DurationMs={DurationMs} HasGeneratedPlan={HasGeneratedPlan}",
                    response.Total,
                    response.Duration.TotalMilliseconds,
                    HasGeneratedPlan);
            }
            catch (Exception ex)
            {
                // Preserve the previous successful workspace state but surface a user-visible error for the failed raw-query attempt.
                _logger.LogError(ex, "Query UI raw-query search failed");
                Error = "Search failed. Please try again.";
            }
            finally
            {
                // Leave the loading state and notify subscribers regardless of success or failure.
                IsLoading = false;
                IsExecutingEditedPlan = false;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Executes the current Monaco editor contents as an edited query plan.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that stops plan execution when the caller no longer needs the result.</param>
        public async Task ExecuteEditedPlanAsync(CancellationToken cancellationToken)
        {
            // Validate the current editor contents before calling into the repository-owned execution pipeline.
            if (!TryDeserializeEditedPlan(EditablePlanText, out var plan, out var validationErrors, out var validationException))
            {
                Error = "Edited plan validation failed. Review diagnostics and try again.";
                ValidationErrors = validationErrors;

                // Record the blocked execution so contributors can diagnose malformed JSON and contract-shape mismatches.
                _logger.LogWarning(
                    validationException,
                    "Query UI edited-plan execution was blocked by validation. QueryTextLength={QueryTextLength} ValidationErrorCount={ValidationErrorCount}",
                    QueryText.Length,
                    validationErrors.Count);

                NotifyChanged();
                return;
            }

            Error = null;
            ValidationErrors = Array.Empty<string>();
            IsLoading = true;
            IsExecutingEditedPlan = true;
            NotifyChanged();

            try
            {
                // Execute the validated repository-owned plan through the host adapter without regenerating it from the raw-query bar.
                var response = await _searchClient.ExecutePlanAsync(plan!, cancellationToken)
                    .ConfigureAwait(false);
                var previousSelectedHit = SelectedHit;

                LastResponse = response;
                SelectedHit = previousSelectedHit is null ? null : FindMatchingHit(previousSelectedHit, response.Hits);
                IsResultDrawerOpen = SelectedHit is not null && IsResultDrawerOpen;
                LastExecutionUsedEditedPlan = true;
                LastSuccessfulEditedPlanText = response.GeneratedPlanJson;

                // Emit a structured success log so edited-plan runs can be distinguished from raw-query searches in diagnostics output.
                _logger.LogInformation(
                    "Query UI edited-plan execution completed. Total={Total} DurationMs={DurationMs}",
                    response.Total,
                    response.Duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                // Preserve the last successful workspace state but surface a focused failure for the edited-plan execution path.
                _logger.LogError(ex, "Query UI edited-plan execution failed");
                Error = "Edited plan execution failed. Please review the plan and try again.";
            }
            finally
            {
                // Leave the loading state and notify subscribers regardless of success or failure.
                IsLoading = false;
                IsExecutingEditedPlan = false;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Selects a result hit so the dedicated explanation view can explain the chosen document.
        /// </summary>
        /// <param name="hit">The result hit that should become the current selection.</param>
        public void SelectHit(Hit hit)
        {
            // Replace the current selection with the chosen hit so the explanation view follows the flat list selection.
            SelectedHit = hit;
            IsResultDrawerOpen = true;
            NotifyChanged();
        }

        /// <summary>
        /// Toggles the dedicated selected-result explanation mode for the current selection.
        /// </summary>
        public void ToggleResultDrawer()
        {
            // Ignore toggle requests until a result has been selected because the explanation view has nothing to inspect otherwise.
            if (SelectedHit is null)
            {
                return;
            }

            IsResultDrawerOpen = !IsResultDrawerOpen;
            NotifyChanged();
        }

        /// <summary>
        /// Updates the Monaco working copy while preserving the generated-plan baseline from the last successful raw-query run.
        /// </summary>
        /// <param name="value">The latest text produced by the editor.</param>
        public void UpdateEditablePlanText(string value)
        {
            // Avoid redundant notifications when the editor reports the same text that is already stored in state.
            if (string.Equals(EditablePlanText, value, StringComparison.Ordinal))
            {
                return;
            }

            EditablePlanText = value ?? string.Empty;
            NotifyChanged();
        }

        /// <summary>
        /// Restores the editor working copy back to the latest generated raw-query plan.
        /// </summary>
        public void ResetEditablePlanTextToGeneratedPlan()
        {
            // Do nothing when no generated baseline exists because there is nothing meaningful to restore.
            if (!HasGeneratedPlan)
            {
                return;
            }

            EditablePlanText = GeneratedPlanText;
            Error = null;
            ValidationErrors = Array.Empty<string>();
            NotifyChanged();
        }

        /// <summary>
        /// Deserializes and validates the editor contents as a repository-owned query plan.
        /// </summary>
        /// <param name="editablePlanText">The JSON text currently held in the Monaco editor.</param>
        /// <param name="plan">The deserialized query plan when validation succeeds; otherwise <see langword="null"/>.</param>
        /// <param name="validationErrors">The blocking validation errors discovered while parsing the edited plan.</param>
        /// <param name="validationException">The parser exception that explained the failure, when available.</param>
        /// <returns><see langword="true"/> when the editor text deserializes into a valid query plan; otherwise <see langword="false"/>.</returns>
        private static bool TryDeserializeEditedPlan(
            string editablePlanText,
            out QueryPlan? plan,
            out IReadOnlyList<string> validationErrors,
            out Exception? validationException)
        {
            if (string.IsNullOrWhiteSpace(editablePlanText))
            {
                // Block empty submissions explicitly so the diagnostics panel can guide the user toward generating or pasting a plan first.
                plan = null;
                validationErrors = ["Edited plan JSON is empty. Generate a raw-query plan or paste a valid query plan before executing."];
                validationException = null;
                return false;
            }

            try
            {
                // Deserialize into the repository-owned contract so host validation matches the same plan model used by the backend layers.
                plan = JsonSerializer.Deserialize<QueryPlan>(editablePlanText, EditedPlanSerializerOptions);

                if (plan is null ||
                    plan.Input is null ||
                    plan.Extracted is null ||
                    plan.Model is null ||
                    plan.Defaults is null ||
                    plan.Execution is null ||
                    plan.Diagnostics is null)
                {
                    validationErrors = ["Edited plan JSON does not satisfy the QueryPlan contract. Ensure all top-level sections are present before executing."];
                    validationException = null;
                    return false;
                }

                validationErrors = Array.Empty<string>();
                validationException = null;
                return true;
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // Return the parser message to the diagnostics UI so the user can correct malformed JSON in place.
                plan = null;
                validationErrors = [$"Edited plan JSON could not be parsed: {ex.Message}"];
                validationException = ex;
                return false;
            }
        }

        /// <summary>
        /// Finds the logically matching hit in a fresh result set so selection can survive response re-projection.
        /// </summary>
        /// <param name="selectedHit">The previously selected hit instance.</param>
        /// <param name="hits">The newly projected result hits.</param>
        /// <returns>The matching new hit instance when one exists; otherwise, <see langword="null"/>.</returns>
        private static Hit? FindMatchingHit(Hit selectedHit, IReadOnlyList<Hit> hits)
        {
            // Prefer the newly projected hit instance so the UI selection tracks the current response payload.
            return hits.FirstOrDefault(hit => AreHitsEquivalent(selectedHit, hit));
        }

        /// <summary>
        /// Determines whether two projected hits represent the same logical result row.
        /// </summary>
        /// <param name="left">The previously selected hit.</param>
        /// <param name="right">The candidate hit from the newest response.</param>
        /// <returns><see langword="true"/> when the hits should be treated as equivalent; otherwise, <see langword="false"/>.</returns>
        private static bool AreHitsEquivalent(Hit left, Hit right)
        {
            // Compare the stable display identity fields so selection can survive fresh host projections of the same logical result.
            return string.Equals(left.Title, right.Title, StringComparison.Ordinal) &&
                   string.Equals(left.Type, right.Type, StringComparison.Ordinal) &&
                   string.Equals(left.Region, right.Region, StringComparison.Ordinal);
        }

        /// <summary>
        /// Raises the shared state-changed notification.
        /// </summary>
        private void NotifyChanged()
        {
            // Notify all subscribed UI components that the shared workspace state has changed.
            Changed?.Invoke();
        }
    }
}
