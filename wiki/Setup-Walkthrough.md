# Setup walkthrough

Use this page when you want the practical execution path after reading [Project setup](Project-Setup).

## Reading path

- Start with [Project setup](Project-Setup) for the concepts, run modes, and repository-specific vocabulary that shape the local environment.
- Keep [Appendix: command reference](Appendix-Command-Reference) nearby for the exact operational commands that must stay verbatim.
- Use [Setup troubleshooting](Setup-Troubleshooting) if the workflow below does not behave as expected.
- Refer back to [Glossary](Glossary), [Solution architecture](Solution-Architecture), and [Architecture walkthrough](Architecture-Walkthrough) when you need more context on the services that appear during startup.
- Keep [Query walkthrough](Query-Walkthrough), [Query signal extraction rules](Query-Signal-Extraction-Rules), and [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping) nearby when the workflow below reaches query-side verification.

## Workflow 1: bring up a local environment from the shared data image

This is the standard path for a developer who wants a known-good File Share data baseline before starting the normal services stack.

### 1. Sign in to Azure and the shared registry

When you are using the shared image hosted in ACR, use the following commands exactly as written:

```powershell
az login
az acr login --name searchacr
```

When `az login` lists available subscriptions, select `AbzuUTL`.

### 2. Pull and retag the shared data image

The AppHost import workflow expects the local image naming convention to match `fss-data-<environment>`, so pull and retag the shared image before you start the importer.

```powershell
docker pull searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker tag searchacr.azurecr.io/fss-data-vnext-e2e:latest fss-data-vnext-e2e:latest
docker rmi searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

If your local `environment` parameter is not `vnext-e2e`, use the same naming pattern described in [Appendix: command reference](Appendix-Command-Reference#image-naming-convention).

### 3. Switch AppHost into import mode

Open `src/Hosts/AppHost/appsettings.json` and set `runmode` to `import`.

This tells the local orchestration flow to start the data-seeding path instead of the day-to-day services stack.

### 4. Start AppHost

You can start `AppHost` from Visual Studio, or from the command line with:

```powershell
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

### 5. Start the explicit import resource

Open the Aspire dashboard and explicitly start `FileShareLoader`.

That explicit-start step matters because import mode intentionally keeps the loader under operator control so you can confirm the image and configuration before seeding local state.

### 6. Wait for import to complete

Use the Aspire dashboard and [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard) guidance to confirm that the import has finished cleanly.

When the import succeeds, `FileShareEmulator` should later show meaningful metadata statistics instead of an empty local state.

### 7. Switch into services mode for normal development

Stop the import-mode run, set `runmode` back to `services`, and start `AppHost` again.

The services-mode run is the normal developer environment because it brings up:

- Azurite
- SQL Server
- Keycloak
- Elasticsearch and Kibana
- `IngestionServiceHost`
- `QueryServiceHost`
- `FileShareEmulator`
- `RulesWorkbench`

### 8. Perform the first health check

After the services-mode run starts:

1. Open the Aspire dashboard.
2. Confirm that the core hosts and dependencies are healthy.
3. Open `FileShareEmulator` and check the home page statistics.
4. Open Kibana from the Aspire dashboard if you need to inspect Elasticsearch state.
5. Open `RulesWorkbench` if your next step is rule authoring or rule diagnosis.

### 9. Verify the local auth surface when Workbench access matters

If your next task involves Workbench, role-based behavior, or other authenticated flows, do one more check before you move on.

1. Open the Keycloak admin UI from the **HTTP** endpoint shown in the Aspire dashboard.
2. Confirm that the `ukho-search` realm is present.
3. Confirm that the `search-workbench` client exists when you need to test Workbench sign-in.
4. Then open Workbench and force a fresh sign-in if you are validating role or mapper changes.

This step is easy to skip, but it closes a real setup gap. A healthy-looking services-mode run can still hide a stale Keycloak data volume or a realm bootstrap drift problem, and those issues tend to appear later as confusing Workbench authorization failures rather than as obvious setup failures.

## Workflow 2: daily services-mode development

After a local machine already has seeded SQL and blob state, the day-to-day loop is shorter:

1. Keep `runmode` set to `services`.
2. Start `AppHost`.
3. Use `FileShareEmulator` to submit indexing batches or inspect queue state.
4. Watch Aspire logs and metrics while the ingestion host processes work.
5. Use Kibana when you need index inspection, management, or ad-hoc queries.

This workflow keeps the expensive seed step out of the normal edit-run-debug loop.

## Workflow 2A: verify query-rule work in services mode

This is the practical loop to use after editing `rules/query/*.json` or when you need to prove that the current local query runtime is healthy before deeper debugging starts.

### 1. Keep `runmode=services` and start `AppHost`

The query verification path depends on the same local services-mode stack described in [Project setup](Project-Setup): `QueryServiceHost`, Elasticsearch, Kibana, Keycloak, and the configuration emulator that seeds the repository `rules` directory into configuration.

### 2. Open `QueryServiceHost`

Use the Aspire dashboard to open the query host endpoint.

This matters because query-rule verification is not just an index-inspection task. You need to exercise the real host, the real planner, and the real Elasticsearch execution adapter together.

### 3. Watch the query-side logs while you search

Open the `QueryServiceHost` logs in Aspire before running the example searches below.

The current runtime logs useful boundaries during planning and execution, including normalization, typed extraction, query-plan generation, and Elasticsearch execution. Those messages help you decide whether the problem is in rule loading, interpretation, or execution.

### 4. Run `latest SOLAS`

This is the simplest high-value verification query because it exercises concept expansion and recency intent at the same time.

Healthy current behavior looks like this:

- the query normalizes to `latest solas`
- the rules engine matches the representative SOLAS concept and latest-sort rules
- the plan gains canonical keyword intent such as `solas`, `maritime`, `safety`, and `msi`
- the plan gains descending sort intent on `majorVersion` and `minorVersion`
- the residual default layer becomes empty because the decisive terms were consumed by the rules

If that does not happen, start by asking whether the edited repository files were actually reseeded into `rules:query:*` rather than assuming Elasticsearch is the first fault.

### 5. Run `latest SOLAS msi`

This query is useful because it proves the runtime can keep rule-owned meaning and surviving residual meaning at the same time.

Healthy current behavior looks like this:

- the same SOLAS concept expansion and latest-sort behavior still appears
- the extra user-supplied token `msi` survives the residual path because the representative rules did not consume it
- the resulting request shape therefore contains both rule-shaped canonical intent and residual default contributions

This is an important local check because it proves the system is not flattening every successful rules-backed query into an all-or-nothing residual outcome.

### 6. Run `latest notice`

This query is the practical query-side verification for explicit execution directives.

Healthy current behavior looks like this:

- the latest-sort behavior still applies
- the notice-shaping rule contributes the current notice-oriented filter and boost behavior before residual defaults run
- the runtime treats `notice` as more than a plain keyword by shaping execution policy rather than only adding broad text matching

This search is especially valuable when contributors are debugging why results look weakly ranked or too broad. It helps separate a missing filter-or-boost rule outcome from an indexing problem.

### 7. Cross-check Kibana when the behavior is unclear

If the searches above still look suspicious, open Kibana from the Aspire dashboard and inspect the indexed canonical documents directly.

This step matters because query debugging has two distinct failure surfaces:

- the query plan may be wrong even though the indexed documents are correct
- the query plan may be correct even though the indexed documents are missing the canonical fields the plan expects

Kibana is therefore part of the verification loop, not just a later troubleshooting tool.

### 8. Use the logs to decide what to fix next

If the runtime is healthy, the logs and the three representative searches together should let you answer these questions in order:

1. did the host start with the expected services-mode configuration?
2. did the current query rules appear to load and match?
3. did the planner produce the canonical intent, filters, boosts, and sorts you expected?
4. did the resulting query execute against Elasticsearch successfully?

That sequence keeps query-rule verification grounded in the actual runtime architecture instead of collapsing every symptom into one vague “search is broken” diagnosis.

## Workflow 3: refresh and publish the shared image

Use this flow only when you have intentionally built or refreshed the local image and need to publish it back to the shared ACR registry.

### 1. Authenticate and confirm subscription access

Run the following commands exactly as written:

```powershell
az login
az acr login --name searchacr
```

When `az login` lists subscriptions, select `AbzuUTL`, and ensure that you are PIM'ed on the subscription before you push.

### 2. Tag and push the local image

```powershell
docker tag fss-data-vnext-e2e:latest searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker push searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

Use the same naming pattern if you are working with a different `environment` value.

## Practical examples

### Example: validating that import mode prepared useful local data

If the services stack starts but the emulator looks empty, revisit Workflow 1 rather than trying to diagnose ingestion first.

The most common cause is that import mode did not run to completion, or `FileShareLoader` was never started explicitly.

### Example: iterating on rules after the first import

When you are changing rule JSON under `rules/ingestion/file-share/...`, you usually do not need to repeat the full import.

Instead:

1. Keep the seeded local baseline.
2. Run in `services` mode.
3. Use `RulesWorkbench` and `FileShareEmulator` to drive targeted indexing.
4. Re-import only when the underlying data image needs to change.

That workflow works because the local configuration seeder reads the repository `rules/` root every time services mode starts. The nested `ingestion/file-share` folders are preserved as configuration key segments, so editing a file in that location is enough to reseed the corresponding `rules:ingestion:file-share:*` entry on the next local start.

The same logic now applies to query-side rule iteration. Editing `rules/query/<rule-id>.json` changes the file-backed authoring surface, and services mode reseeds that file into the flat `rules:query:<rule-id>` configuration namespace used by the query rules catalog. The practical difference is in verification: for ingestion rules you normally drive batches through `FileShareEmulator`, while for query rules you normally prove the change through `QueryServiceHost`, query-side logs, and Kibana.

### Example: validating query-rule behavior after a local edit

Suppose you update a query rule that should affect `latest notice`.

1. Keep the seeded local baseline and run in `services` mode.
2. Restart or refresh the services stack so the configuration emulator and query rules catalog can see the edited `rules/query/*.json` file.
3. Open `QueryServiceHost` and run `latest notice`.
4. Check the query host logs for matched rule ids and applied execution diagnostics.
5. If the query still looks wrong, inspect Kibana before changing the rule again so you can tell whether the problem is in planning or in the indexed canonical data.

This is the shortest realistic verification loop for query-rule work in the current repository.

### Example: validating a Keycloak realm or mapper change

When you change local Keycloak users, roles, mappers, or other realm settings, keep the verification loop separate from ingestion debugging.

1. Apply the change in Keycloak or update the realm JSON.
2. If you changed the source-controlled realm file, follow the clean re-import guidance in [Setup troubleshooting](Setup-Troubleshooting).
3. Open the admin UI again through the Aspire HTTP endpoint.
4. Force a fresh Workbench sign-in.
5. Only after the auth flow looks correct should you continue with module, role, or tool-specific debugging.

## Related pages

- [Project setup](Project-Setup)
- [Setup troubleshooting](Setup-Troubleshooting)
- [Appendix: command reference](Appendix-Command-Reference)
- [Query pipeline](Query-Pipeline)
- [Query walkthrough](Query-Walkthrough)
- [Query signal extraction rules](Query-Signal-Extraction-Rules)
- [Query model and Elasticsearch mapping](Query-Model-and-Elasticsearch-Mapping)
- [Tools: `FileShareImageLoader` and `FileShareEmulator`](Tools-FileShareImageLoader-and-FileShareEmulator)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Keycloak and Workbench integration](keycloak-workbench-integration)
- [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard)
