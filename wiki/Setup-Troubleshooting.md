# Setup troubleshooting

Use this page after you have read [Project setup](Project-Setup) and walked through the practical steps in [Setup walkthrough](Setup-Walkthrough).

## How to use this page

Start with the symptom that most closely matches what you are seeing, then follow the linked setup and tooling pages for deeper context.

## Quick symptom guide

| Symptom | Likely cause | Next page |
|---|---|---|
| `FileShareLoader` cannot find the image | The local Docker tag does not match `fss-data-<environment>` | [Appendix: command reference](Appendix-Command-Reference#image-naming-convention) |
| The import run starts but nothing seems to happen | `FileShareLoader` was not started from the Aspire dashboard | [Setup walkthrough](Setup-Walkthrough#5-start-the-explicit-import-resource) |
| The services stack is up but the emulator has little or no useful data | Import mode never seeded SQL/blob state, or it seeded the wrong `environment` | [Setup walkthrough](Setup-Walkthrough#workflow-1-bring-up-a-local-environment-from-the-shared-data-image) |
| ACR pull or push fails | Azure sign-in, subscription selection, or PIM access is missing | [Appendix: command reference](Appendix-Command-Reference#acr-authentication-and-shared-image-pull) |
| Kibana sign-in fails | The username or `elastic-password` value is wrong for the current Aspire run | [Project setup](Project-Setup#supporting-tools-and-post-start-checks) |
| Rule changes are not reflected locally | The services-mode stack was not restarted after updating repository rules | [Project setup](Project-Setup#configuration-behavior-in-local-aspire) |
| Workbench login or role-based behavior looks wrong | The local Keycloak realm, mapper, or persisted data volume does not match the expected bootstrap state | [Project setup](Project-Setup#why-keycloak-and-workbench-auth-belong-in-setup) |

## Common issues

### The loader cannot find the image

The import path expects the local image name to follow `fss-data-<environment>` exactly.

Check all of the following together:

- `Parameters:environment` in `src/Hosts/AppHost/appsettings.json`
- the local Docker image tag
- the blob container name created by the loader
- the `.bacpac` filename inside the image

If one of those values drifts away from the others, the import workflow can start but fail to find the expected content.

### You changed `environment` and now the seeded state looks wrong

Changing `environment` affects more than one moving part.

The safest correction is to treat the environment label as a complete set of linked values, retag the image to match, and then rerun the import workflow from [Setup walkthrough](Setup-Walkthrough#workflow-1-bring-up-a-local-environment-from-the-shared-data-image).

### You expected import mode to run automatically

`FileShareLoader` is intentionally configured with explicit start.

That means AppHost can start in `runmode=import` without immediately mutating local state, but it also means the operator must start the loader manually from the Aspire dashboard.

### You skipped import mode and the stack still came up

The services stack can be healthy even when the local SQL and blob baseline is empty or stale.

If `FileShareEmulator` shows little or no useful metadata, stop the services run and complete the import workflow first.

### Kibana opens but you cannot sign in

Use:

- username: `kibana_admin`
- password: the current `elastic-password` parameter value from the Aspire dashboard **Parameters** tab

If the password keeps failing, confirm that you are using the value from the active run rather than a copied value from an earlier session.

### Workbench login fails or expected roles are missing

Start by treating this as a setup and auth-baseline problem before treating it as a Workbench bug.

Check all of the following together:

- open the Keycloak admin UI from the Aspire **HTTP** endpoint rather than the HTTPS endpoint
- confirm the `ukho-search` realm exists
- confirm the `search-workbench` client still exists
- confirm the relevant mapper still emits `realm_access.roles`
- force a fresh Workbench sign-in after mapper or role changes

If the current Keycloak state does not match the source-controlled bootstrap you expected, the persisted Keycloak volume may still be serving older realm data.

## Moderately advanced issues

### ACR authentication succeeds but push still fails

A successful `az login` and `az acr login --name searchacr` sequence does not by itself prove that the current session has push rights.

If a push fails after authentication, confirm that:

- you selected the `AbzuUTL` subscription when Azure prompted for a choice
- you are PIM'ed on the subscription before pushing
- the image tag points at `searchacr.azurecr.io/...` rather than only the local short name

### Rules changed but local behaviour still looks stale

In the local setup, AppHost loads repository rules from `rules/` into the configuration emulator during the services-mode run.

If you update rule JSON and do not see the change reflected, restart the services-mode stack so the configuration emulator and runtime services reload the repository content.

### You changed the realm JSON but Keycloak still behaves like the old configuration

The local Keycloak resource uses a persisted data volume, so editing `src/Hosts/AppHost/Realms/ukho-search-realm.json` does not automatically rewrite an already-imported realm.

If you need Keycloak to rebuild from the updated JSON:

1. stop Aspire or stop the Keycloak container
2. identify the Keycloak volume from the current container mounts
3. delete that volume intentionally
4. restart Aspire so Keycloak imports the realm again from the repo `Realms` folder

Use the exact inspection and deletion commands from [Appendix: command reference](Appendix-Command-Reference#keycloak-volume-inspection-and-clean-reimport).

### Realm export or import keeps failing unexpectedly

Two common causes are easy to miss:

- the filename in `src/Hosts/AppHost/Realms/` no longer matches the internal realm name exactly
- stale import files remain in the persisted Keycloak volume even though the repo folder now contains only the correct file

The command appendix includes the inspection commands for both the running container mounts and the import folder inside the volume.

### Missing ZIP behaviour is not what you expected

`IngestionServiceHost` reads `ingestionmode` and maps it to `IngestionModeOptions`.

- `Strict` preserves fail-fast ZIP behaviour.
- `BestEffort` allows missing ZIPs to be skipped when the failure is specifically treated as not found.

If the indexing behaviour does not match what you expected, confirm the active `ingestionMode` value before tracing deeper into provider-specific code.

### Local state needs a clean baseline again

If the emulator data, blob state, or indexes no longer match the image you expect, the quickest recovery path is usually:

1. confirm the intended image tag and `environment`
2. rerun the import workflow
3. restart the services-mode stack
4. use `FileShareEmulator` and Kibana to verify the refreshed state

## What to check before escalating

Before moving deeper into ingestion or provider debugging, confirm the basics in this order:

1. `runmode` is set to the workflow you intended.
2. The image name matches `fss-data-<environment>`.
3. `FileShareLoader` was started explicitly during import mode.
4. `FileShareEmulator` shows meaningful metadata after the import.
5. The services-mode stack is healthy in Aspire.
6. Kibana and `RulesWorkbench` open successfully.

## Related pages

- [Project setup](Project-Setup)
- [Setup walkthrough](Setup-Walkthrough)
- [Appendix: command reference](Appendix-Command-Reference)
- [Tools: `FileShareImageLoader` and `FileShareEmulator`](Tools-FileShareImageLoader-and-FileShareEmulator)
- [Keycloak and Workbench integration](keycloak-workbench-integration)
- [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard)
