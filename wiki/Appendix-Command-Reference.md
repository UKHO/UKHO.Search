# Appendix: command reference

Use this page as the exact-command companion to [Project setup](Project-Setup) and [Setup walkthrough](Setup-Walkthrough).

This appendix keeps the high-value operational commands together so the main setup pages can stay narrative-first without losing verbatim accuracy.

## Local AppHost start command

Use this command when you want to start the Aspire-orchestrated local environment from the command line instead of launching from Visual Studio.

```powershell
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

## ACR authentication and shared image pull

Use this sequence when you need the shared File Share data image from the `searchacr` registry.

```powershell
az login
az acr login --name searchacr
docker pull searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker tag searchacr.azurecr.io/fss-data-vnext-e2e:latest fss-data-vnext-e2e:latest
docker rmi searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

Operational notes:

- When `az login` lists available subscriptions, select `AbzuUTL`.
- The retag matters because AppHost expects the local image name to follow `fss-data-<environment>`.

## Shared image push

Use this sequence when you have intentionally refreshed the local image and need to publish it back to the shared registry.

```powershell
az login
az acr login --name searchacr
docker tag fss-data-vnext-e2e:latest searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker push searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

Operational notes:

- When `az login` lists available subscriptions, select `AbzuUTL`.
- Ensure you are PIM'ed on the subscription before pushing.

## Image naming convention

The local image name must match this convention:

- `fss-data-<environment>`

So if `environment` is `vnext-e2e`, the loader expects:

- `fss-data-vnext-e2e`

## Keycloak realm export

Use this sequence when you need a full local Keycloak realm export that preserves users, groups, roles, clients, scopes, and mapper changes for the `ukho-search` bootstrap.

Find the running Keycloak container:

```powershell
docker ps --format "{{.Names}}`t{{.Image}}" | Select-String keycloak
```

Inspect the image used by that container:

```powershell
docker inspect keycloak-48f329f9 --format "{{.Config.Image}}"
```

Stop the running Keycloak container before the offline export:

```powershell
docker stop keycloak-48f329f9
```

Create a host export folder:

```powershell
New-Item -ItemType Directory -Force D:\Temp\keycloak-export | Out-Null
```

Run the full export using the same persisted Keycloak data volume:

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export <image-from-step-2> export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Example with an explicit image value:

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export quay.io/keycloak/keycloak:latest export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Copy the exported file back into the repository bootstrap folder:

```powershell
Copy-Item D:\Temp\keycloak-export\ukho-search-realm.json D:\Dev\UKHO\UKHO.Search\src\Hosts\AppHost\Realms\ukho-search-realm.json -Force
```

Start Keycloak again if you stopped only the container:

```powershell
docker start keycloak-48f329f9
```

Operational notes:

- Keep the filename as `ukho-search-realm.json` so it matches the realm name exactly.
- The `--users same_file` flag is what makes the export suitable for a full local bootstrap.
- Use the Aspire **HTTP** endpoint for the local Keycloak admin UI.

## Keycloak volume inspection and clean re-import

Use this sequence when the local Keycloak state appears stale after editing the realm JSON or when you need to inspect the persisted import state.

Inspect the current container mounts:

```powershell
docker inspect keycloak-48f329f9 --format "{{json .Mounts}}"
```

Inspect the persisted import folder in the Keycloak data volume:

```powershell
docker run --rm -v <keycloak-volume-name>:/data alpine sh -c "ls -la /data/import"
```

Delete the persisted Keycloak volume when you intentionally want a clean realm re-import on the next Aspire start:

```powershell
docker volume rm <keycloak-volume-name>
```

Operational notes:

- Only the correctly named realm file should remain in the import folder for a clean startup.
- Deleting the volume removes the persisted Keycloak database and import cache state, so use it only when a fresh bootstrap is intended.

## Related pages

- [Project setup](Project-Setup)
- [Setup walkthrough](Setup-Walkthrough)
- [Setup troubleshooting](Setup-Troubleshooting)
- [Tools: `FileShareImageLoader` and `FileShareEmulator`](Tools-FileShareImageLoader-and-FileShareEmulator)
- [Keycloak and Workbench integration](keycloak-workbench-integration)
