# Keycloak browser-host integration

## Purpose

This document explains how Keycloak is wired into the local Aspire developer environment for the repository's browser-facing hosts, how the `ukho-search` realm is configured, how role claims flow into host authorization, and how to recreate the full realm export later when users, roles, groups, clients, or mappers change.

This is intended to be the single operational reference for the local Keycloak setup.

At the time of writing, the shared browser-host authentication model applies directly to `WorkbenchHost`, `IngestionServiceHost`, and `QueryServiceHost`. The page still uses Workbench as the original narrative anchor because Workbench established the first complete host integration, but the authentication composition root, lifecycle endpoints, and role-claims normalization path are now shared host concerns rather than Workbench-only behavior.

## Current realm and import file

The current realm name is:

- `ukho-search`

The realm import file used by Aspire is:

- `src/Hosts/AppHost/Realms/ukho-search-realm.json`

Keycloak is strict about the import file naming convention. The filename must match the realm name inside the JSON:

- realm name in JSON: `ukho-search`
- required filename: `ukho-search-realm.json`

If the filename and the internal realm name do not match exactly, Keycloak startup import fails.

## Where the integration is configured in code

### Aspire AppHost

Keycloak is added to the local Aspire environment in:

- `src/Hosts/AppHost/AppHost.cs`

The important setup is the Keycloak resource registration:

- `builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsernameParameter, keyCloakPasswordParameter)`
- `.WithDataVolume()`
- `.WithRealmImport("./Realms")`

What this means:

- Aspire starts a local Keycloak container
- Keycloak persists its database in a Docker volume
- Keycloak imports realm JSON files from the `Realms` folder on first startup against an empty data volume

Because `.WithDataVolume()` is enabled, changing the JSON file later does **not** update an already-imported realm automatically. To force a clean re-import after changing the JSON, the Keycloak data volume must be deleted before restarting Aspire.

### Shared browser-host OpenID Connect foundation

The repository no longer keeps the browser authentication wiring inside `WorkbenchHost` alone. The shared composition root now lives in:

- `src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs`
- `src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs`
- `src/Hosts/UKHO.Search.ServiceDefaults/KeycloakRealmRoleClaimsTransformation.cs`

This shared composition root is important because it gives browser-facing hosts one obvious way to opt into the repository's Keycloak model. A **composition root** is the point where a host stitches together infrastructure and runtime behavior. In this repository, the composition root for browser authentication now sits in `UKHO.Search.ServiceDefaults`, which means the authentication plumbing can be reused without pushing UI logic into lower layers or copying host-local startup code.

The shared service-registration path configures the following pieces together so they do not drift apart over time:

- the cookie authentication scheme that stores the local browser session
- the OpenID Connect challenge scheme that redirects the browser to Keycloak
- the fallback authorization policy that requires an authenticated user everywhere unless an endpoint explicitly opts out
- the Blazor cascading authentication state needed by interactive components
- the Keycloak realm-role claims transformation that normalizes roles into `ClaimTypes.Role`

The shared registration path now also deliberately isolates the cookie names used by each browser host on `localhost`. This matters because HTTP cookies are scoped by domain and path, not by TCP port. In practical terms, `https://localhost:7161`, `https://localhost:7152`, and `https://localhost:10000` all participate in the same browser cookie namespace unless the application gives them different cookie names. Without that isolation, one browser host can end up receiving another host's authentication, nonce, or correlation cookies, which is a common source of noisy sign-in failures, nonce mismatches, and eventually oversized request headers.

The shared registration path also no longer saves OpenID Connect tokens into the local authentication cookie. The browser hosts in this repository only need a local authenticated session and normalized role claims; they do not currently call downstream APIs with the saved tokens. Keeping the tokens out of the cookie significantly reduces cookie size, which in turn reduces the risk of `HTTP 431` errors when moving between several protected localhost browser hosts during the same development session.

The shared endpoint-registration path maps the explicit authentication lifecycle endpoints under `/authentication`. In this page, **lifecycle endpoints** means the small host endpoints that begin login or complete logout for the browser shell without being part of the normal application UI.

### WorkbenchHost composition root

`WorkbenchHost` now consumes that shared foundation from:

- `src/workbench/server/WorkbenchHost/Program.cs`

The host calls:

- `builder.Services.AddKeycloakBrowserHostAuthentication("search-workbench")`
- `app.MapKeycloakBrowserHostAuthenticationEndpoints()`

The Keycloak client remains:

- `search-workbench`

The important behavioral details are intentionally unchanged from the earlier Workbench-only wiring:

- the Keycloak realm is still `ukho-search`
- the sign-in flow still uses the OpenID Connect authorization-code flow
- `RequireHttpsMetadata` remains disabled for the local developer environment
- tokens are no longer persisted in the local authentication cookie
- the local authenticated browser session is still maintained with the ASP.NET Core cookie scheme

In addition, Workbench now uses a host-specific cookie-name prefix for its local authentication, nonce, and correlation cookies. This keeps the Workbench cookies isolated from the cookies emitted by `IngestionServiceHost` and `QueryServiceHost` even though all three hosts run on `localhost`.

Because the browser hosts currently share one public Keycloak client, the client's allowed redirect and logout callback URLs must cover every local host URL that participates in that shared model. In the checked-in realm export, that means the `search-workbench` client now includes the Workbench HTTPS callback, the IngestionServiceHost HTTPS callback, and the QueryServiceHost HTTPS callback. If one of those URLs is missing, Keycloak rejects the authorization request with an `invalid_request` error for `redirect_uri`.

At present the shared client must allow these local HTTPS URLs:

- `https://localhost:10000/signin-oidc`
- `https://localhost:10000/signout-callback-oidc`
- `https://localhost:10000`
- `https://localhost:7152/signin-oidc`
- `https://localhost:7152/signout-callback-oidc`
- `https://localhost:7152`
- `https://localhost:7161/signin-oidc`
- `https://localhost:7161/signout-callback-oidc`
- `https://localhost:7161`

The exact port numbers matter. OpenID Connect clients treat the redirect URI as an exact contract, and the port is part of that URI. If a host starts on a different port than the one recorded in Keycloak, Keycloak considers the request invalid even if the host name and path look correct.

### IngestionServiceHost composition root

`IngestionServiceHost` now consumes the same shared foundation from:

- `src/Hosts/IngestionServiceHost/Program.cs`
- `src/Hosts/IngestionServiceHost/Components/Routes.razor`
- `src/Hosts/IngestionServiceHost/Components/Authentication/RedirectToLogin.razor.cs`

The host calls the same service-registration and endpoint-registration extensions as Workbench:

- `builder.Services.AddKeycloakBrowserHostAuthentication("search-workbench")`
- `app.MapKeycloakBrowserHostAuthenticationEndpoints()`

That means the ingestion UI now follows the same browser authentication story as Workbench. An unauthenticated request for the normal UI reaches the host, the fallback authorization policy detects that no signed-in user is present, and the host issues an OpenID Connect challenge through Keycloak. After sign-in, the ASP.NET Core cookie session restores the authenticated principal for later requests and for the interactive Blazor circuit.

`IngestionServiceHost` also uses authorization-aware routing inside `Components/Routes.razor`. The router uses `AuthorizeRouteView`, which is Blazor's route-view component that understands authorization state. When route authorization concludes that the current user is not authorized, the host's `RedirectToLogin` component performs a full-page navigation to `/authentication/login`. In practice, this gives the ingestion host a second protective layer: the server-side fallback authorization policy protects the initial HTTP request, and the Blazor router keeps later interactive navigation aligned with the same authenticated-user expectation.

Like the other browser hosts, `IngestionServiceHost` now also uses host-specific cookie names for its local authentication, nonce, and correlation cookies. This prevents the ingestion UI from trying to participate in another host's in-flight authentication round-trip merely because the browser is still holding cookies for another `localhost` port.

### QueryServiceHost composition root

`QueryServiceHost` now consumes the same shared foundation from:

- `src/Hosts/QueryServiceHost/Program.cs`
- `src/Hosts/QueryServiceHost/Components/Routes.razor`
- `src/Hosts/QueryServiceHost/Components/Authentication/RedirectToLogin.razor.cs`

The host calls the same service-registration and endpoint-registration extensions as the other protected browser hosts:

- `builder.Services.AddKeycloakBrowserHostAuthentication("search-workbench")`
- `app.MapKeycloakBrowserHostAuthenticationEndpoints()`

That means the query UI now follows the same browser authentication story as Workbench and Ingestion. An unauthenticated request for the normal UI reaches the host, the fallback authorization policy detects that no signed-in user is present, and the host issues an OpenID Connect challenge through Keycloak. After sign-in, the ASP.NET Core cookie session restores the authenticated principal for later requests and for the interactive Blazor circuit.

`QueryServiceHost` also now uses authorization-aware routing inside `Components/Routes.razor`. The router uses `AuthorizeRouteView`, which is Blazor's route-view component that understands authorization state. When route authorization concludes that the current user is not authorized, the host's `RedirectToLogin` component performs a full-page navigation to `/authentication/login`. This keeps the query host aligned with the other protected browser hosts by combining the server-side fallback authorization policy with an interactive-routing guard inside the Blazor router.

The checked-in `src/Hosts/QueryServiceHost/Properties/launchSettings.json` file is part of this local contract rather than an incidental developer-only file. Its HTTPS port is the source of truth for the local callback URLs that must also exist on the shared Keycloak client. If the launch settings port changes, the realm export must be updated to match before local sign-in will work again.

`QueryServiceHost` also participates in the host-specific cookie-name isolation described earlier in this page. That isolation is especially important when developers authenticate to Query first and then open Ingestion or Workbench in the same browser session, because it prevents the other hosts from inheriting Query's transient OpenID Connect cookies or needlessly carrying Query's larger session state into their own challenge round-trips.

### Login and logout endpoints

Workbench now inherits the explicit auth endpoints from the shared browser-host endpoint extension in:

- `src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs`

Current endpoints:

- `GET /authentication/login`
- `GET /authentication/logout`
- `POST /authentication/logout`

These endpoints remain useful during testing when forcing a fresh login after mapper or role changes. They are also intentionally anonymous so a signed-out or expired browser session can still begin a challenge or complete a clean sign-out round-trip.

Because the endpoint mapping is now shared, these routes apply to `WorkbenchHost`, `IngestionServiceHost`, and `QueryServiceHost`. They are the only deliberately anonymous browser lifecycle routes in the shared host-auth model.

## How role claims reach Workbench

Role claim transformation is now implemented in the shared host-auth foundation at:

- `src/Hosts/UKHO.Search.ServiceDefaults/KeycloakRealmRoleClaimsTransformation.cs`

The transformer currently reads role information from these claim shapes:

- `roles`
- `realm_access.roles`
- `realm_access` containing JSON with a `roles` array

It then adds ASP.NET role claims of type:

- `System.Security.Claims.ClaimTypes.Role`

This is why the mapper configuration matters. The shared claims transformation means any browser host that adopts the same service-defaults extension will see the same normalized ASP.NET Core role claims, which keeps authorization behavior aligned across hosts instead of letting each host parse Keycloak claims differently.

## Required Keycloak mapper configuration

For the Workbench client, the role mapper is configured in Keycloak under:

- `Clients`
- select client `search-workbench`
- `Client details`
- `Dedicated scopes`
- `Mapper details`

Important required settings:

- the mapping is for **realm roles**
- token claim name must be `realm_access.roles`
- `Add to ID token` must be **On**

Why this matters:

- the Workbench login principal is built from OpenID Connect identity information
- the claims transformer reads the claims on that principal
- if the mapper is not emitted into the ID token, the roles will not appear in Workbench even if they exist in Keycloak

### Very important: assign the correct role type

This mapping is for **realm roles**, not client roles.

When assigning permissions to a user, make sure you add the role as a **realm role** on the user. If a client role is assigned instead, the current mapper/transformer path will not produce the expected Workbench role claims.

## Accessing Keycloak locally

When launching through Aspire, access the Keycloak admin UI from the **HTTP** endpoint shown in the Aspire dashboard.

- use the **HTTP** endpoint
- do **not** use the HTTPS endpoint

For this local setup, the HTTPS endpoint does not work for the admin UI path used during development.

The default Keycloak admin username and password are available in the Aspire dashboard:

- open the Aspire dashboard
- go to the `Parameters` tab
- find the Keycloak username and password parameters there

## Fresh realm import behavior

The realm import JSON is only used automatically when Keycloak starts with a fresh empty data store.

If the Keycloak volume already contains a previously imported realm, Keycloak will keep using the persisted state and ignore later JSON changes.

To force a clean import:

1. stop Aspire or stop the Keycloak container
2. delete the persisted Keycloak Docker volume
3. restart Aspire

This causes Keycloak to start from scratch and import the realm JSON from `src/Hosts/AppHost/Realms/` again.

This matters especially for redirect-URI fixes. When you update the `search-workbench` client in `src/Hosts/AppHost/Realms/ukho-search-realm.json`, an already-running Keycloak container does not pick up those new redirect and logout callback URLs automatically if the persistent data volume already contains an older imported realm. In that situation the code can be correct while Keycloak still enforces stale client settings.

## Troubleshooting shared browser-host sign-in

The most common local sign-in failures for `WorkbenchHost`, `IngestionServiceHost`, and `QueryServiceHost` fall into two categories: the host cannot reach Keycloak at all, or Keycloak is reachable but rejects the browser's redirect URI. The distinction matters because the symptoms look similar to a user, but the corrective action is different.

### Symptom: `No such host is known` for `keycloak`

If the exception chain includes messages such as `No such host is known`, `keycloak:443`, or failure to retrieve `/.well-known/openid-configuration`, the host usually started without the Aspire-provided Keycloak connection details.

In practice this means the browser host was launched outside the expected AppHost orchestration path. The shared browser-host authentication setup expects the host to receive its Keycloak connection through Aspire resource wiring. When that wiring is absent, the hostname `keycloak` is treated as if it were a normal DNS entry, and the machine running the browser host cannot resolve it.

Use this checklist:

1. start the environment through `src/Hosts/AppHost`
2. do not launch `WorkbenchHost`, `IngestionServiceHost`, or `QueryServiceHost` directly when verifying the shared Keycloak flow
3. confirm the relevant host has an Aspire reference to the Keycloak resource in `AppHost.cs`
4. confirm Keycloak is actually running before the browser host starts

If this is the failure mode, changing redirect URIs in Keycloak will not help until the host is running under the correct Aspire wiring.

### Symptom: `invalid_request` with `Invalid parameter: redirect_uri`

If Keycloak is reachable but the exception says `invalid_request` and specifically mentions `redirect_uri`, the host was able to start the OpenID Connect flow but the current browser URL does not match one of the callback URLs recorded on the shared `search-workbench` client.

This usually happens after one of these changes:

- a browser host starts on a different local HTTPS port
- the checked-in realm JSON was updated but Keycloak is still using an older imported copy from its persistent volume
- the Keycloak client contains the Workbench callback URLs but is missing either the IngestionServiceHost or QueryServiceHost callback URLs

Use this checklist:

1. identify the exact host URL shown in the browser, including the port
2. confirm that the corresponding `signin-oidc`, `signout-callback-oidc`, and root URL are present on the `search-workbench` client in `src/Hosts/AppHost/Realms/ukho-search-realm.json`
3. if the realm JSON was corrected recently, stop the stack, delete the persisted Keycloak Docker volume, and restart AppHost so Keycloak re-imports the realm
4. retry the sign-in flow only after the fresh import completes

The key idea is that Keycloak evaluates the redirect URI exactly. `https://localhost:7152/signin-oidc`, `https://localhost:7161/signin-oidc`, and `https://localhost:10000/signin-oidc` are different values, not interchangeable variants of the same callback.

### Symptom: `HTTP ERROR 431`

If one browser host signs in successfully but loading another browser host on `localhost` then fails with `HTTP ERROR 431`, the browser is usually sending too much cookie data back to the second host. An HTTP 431 response means the request headers are too large. In this authentication scenario, the oversized headers usually come from cookies rather than from custom request headers typed by a user.

In a multi-host localhost setup, this can happen when several protected browser hosts reuse the same cookie namespace or when a host stores more state in its authentication cookie than it truly needs. Because cookies are domain-based rather than port-based, `localhost` browser hosts can accidentally burden each other with authentication cookies that were really intended for only one host.

The current repository design addresses this in three ways:

1. each browser host now uses its own cookie-name prefix for the local authentication, nonce, and correlation cookies; and
2. the shared browser-host authentication path no longer saves OpenID Connect tokens into the local authentication cookie; and
3. the shared browser-host authentication path now stores the full authentication ticket in server-side memory so the browser cookie only carries a compact session identifier.

That third point matters because removing saved tokens from the cookie reduces payload size, but a cookie can still become larger than desirable if it carries the full serialized authentication ticket. By moving the ticket into a server-side session store, the browser sends only a short key back to the host, which keeps request headers small even when developers move repeatedly between protected localhost hosts in the same browser session.

If you still see `HTTP ERROR 431` after pulling the latest code, clear the browser cookies for `localhost` or use a fresh InPrivate/incognito session before retrying. Old cookies created by an earlier version of the shared authentication setup can remain in the browser until they expire or are deleted.

### Quick diagnosis table

| Symptom | Likely cause | First thing to check |
| --- | --- | --- |
| `No such host is known` for `keycloak` | Host is not running with the Aspire Keycloak reference | Start through `AppHost` and verify the Keycloak resource reference |
| `invalid_request` / `Invalid parameter: redirect_uri` | Keycloak client redirect settings do not match the current browser host URL | Compare the current HTTPS port and callback URLs against the realm JSON |
| Sign-in still fails after updating the realm JSON | Keycloak is using stale imported client settings from its persistent volume | Delete the Keycloak volume and restart so the realm imports again |
| `HTTP ERROR 431` after signing into one localhost browser host and opening another | Browser is still sending oversized authentication cookies across the shared `localhost` cookie namespace | Use a fresh browser session and confirm the host is running the latest shared authentication code with compact cookies and server-side ticket storage |

## Full realm export: when to use it

Use a full realm export whenever you change any of the following in Keycloak and want those changes preserved in source control for future local environments:

- users
- groups
- realm roles
- group memberships
- user role assignments
- clients
- client scopes
- protocol mappers
- redirect URIs
- client secrets or settings
- realm settings generally

Do **not** rely on the Keycloak admin UI partial export for this. The UI export is not the right mechanism for a complete local bootstrap.

Use the Keycloak CLI export command instead.

## Full realm export procedure

These steps produce a **full export** that includes users, groups, roles, mappings, clients, scopes, and other realm data.

### Summary

- stop the running Keycloak container first
- use a one-off container to run the export offline against the same mounted Keycloak data
- export to a host folder
- copy the generated file into `src/Hosts/AppHost/Realms/`
- ensure the filename matches the realm name exactly

### Step 1: find the running container

Example current container name:

- `keycloak-48f329f9`

To find it later if the name changes:

```powershell
docker ps --format "{{.Names}}`t{{.Image}}" | Select-String keycloak
```

### Step 2: find the image used by the container

```powershell
docker inspect keycloak-48f329f9 --format "{{.Config.Image}}"
```

This returns the image name to use in the export command.

### Step 3: stop the Keycloak container

Keycloak CLI export is an offline operation for this local H2-backed setup.

```powershell
docker stop keycloak-48f329f9
```

### Step 4: create a host export folder

```powershell
New-Item -ItemType Directory -Force D:\Temp\keycloak-export | Out-Null
```

### Step 5: run the full export

Run a one-off container using the same persisted Keycloak data volume as the stopped container.

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export <image-from-step-2> export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Example using an explicit image value:

```powershell
docker run --rm --volumes-from keycloak-48f329f9 -v D:\Temp\keycloak-export:/export quay.io/keycloak/keycloak:latest export --realm ukho-search --users same_file --file /export/ukho-search-realm.json
```

Important flag:

- `--users same_file`

This is what puts users into the same realm JSON so the export is suitable for a complete local developer bootstrap.

### Step 6: copy the exported file into the repo

```powershell
Copy-Item D:\Temp\keycloak-export\ukho-search-realm.json D:\Dev\UKHO\UKHO.Search\src\Hosts\AppHost\Realms\ukho-search-realm.json -Force
```

### Step 7: restart Keycloak if needed

If you stopped only the Keycloak container and are not immediately restarting Aspire:

```powershell
docker start keycloak-48f329f9
```

## File naming rules for AppHost/Realms

The import file in `src/Hosts/AppHost/Realms/` must follow this format:

- `<realm-name>-realm.json`

For the current realm, that means:

- `ukho-search-realm.json`

Do not use:

- `UKHOSearch-realm.json`
- `ADDSSearch-realm.json`
- `ukho-search-realm-original.json`

Those may cause Keycloak import problems if they end up in the import directory and do not match the internal realm name.

## Known issue: stale import files in the Docker volume

During troubleshooting it is possible to end up with stale files in the Keycloak Docker volume under:

- `/opt/keycloak/data/import`

That can happen even when the repo contains only the correct file.

To inspect the import directory in the Keycloak data volume:

```powershell
docker inspect keycloak-48f329f9 --format "{{json .Mounts}}"
```

If the container is stopped, you can inspect the volume contents with a temporary Alpine container:

```powershell
docker run --rm -v <keycloak-volume-name>:/data alpine sh -c "ls -la /data/import"
```

Only the correctly named realm file should remain there for a clean startup.

## Practical local verification walkthrough

Once Aspire, Keycloak, and the relevant hosts are running together, you can verify the shared browser-host authentication story in a predictable sequence.

Start by opening `WorkbenchHost` in a private browser window or after clearing the local auth cookie. You should be redirected to Keycloak before the shell becomes usable. After successful sign-in, the browser returns to the host and the shell loads normally. Logging out should clear the local cookie session and the upstream OpenID Connect session so that a fresh visit requires sign-in again.

Repeat the same flow for `IngestionServiceHost`. The important point is not merely that a login screen appears, but that the ingestion UI no longer renders anonymously. A direct request for the host should trigger Keycloak first, the authenticated UI should load only after sign-in, and the logout lifecycle route should force a fresh authentication round-trip on the next visit.

This shared verification path is useful because it proves that both hosts are consuming the same composition root rather than carrying two almost-identical but independently drifting authentication setups. If one host behaves differently from the other during this walkthrough, treat that as a regression in the shared browser-host model rather than as a harmless host-specific quirk.

## Forcing a clean re-import after changing the realm file

If the realm JSON is changed and you want Keycloak to recreate the realm from that file:

1. stop the Keycloak container or stop Aspire
2. delete the Keycloak data volume
3. restart Aspire

To identify the Keycloak volume from the current container:

```powershell
docker inspect keycloak-48f329f9 --format "{{json .Mounts}}"
```

Once you have the volume name, delete it with:

```powershell
docker volume rm <keycloak-volume-name>
```

This deletes the persisted Keycloak database and import cache state, so only do this when you intentionally want a fresh bootstrap.

## Operational checklist when realm changes are made

When users, roles, groups, or mappers are updated in Keycloak:

1. make the changes in the Keycloak UI
2. verify the mapper for `search-workbench` still outputs `realm_access.roles`
3. verify `Add to ID token` is still on
4. export the realm again using the full CLI export procedure above
5. copy the file back to `src/Hosts/AppHost/Realms/ukho-search-realm.json`
6. if you want to test the import path itself, delete the Keycloak volume and restart Aspire
7. log into Workbench and confirm the expected role claims appear

## Security note

A full realm export can contain sensitive development data, including:

- users
- password hashes
- client settings
- secrets depending on client configuration

Treat the export file accordingly and review it carefully before committing changes.
