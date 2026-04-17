using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StudioServiceHost;
using StudioServiceHost.Tests.TestDoubles;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies the Studio service host ingestion and operations endpoints using a controllable in-memory provider double.
    /// </summary>
    public sealed class IngestionEndpointTests
    {
        /// <summary>
        /// Verifies that fetching a payload by identifier returns the provider-wrapped payload for a known provider.
        /// </summary>
        [Fact]
        public async Task GetIngestionPayloadById_returns_wrapped_payload_for_known_provider_and_id()
        {
            // Configure the test provider to return a fixed payload envelope for the requested identifier.
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(
                    StudioIngestionFetchPayloadResult.Success(
                        new StudioIngestionPayloadEnvelope
                        {
                            Id = id,
                            Payload = JsonDocument.Parse("""
                                {
                                  "RequestType": "IndexItem"
                                }
                                """).RootElement.Clone()
                        }))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Request the payload endpoint so the wrapped provider payload can be asserted.
                var response = await app.GetTestClient().GetFromJsonAsync<StudioIngestionPayloadEnvelope>("/ingestion/test-provider/batch-123");

                response.ShouldNotBeNull();
                response.Id.ShouldBe("batch-123");
                response.Payload.GetProperty("RequestType").GetString().ShouldBe("IndexItem");
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that payload retrieval remains available while a long-running ingestion operation is active.
        /// </summary>
        [Fact]
        public async Task GetIngestionPayloadById_returns_success_when_a_long_running_operation_is_active()
        {
            // Use a task completion source so the provider-wide operation can remain active while the payload endpoint is queried.
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(
                    StudioIngestionFetchPayloadResult.Success(
                        new StudioIngestionPayloadEnvelope
                        {
                            Id = id,
                            Payload = JsonDocument.Parse("""
                                {
                                  "RequestType": "IndexItem"
                                }
                                """).RootElement.Clone()
                        })),
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    // Report initial progress and then wait so the operation remains active during the payload request.
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 1.",
                        Completed = 0,
                        Total = 1
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start a long-running ingestion operation first so the host has an active tracked operation.
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                // Wait until the operation reports itself as running before querying the payload endpoint.
                _ = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                // Query the payload endpoint and verify that the active operation does not block reads.
                var response = await app.GetTestClient().GetAsync("/ingestion/test-provider/batch-123");
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionPayloadEnvelope>();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldNotBeNull();
                body.Id.ShouldBe("batch-123");

                releaseOperation.SetResult(true);
            }
            finally
            {
                // Always release the long-running provider callback before shutting the host down.
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that a completed operation can be retrieved by identifier after provider-wide ingestion finishes.
        /// </summary>
        [Fact]
        public async Task GetOperationById_returns_completed_operation_after_it_finishes()
        {
            // Configure the test provider to complete immediately after reporting terminal progress.
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 2 of 2.",
                        Completed = 2,
                        Total = 2
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 2 of 2.", 2, 2));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start provider-wide ingestion and capture the accepted operation identifier from the response body.
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var acceptedOperation = await startResponse.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                acceptedOperation.ShouldNotBeNull();

                // Poll the retained operations endpoint until the terminal succeeded state is observed.
                var completedOperation = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    $"/operations/{acceptedOperation.OperationId}",
                    operation => operation.Status == StudioIngestionOperationStatuses.Succeeded);

                completedOperation.OperationId.ShouldBe(acceptedOperation.OperationId);
                completedOperation.Completed.ShouldBe(2);
                completedOperation.Total.ShouldBe(2);
                completedOperation.CompletedUtc.ShouldNotBeNull();
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that provider contexts are returned in display-name order.
        /// </summary>
        [Fact]
        public async Task GetIngestionContexts_returns_contexts_sorted_by_display_name()
        {
            // Configure the provider to return contexts in unsorted order so the endpoint ordering can be asserted.
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "2", DisplayName = "Zulu", IsDefault = false },
                            new StudioIngestionContextResponse { Value = "1", DisplayName = "Alpha", IsDefault = true }
                        ]
                    })
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Query the contexts endpoint and verify that the host sorts the response deterministically.
                var response = await app.GetTestClient().GetFromJsonAsync<StudioIngestionContextsResponse>("/ingestion/test-provider/contexts");

                response.ShouldNotBeNull();
                response.Provider.ShouldBe("test-provider");
                response.Contexts.Select(context => context.DisplayName).ToArray().ShouldBe(["Alpha", "Zulu"]);
                response.Contexts[0].Value.ShouldBe("1");
                response.Contexts[0].IsDefault.ShouldBeTrue();
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that requesting ingestion for an unknown context returns a bad-request response.
        /// </summary>
        [Fact]
        public async Task PutIngestionContext_returns_bad_request_for_unknown_context()
        {
            // Configure the provider to expose a single known context so an invalid request can be rejected.
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    })
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Request ingestion for an unknown context and verify that the endpoint returns a validation error.
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/context/999", content: null);
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("999");
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that requesting ingestion for a known context returns an accepted tracked operation.
        /// </summary>
        [Fact]
        public async Task PutIngestionContext_returns_accepted_operation_for_known_context()
        {
            // Configure the provider to accept indexing for a single known context.
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    }),
                IndexContextHandler = (context, progress, cancellationToken) =>
                {
                    // Assert the forwarded context and report a simple successful progress update.
                    context.ShouldBe("12");
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start context-scoped ingestion and validate the accepted operation envelope returned by the host.
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/context/12", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.ContextIndex);
                body.Context.ShouldBe("12");
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that resetting indexing status for a known context returns an accepted tracked operation.
        /// </summary>
        [Fact]
        public async Task PostResetIndexingStatusForContext_returns_accepted_operation_for_known_context()
        {
            // Configure the provider to accept reset-indexing-status operations for a single known context.
            var provider = new TestStudioIngestionProvider
            {
                GetContextsHandler = () => Task.FromResult(
                    new StudioIngestionContextsResponse
                    {
                        Provider = "test-provider",
                        Contexts =
                        [
                            new StudioIngestionContextResponse { Value = "12", DisplayName = "Admiralty", IsDefault = false }
                        ]
                    }),
                ResetIndexingStatusForContextHandler = (context, progress, cancellationToken) =>
                {
                    // Assert the forwarded context and report a simple successful progress update.
                    context.ShouldBe("12");
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Reset indexing status for 2 items.",
                        Completed = 2,
                        Total = 2
                    });

                    return Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 2 items.", 2, 2));
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start the context-scoped reset operation and validate the accepted operation envelope.
                var response = await app.GetTestClient().PostAsync("/ingestion/test-provider/context/12/operations/reset-indexing-status", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.ResetIndexingStatus);
                body.Context.ShouldBe("12");
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that provider-wide ingestion returns an accepted operation envelope for a known provider.
        /// </summary>
        [Fact]
        public async Task PutIngestionAll_returns_accepted_operation_envelope_for_known_provider()
        {
            // Configure the provider to accept provider-wide ingestion and emit a simple completion update.
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    await Task.Yield();
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start provider-wide ingestion and validate the accepted operation envelope.
                var response = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var body = await response.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
                body.ShouldNotBeNull();
                body.Provider.ShouldBe("test-provider");
                body.OperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);
                body.Status.ShouldBe(StudioIngestionOperationStatuses.Queued);
                Guid.TryParse(body.OperationId, out _).ShouldBeTrue();
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that the active-operations endpoint reports a running provider-wide ingestion operation.
        /// </summary>
        [Fact]
        public async Task GetOperationsActive_returns_running_operation_after_provider_wide_ingestion_starts()
        {
            // Hold the provider callback open so the host has time to report the running operation state.
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    // Emit an initial running state before waiting for the test to continue execution.
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 2.",
                        Completed = 0,
                        Total = 2
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    // Emit terminal progress once the test allows the callback to complete.
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 2 of 2.",
                        Completed = 2,
                        Total = 2
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 2 of 2.", 2, 2);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start provider-wide ingestion before polling the active operations endpoint.
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                // Wait until the active operations endpoint reports a running state for the tracked operation.
                var activeOperation = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                activeOperation.ShouldNotBeNull();
                activeOperation.Provider.ShouldBe("test-provider");
                activeOperation.OperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);
                activeOperation.Status.ShouldBe(StudioIngestionOperationStatuses.Running);
                activeOperation.Completed.ShouldBe(0);
                activeOperation.Total.ShouldBe(2);

                releaseOperation.SetResult(true);
            }
            finally
            {
                // Always release the provider callback before shutting the host down.
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that payload submission is rejected while another long-running ingestion operation is active.
        /// </summary>
        [Fact]
        public async Task SubmitIngestionPayload_returns_conflict_when_a_long_running_operation_is_active()
        {
            // Hold the provider callback open so the active-operation lock remains in effect during payload submission.
            var releaseOperation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 0 of 1.",
                        Completed = 0,
                        Total = 1
                    });

                    await releaseOperation.Task.WaitAsync(cancellationToken);

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start provider-wide ingestion before attempting payload submission.
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                startResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                // Wait until the active operations endpoint reports the running state so the lock is definitely in place.
                _ = await WaitForOperationStateAsync(
                    app.GetTestClient(),
                    "/operations/active",
                    operation => operation.Status == StudioIngestionOperationStatuses.Running);

                // Submit a payload while the operation lock is active and verify that the host reports a conflict.
                var response = await app.GetTestClient().PostAsJsonAsync(
                    "/ingestion/test-provider/payload",
                    new StudioIngestionPayloadEnvelope
                    {
                        Id = "batch-123",
                        Payload = JsonDocument.Parse("""
                            {
                              "RequestType": "IndexItem"
                            }
                            """).RootElement.Clone()
                    });

                var body = await response.Content.ReadFromJsonAsync<StudioIngestionOperationConflictResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
                body.ShouldNotBeNull();
                body.ActiveProvider.ShouldBe("test-provider");
                body.ActiveOperationType.ShouldBe(StudioIngestionOperationTypes.IndexAll);

                releaseOperation.SetResult(true);
            }
            finally
            {
                // Always release the provider callback before shutting the host down.
                releaseOperation.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that the server-sent-events endpoint streams operation updates and completes after a terminal event.
        /// </summary>
        [Fact]
        public async Task GetOperationEvents_returns_live_updates_and_completes_after_terminal_event()
        {
            // Delay provider completion so the test can connect to the events stream before the terminal event is emitted.
            var allowCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var provider = new TestStudioIngestionProvider
            {
                IndexAllHandler = async (progress, cancellationToken) =>
                {
                    await allowCompletion.Task.WaitAsync(cancellationToken);
                    progress.Report(new StudioIngestionOperationProgressUpdate
                    {
                        Message = "Processed 1 of 1.",
                        Completed = 1,
                        Total = 1
                    });

                    return StudioIngestionOperationExecutionResult.Success("Processed 1 of 1.", 1, 1);
                }
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Start provider-wide ingestion and capture the accepted operation identifier from the response body.
                var startResponse = await app.GetTestClient().PutAsync("/ingestion/test-provider/all", content: null);
                var acceptedOperation = await startResponse.Content.ReadFromJsonAsync<StudioIngestionAcceptedOperationResponse>();

                acceptedOperation.ShouldNotBeNull();

                // Connect to the event stream before allowing the provider callback to complete.
                var responseTask = app.GetTestClient().GetAsync(
                    $"/operations/{acceptedOperation.OperationId}/events",
                    HttpCompletionOption.ResponseHeadersRead);

                var response = await responseTask;

                allowCompletion.SetResult(true);
                var body = await response.Content.ReadAsStringAsync();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldContain("\"status\":\"succeeded\"");
                body.ShouldContain("\"completed\":1");
            }
            finally
            {
                // Always release the provider callback before shutting the host down.
                allowCompletion.TrySetResult(true);
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that querying a payload for an unknown provider returns a bad-request response.
        /// </summary>
        [Fact]
        public async Task GetIngestionPayloadById_returns_bad_request_for_unknown_provider()
        {
            // Build the host without registering the requested provider so the endpoint returns a validation error.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                });

            await app.StartAsync();

            try
            {
                // Query the payload endpoint for an unknown provider and inspect the validation payload.
                var response = await app.GetTestClient().GetAsync("/ingestion/unknown-provider/batch-123");
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("unknown-provider");
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that querying a missing payload identifier returns a not-found response.
        /// </summary>
        [Fact]
        public async Task GetIngestionPayloadById_returns_not_found_for_unknown_id()
        {
            // Configure the provider to report a missing payload for any requested identifier.
            var provider = new TestStudioIngestionProvider
            {
                FetchPayloadByIdHandler = id => Task.FromResult(StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{id}'."))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Query the payload endpoint and verify that the provider-reported not-found result is preserved.
                var response = await app.GetTestClient().GetAsync("/ingestion/test-provider/missing-id");
                var error = await response.Content.ReadFromJsonAsync<StudioIngestionErrorResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
                error.ShouldNotBeNull();
                error.Message.ShouldContain("missing-id");
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that payload submission succeeds after a provider accepts the request.
        /// </summary>
        [Fact]
        public async Task SubmitIngestionPayload_returns_success_after_provider_accepts_payload()
        {
            // Configure the provider to accept payload submission and return a success response.
            var provider = new TestStudioIngestionProvider
            {
                SubmitPayloadHandler = request => Task.FromResult(StudioIngestionSubmitPayloadResult.Success("Payload submitted successfully."))
            };

            var app = BuildApp(provider);
            await app.StartAsync();

            try
            {
                // Submit a payload and verify that the provider receives the expected payload envelope.
                var response = await app.GetTestClient().PostAsJsonAsync(
                    "/ingestion/test-provider/payload",
                    new StudioIngestionPayloadEnvelope
                    {
                        Id = "batch-123",
                        Payload = JsonDocument.Parse("""
                            {
                              "RequestType": "IndexItem"
                            }
                            """).RootElement.Clone()
                    });

                var body = await response.Content.ReadFromJsonAsync<StudioIngestionSubmitPayloadResponse>();

                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                body.ShouldNotBeNull();
                body.Accepted.ShouldBeTrue();
                provider.SubmittedRequests.Count.ShouldBe(1);
                provider.SubmittedRequests[0].Id.ShouldBe("batch-123");
                provider.SubmittedRequests[0].Payload.GetProperty("RequestType").GetString().ShouldBe("IndexItem");
            }
            finally
            {
                // Stop and dispose the host once the assertion has completed.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Builds a test host with the supplied provider registered as a Studio provider.
        /// </summary>
        /// <param name="provider">The controllable provider double used by the ingestion endpoint tests.</param>
        /// <returns>The configured test host application.</returns>
        private static WebApplication BuildApp(TestStudioIngestionProvider provider)
        {
            // Build the host with the minimal rules configuration and the supplied provider double.
            return StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("test-provider", "Test Provider", "Provider used by ingestion endpoint tests."));
                    builder.Services.AddSingleton<IStudioProvider>(provider);
                });
        }

        /// <summary>
        /// Creates the minimal rules configuration needed for ingestion endpoint host startup during tests.
        /// </summary>
        /// <returns>The in-memory rules configuration consumed by the test host builder.</returns>
        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            // Return a single valid rule so the host can complete startup and expose ingestion endpoints.
            return new Dictionary<string, string?>
            {
                ["SkipAddsConfiguration"] = "true",
                ["rules:ingestion:file-share:rule-1"] = """
                    {
                      "schemaVersion": "1.0",
                      "rule": {
                        "id": "rule-1",
                        "title": "Studio service host ingestion test rule",
                        "if": { "path": "id", "exists": true },
                        "then": { "keywords": { "add": [ "k" ] } }
                      }
                    }
                    """
            };
        }

        /// <summary>
        /// Provides a controllable in-memory Studio ingestion provider used by ingestion endpoint tests.
        /// </summary>
        private sealed class TestStudioIngestionProvider : IStudioIngestionProvider
        {
            /// <summary>
            /// Gets or initializes the callback used when payload lookup by identifier is exercised.
            /// </summary>
            public Func<string, Task<StudioIngestionFetchPayloadResult>>? FetchPayloadByIdHandler { get; init; }

            /// <summary>
            /// Gets or initializes the callback used when provider contexts are requested.
            /// </summary>
            public Func<Task<StudioIngestionContextsResponse>>? GetContextsHandler { get; init; }

            /// <summary>
            /// Gets or initializes the callback used when provider-wide ingestion is requested.
            /// </summary>
            public Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? IndexAllHandler { get; init; }

            /// <summary>
            /// Gets or initializes the callback used when context-scoped ingestion is requested.
            /// </summary>
            public Func<string, IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? IndexContextHandler { get; init; }

            /// <summary>
            /// Gets the provider name exposed to the Studio service host.
            /// </summary>
            public string ProviderName => "test-provider";

            /// <summary>
            /// Gets or initializes the callback used when provider-wide reset-indexing-status is requested.
            /// </summary>
            public Func<IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? ResetIndexingStatusHandler { get; init; }

            /// <summary>
            /// Gets or initializes the callback used when context-scoped reset-indexing-status is requested.
            /// </summary>
            public Func<string, IProgress<StudioIngestionOperationProgressUpdate>, CancellationToken, Task<StudioIngestionOperationExecutionResult>>? ResetIndexingStatusForContextHandler { get; init; }

            /// <summary>
            /// Gets the payload submissions captured by the provider double.
            /// </summary>
            public List<StudioIngestionPayloadEnvelope> SubmittedRequests { get; } = [];

            /// <summary>
            /// Gets or initializes the callback used when payload submission is exercised.
            /// </summary>
            public Func<StudioIngestionPayloadEnvelope, Task<StudioIngestionSubmitPayloadResult>>? SubmitPayloadHandler { get; init; }

            /// <summary>
            /// Fetches a payload by identifier using the configured callback or a default not-found result.
            /// </summary>
            /// <param name="id">The provider-defined item identifier to load.</param>
            /// <param name="cancellationToken">The token supplied by the caller. The in-memory test double does not actively observe it.</param>
            /// <returns>The configured payload result or a default not-found result.</returns>
            public Task<StudioIngestionFetchPayloadResult> FetchPayloadByIdAsync(string id, CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a not-found provider response.
                return FetchPayloadByIdHandler is null
                    ? Task.FromResult(StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{id}'."))
                    : FetchPayloadByIdHandler(id);
            }

            /// <summary>
            /// Loads provider contexts using the configured callback or an empty default response.
            /// </summary>
            /// <param name="cancellationToken">The token supplied by the caller. The in-memory test double does not actively observe it.</param>
            /// <returns>The configured contexts response or an empty default response.</returns>
            public Task<StudioIngestionContextsResponse> GetContextsAsync(CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a provider with no contexts.
                return GetContextsHandler is null
                    ? Task.FromResult(new StudioIngestionContextsResponse { Provider = ProviderName, Contexts = [] })
                    : GetContextsHandler();
            }

            /// <summary>
            /// Starts provider-wide ingestion using the configured callback or a default no-op success result.
            /// </summary>
            /// <param name="progress">The progress reporter supplied by the host to capture operation updates.</param>
            /// <param name="cancellationToken">The token supplied by the caller and forwarded to the configured callback.</param>
            /// <returns>The configured execution result or a default successful no-op result.</returns>
            public Task<StudioIngestionOperationExecutionResult> IndexAllAsync(
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a no-op successful ingestion run.
                return IndexAllHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 0 of 0.", 0, 0))
                    : IndexAllHandler(progress, cancellationToken);
            }

            /// <summary>
            /// Starts context-scoped ingestion using the configured callback or a default no-op success result.
            /// </summary>
            /// <param name="context">The provider-neutral context requested by the host.</param>
            /// <param name="progress">The progress reporter supplied by the host to capture operation updates.</param>
            /// <param name="cancellationToken">The token supplied by the caller and forwarded to the configured callback.</param>
            /// <returns>The configured execution result or a default successful no-op result.</returns>
            public Task<StudioIngestionOperationExecutionResult> IndexContextAsync(
                string context,
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a no-op successful context ingestion run.
                return IndexContextHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Processed 0 of 0.", 0, 0))
                    : IndexContextHandler(context, progress, cancellationToken);
            }

            /// <summary>
            /// Starts provider-wide reset-indexing-status using the configured callback or a default no-op success result.
            /// </summary>
            /// <param name="progress">The progress reporter supplied by the host to capture operation updates.</param>
            /// <param name="cancellationToken">The token supplied by the caller and forwarded to the configured callback.</param>
            /// <returns>The configured execution result or a default successful no-op result.</returns>
            public Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusAsync(
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a no-op successful reset operation.
                return ResetIndexingStatusHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 0 items.", 0, 0))
                    : ResetIndexingStatusHandler(progress, cancellationToken);
            }

            /// <summary>
            /// Starts context-scoped reset-indexing-status using the configured callback or a default no-op success result.
            /// </summary>
            /// <param name="context">The provider-neutral context requested by the host.</param>
            /// <param name="progress">The progress reporter supplied by the host to capture operation updates.</param>
            /// <param name="cancellationToken">The token supplied by the caller and forwarded to the configured callback.</param>
            /// <returns>The configured execution result or a default successful no-op result.</returns>
            public Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusForContextAsync(
                string context,
                IProgress<StudioIngestionOperationProgressUpdate> progress,
                CancellationToken cancellationToken = default)
            {
                // Return the configured callback result when present, otherwise emulate a no-op successful reset operation.
                return ResetIndexingStatusForContextHandler is null
                    ? Task.FromResult(StudioIngestionOperationExecutionResult.Success("Reset indexing status for 0 items.", 0, 0))
                    : ResetIndexingStatusForContextHandler(context, progress, cancellationToken);
            }

            /// <summary>
            /// Submits an ingestion payload using the configured callback or a default success result.
            /// </summary>
            /// <param name="request">The payload envelope submitted by the host.</param>
            /// <param name="cancellationToken">The token supplied by the caller. The in-memory test double does not actively observe it.</param>
            /// <returns>The configured submission result or a default success result.</returns>
            public async Task<StudioIngestionSubmitPayloadResult> SubmitPayloadAsync(StudioIngestionPayloadEnvelope request, CancellationToken cancellationToken = default)
            {
                // Store a defensive copy of the submitted payload so later assertions cannot be affected by caller mutation.
                SubmittedRequests.Add(new StudioIngestionPayloadEnvelope
                {
                    Id = request.Id,
                    Payload = request.Payload.Clone()
                });

                // Return the configured callback result when present, otherwise emulate a successful provider submission.
                return SubmitPayloadHandler is null
                    ? StudioIngestionSubmitPayloadResult.Success("Payload submitted successfully.")
                    : await SubmitPayloadHandler(request).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Polls an operations endpoint until the supplied predicate matches the returned operation state.
        /// </summary>
        /// <param name="client">The HTTP client used to poll the host endpoint.</param>
        /// <param name="path">The endpoint path to poll.</param>
        /// <param name="predicate">The predicate that determines when the desired operation state has been observed.</param>
        /// <returns>The first operation state that satisfies the supplied predicate.</returns>
        /// <exception cref="ShouldAssertException">Thrown when the desired operation state is not observed before the retry budget is exhausted.</exception>
        private static async Task<StudioIngestionOperationStateResponse> WaitForOperationStateAsync(
            HttpClient client,
            string path,
            Func<StudioIngestionOperationStateResponse, bool> predicate)
        {
            // Poll the target endpoint with a short retry window because the in-memory operations complete quickly in tests.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var response = await client.GetAsync(path);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var operation = await response.Content.ReadFromJsonAsync<StudioIngestionOperationStateResponse>();
                    if (operation is not null && predicate(operation))
                    {
                        return operation;
                    }
                }

                // Delay briefly between polls so the tracked operation has time to advance its state.
                await Task.Delay(50);
            }

            throw new ShouldAssertException($"Timed out waiting for operation state at '{path}'.");
        }
    }
}
