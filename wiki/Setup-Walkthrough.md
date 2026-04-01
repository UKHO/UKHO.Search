# Setup walkthrough

Use this page when you want the practical execution path after reading [Project setup](Project-Setup).

## Reading path

- Start with [Project setup](Project-Setup) for the concepts, run modes, and repository-specific vocabulary that shape the local environment.
- Keep [Appendix: command reference](Appendix-Command-Reference) nearby for the exact operational commands that must stay verbatim.
- Use [Setup troubleshooting](Setup-Troubleshooting) if the workflow below does not behave as expected.
- Refer back to [Glossary](Glossary), [Solution architecture](Solution-Architecture), and [Architecture walkthrough](Architecture-Walkthrough) when you need more context on the services that appear during startup.

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

When you are changing rule JSON under `rules/file-share/...`, you usually do not need to repeat the full import.

Instead:

1. Keep the seeded local baseline.
2. Run in `services` mode.
3. Use `RulesWorkbench` and `FileShareEmulator` to drive targeted indexing.
4. Re-import only when the underlying data image needs to change.

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
- [Tools: `FileShareImageLoader` and `FileShareEmulator`](Tools-FileShareImageLoader-and-FileShareEmulator)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Keycloak and Workbench integration](keycloak-workbench-integration)
- [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard)
