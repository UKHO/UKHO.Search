using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using QueryServiceHost.Models;
using QueryServiceHost.State;
using Shouldly;
using UKHO.Search.Query.Models;
using Xunit;

namespace QueryServiceHost.Tests
{
    /// <summary>
    /// Verifies the host state behaviors introduced by the single-screen query workspace shell.
    /// </summary>
    public sealed class QueryUiStateTests
    {
        /// <summary>
        /// Verifies that a successful raw-query execution stores the generated plan separately from the editable working copy shown in Monaco.
        /// </summary>
        [Fact]
        public async Task ExecuteSearchAsync_stores_the_latest_generated_plan_and_initializes_the_editor_working_copy()
        {
            // Return a deterministic response so the state container can be verified without external pipeline dependencies.
            var response = CreateResponse(
                generatedPlanJson: "{\n  \"Input\": {\n    \"RawText\": \"wreck 42\"\n  }\n}",
                hits:
                [
                    CreateHit(title: "Wreck 42", type: "Wreck", region: "North Sea")
                ]);

            var searchClient = new DelegatingQueryUiSearchClient((_, _) => Task.FromResult(response));
            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance)
            {
                QueryText = "wreck 42"
            };

            var notificationCount = 0;
            state.Changed += () => notificationCount++;

            // Execute the raw-query path that should now populate the generated-plan workspace.
            await state.ExecuteSearchAsync(CancellationToken.None);

            state.LastResponse.ShouldBe(response);
            state.GeneratedPlanText.ShouldBe(response.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(response.GeneratedPlanJson);
            state.Error.ShouldBeNull();
            state.HasValidationErrors.ShouldBeFalse();
            state.HasSubmitted.ShouldBeTrue();
            state.LastExecutionUsedEditedPlan.ShouldBeFalse();
            notificationCount.ShouldBeGreaterThanOrEqualTo(2);
        }

        /// <summary>
        /// Verifies that user edits change only the working copy and do not overwrite the last generated-plan baseline.
        /// </summary>
        [Fact]
        public async Task UpdateEditablePlanText_keeps_the_generated_plan_baseline_intact()
        {
            // Seed the state with a generated plan so the edit action can diverge from a known baseline.
            const string generatedPlan = "{\n  \"Input\": {\n    \"RawText\": \"pipeline\"\n  }\n}";
            var response = CreateResponse(generatedPlanJson: generatedPlan, hits: []);
            var searchClient = new DelegatingQueryUiSearchClient((_, _) => Task.FromResult(response));
            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance);

            await state.ExecuteSearchAsync(CancellationToken.None);

            // Simulate Monaco editing the working copy after the raw-query run completed.
            state.UpdateEditablePlanText("{\n  \"Input\": {\n    \"RawText\": \"pipeline edited\"\n  }\n}");

            state.GeneratedPlanText.ShouldBe(generatedPlan);
            state.EditablePlanText.ShouldContain("pipeline edited");
        }

        /// <summary>
        /// Verifies that the flatter results list still retains selection when a matching hit reappears after a subsequent search.
        /// </summary>
        [Fact]
        public async Task ExecuteSearchAsync_preserves_selection_when_a_matching_hit_reappears()
        {
            // Queue two responses so the second search returns a fresh hit instance with the same logical identity.
            var firstResponse = CreateResponse(
                generatedPlanJson: "{\n  \"Input\": {\n    \"RawText\": \"wreck\"\n  }\n}",
                hits:
                [
                    CreateHit(title: "Wreck 001", type: "Wreck", region: "North Sea")
                ]);

            var secondResponse = CreateResponse(
                generatedPlanJson: "{\n  \"Input\": {\n    \"RawText\": \"wreck updated\"\n  }\n}",
                hits:
                [
                    CreateHit(title: "Wreck 001", type: "Wreck", region: "North Sea")
                ]);

            var responses = new Queue<QueryResponse>([firstResponse, secondResponse]);
            var searchClient = new DelegatingQueryUiSearchClient((_, _) => Task.FromResult(responses.Dequeue()));
            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance);

            await state.ExecuteSearchAsync(CancellationToken.None);
            state.SelectHit(firstResponse.Hits[0]);
            state.IsResultDrawerOpen.ShouldBeTrue();

            // Execute the next search and verify that logical selection survives the response re-projection.
            await state.ExecuteSearchAsync(CancellationToken.None);

            state.SelectedHit.ShouldNotBeNull();
            state.SelectedHit.ShouldBeSameAs(secondResponse.Hits[0]);
            state.IsResultDrawerOpen.ShouldBeTrue();
            state.GeneratedPlanText.ShouldBe(secondResponse.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(secondResponse.GeneratedPlanJson);
        }

        /// <summary>
        /// Verifies that selecting a result opens the full-screen explanation mode and the explicit toggle can return to the workspace and reopen it without clearing state.
        /// </summary>
        [Fact]
        public async Task SelectHit_and_ToggleResultDrawer_manage_the_full_screen_explanation_mode_without_clearing_workspace_state()
        {
            // Seed the state with one deterministic hit so the explanation behavior can be verified independently from result refresh logic.
            var response = CreateResponse(
                generatedPlanJson: SerializePlan(CreatePlan("wreck 42")),
                hits:
                [
                    CreateHit(title: "Wreck 42", type: "Wreck", region: "North Sea")
                ]);
            var searchClient = new DelegatingQueryUiSearchClient((_, _) => Task.FromResult(response));
            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance)
            {
                QueryText = "wreck 42"
            };

            await state.ExecuteSearchAsync(CancellationToken.None);

            // Select the hit and verify that inspection opens immediately in the explanation mode.
            state.SelectHit(response.Hits[0]);
            state.SelectedHit.ShouldBeSameAs(response.Hits[0]);
            state.IsResultDrawerOpen.ShouldBeTrue();
            state.QueryText.ShouldBe("wreck 42");
            state.LastResponse.ShouldBeSameAs(response);
            state.GeneratedPlanText.ShouldBe(response.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(response.GeneratedPlanJson);

            // Return to the main workspace and reopen the explanation without disturbing the current selection or query workspace state.
            state.ToggleResultDrawer();
            state.IsResultDrawerOpen.ShouldBeFalse();
            state.SelectedHit.ShouldBeSameAs(response.Hits[0]);
            state.QueryText.ShouldBe("wreck 42");
            state.LastResponse.ShouldBeSameAs(response);
            state.GeneratedPlanText.ShouldBe(response.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(response.GeneratedPlanJson);

            state.ToggleResultDrawer();
            state.IsResultDrawerOpen.ShouldBeTrue();
            state.SelectedHit.ShouldBeSameAs(response.Hits[0]);
            state.QueryText.ShouldBe("wreck 42");
            state.LastResponse.ShouldBeSameAs(response);
            state.GeneratedPlanText.ShouldBe(response.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(response.GeneratedPlanJson);
        }

        /// <summary>
        /// Verifies that a valid edited plan executes through the supplied-plan route without overwriting the generated raw-query baseline.
        /// </summary>
        [Fact]
        public async Task ExecuteEditedPlanAsync_executes_the_editor_plan_without_overwriting_the_generated_baseline()
        {
            // Seed the state with a raw-query baseline and a distinct edited-plan response so the two execution paths can be differentiated.
            var rawPlan = CreatePlan("wreck 42");
            var editedPlan = CreatePlan("edited wreck 42");
            var rawResponse = CreateResponse(
                generatedPlanJson: SerializePlan(rawPlan),
                hits:
                [
                    CreateHit(title: "Wreck 42", type: "Wreck", region: "North Sea")
                ]);
            var editedResponse = CreateResponse(
                generatedPlanJson: SerializePlan(editedPlan),
                hits:
                [
                    CreateHit(title: "Edited Wreck 42", type: "Wreck", region: "North Sea")
                ]);

            var rawSearchCount = 0;
            var editedPlanExecutionCount = 0;
            var searchClient = new DelegatingQueryUiSearchClient(
                (_, _) =>
                {
                    rawSearchCount++;
                    return Task.FromResult(rawResponse);
                },
                (plan, _) =>
                {
                    editedPlanExecutionCount++;
                    plan.Input.RawText.ShouldBe("edited wreck 42");
                    return Task.FromResult(editedResponse);
                });

            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance)
            {
                QueryText = "wreck 42"
            };

            await state.ExecuteSearchAsync(CancellationToken.None);
            var editedPlanJson = SerializePlan(editedPlan);
            state.UpdateEditablePlanText(editedPlanJson);

            // Execute the Monaco working copy and verify that the generated raw-query baseline remains available for reset.
            await state.ExecuteEditedPlanAsync(CancellationToken.None);

            rawSearchCount.ShouldBe(1);
            editedPlanExecutionCount.ShouldBe(1);
            state.GeneratedPlanText.ShouldBe(rawResponse.GeneratedPlanJson);
            state.EditablePlanText.ShouldBe(editedPlanJson);
            state.LastSuccessfulEditedPlanText.ShouldBe(editedResponse.GeneratedPlanJson);
            state.LastExecutionUsedEditedPlan.ShouldBeTrue();
            state.LastResponse.ShouldBeSameAs(editedResponse);
            state.Error.ShouldBeNull();
        }

        /// <summary>
        /// Verifies that invalid edited-plan JSON is blocked locally, surfaces validation messages, and does not call the execution client.
        /// </summary>
        [Fact]
        public async Task ExecuteEditedPlanAsync_when_json_is_invalid_surfaces_validation_errors_without_executing_the_plan()
        {
            // Configure a client that would fail loudly if the edited-plan route were invoked because validation should stop execution first.
            var editedPlanExecutionCount = 0;
            var searchClient = new DelegatingQueryUiSearchClient(
                (_, _) => Task.FromResult(CreateResponse(generatedPlanJson: string.Empty, hits: [])),
                (_, _) =>
                {
                    editedPlanExecutionCount++;
                    return Task.FromResult(CreateResponse(generatedPlanJson: string.Empty, hits: []));
                });

            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance);
            state.UpdateEditablePlanText("{ invalid json }");

            // Execute the malformed editor contents and verify that validation blocks the supplied-plan path.
            await state.ExecuteEditedPlanAsync(CancellationToken.None);

            editedPlanExecutionCount.ShouldBe(0);
            state.LastResponse.ShouldBeNull();
            state.HasValidationErrors.ShouldBeTrue();
            state.ValidationErrors.Count.ShouldBe(1);
            state.ValidationErrors[0].ShouldContain("could not be parsed");
            state.Error.ShouldBe("Edited plan validation failed. Review diagnostics and try again.");
        }

        /// <summary>
        /// Verifies that resetting the editor restores the latest raw-query baseline and clears validation messages so the user can start again cleanly.
        /// </summary>
        [Fact]
        public async Task ResetEditablePlanTextToGeneratedPlan_restores_the_latest_raw_query_baseline_and_clears_validation_errors()
        {
            // Seed the state with a generated plan so reset has a baseline to restore.
            var response = CreateResponse(generatedPlanJson: SerializePlan(CreatePlan("wreck 42")), hits: []);
            var searchClient = new DelegatingQueryUiSearchClient((_, _) => Task.FromResult(response));
            var state = new QueryUiState(searchClient, NullLogger<QueryUiState>.Instance);

            await state.ExecuteSearchAsync(CancellationToken.None);
            state.UpdateEditablePlanText("{ invalid json }");
            await state.ExecuteEditedPlanAsync(CancellationToken.None);

            // Reset the editor back to the raw-query baseline so the user can discard bad edits quickly.
            state.ResetEditablePlanTextToGeneratedPlan();

            state.EditablePlanText.ShouldBe(state.GeneratedPlanText);
            state.HasValidationErrors.ShouldBeFalse();
            state.Error.ShouldBeNull();
        }

        /// <summary>
        /// Creates a deterministic host response for a state test scenario.
        /// </summary>
        /// <param name="generatedPlanJson">The generated-plan JSON that the response should expose to the host editor.</param>
        /// <param name="hits">The result hits that the response should return.</param>
        /// <returns>A host response shaped for state-container testing.</returns>
        private static QueryResponse CreateResponse(string generatedPlanJson, IReadOnlyList<Hit> hits)
        {
            // Keep the response factory tiny and explicit so each test can focus on the state transitions it cares about.
            return new QueryResponse
            {
                GeneratedPlanJson = generatedPlanJson,
                Hits = hits,
                Total = hits.Count,
                Duration = TimeSpan.FromMilliseconds(42)
            };
        }

        /// <summary>
        /// Creates a deterministic hit for result-selection test scenarios.
        /// </summary>
        /// <param name="title">The result title shown in the flat list.</param>
        /// <param name="type">The result type metadata shown beside the title.</param>
        /// <param name="region">The result region metadata shown beside the title.</param>
        /// <returns>A host hit instance with stable identity fields.</returns>
        private static Hit CreateHit(string title, string? type, string? region)
        {
            // Return only the fields required by the result-selection and layout tests to keep the fixture readable.
            return new Hit
            {
                Title = title,
                Type = type,
                Region = region,
                MatchedFields = ["title"]
            };
        }

        /// <summary>
        /// Creates a deterministic repository-owned query plan for edited-plan execution tests.
        /// </summary>
        /// <param name="rawText">The raw query text that should be preserved inside the query plan.</param>
        /// <returns>A deterministic query plan instance.</returns>
        private static QueryPlan CreatePlan(string rawText)
        {
            // Populate only the required query-plan sections so the host-state tests stay focused on UI orchestration behavior.
            return new QueryPlan
            {
                Input = new QueryInputSnapshot
                {
                    RawText = rawText,
                    NormalizedText = rawText.ToLowerInvariant(),
                    CleanedText = rawText,
                    Tokens = rawText.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                    ResidualTokens = rawText.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                    ResidualText = rawText
                },
                Extracted = new QueryExtractedSignals(),
                Model = new CanonicalQueryModel
                {
                    Keywords = rawText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(static token => token.ToLowerInvariant())
                        .ToArray()
                },
                Defaults = new QueryDefaultContributions(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };
        }

        /// <summary>
        /// Serializes a deterministic query plan into the formatted JSON representation shown in Monaco.
        /// </summary>
        /// <param name="plan">The query plan that should be converted into editor JSON.</param>
        /// <returns>The formatted JSON representation used by the host workspace.</returns>
        private static string SerializePlan(QueryPlan plan)
        {
            // Match the production editor payload format so the tests reflect the same serialized shape used by the host adapter.
            return JsonSerializer.Serialize(plan, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
