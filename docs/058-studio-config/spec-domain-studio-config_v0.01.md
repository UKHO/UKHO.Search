# Work Package: `058-studio-config` — StudioHost endpoint propagation to Theia

**Target output path:** `docs/058-studio-config/spec-domain-studio-config_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for passing the runtime endpoint address of `StudioHost` into the Eclipse Theia studio environment.

The intent is to make the `StudioHost` base address available to Theia extensions so that browser-hosted studio features can call back into minimal APIs exposed by `src/Studio/StudioHost` without hard-coded URLs.

As a temporary proof of the mechanism, this work package also defines a simple test minimal API on `StudioHost` and a matching welcome-page behavior in Theia that displays the returned value.

This package builds on the shell baseline established in `docs/057-studio-shell/spec-domain-studio-shell_v0.01.md` and turns the previously reserved future API hook into an active runtime configuration contract.

### 1.2 Scope

This specification covers:

- resolving the `StudioHost` HTTP endpoint from Aspire `AppHost`
- propagating that endpoint into the JavaScript/Theia runtime started from `src/Studio/Server`
- defining the contract by which Theia extensions read the configured `StudioHost` base URL
- exposing a temporary test minimal API at `GET /echo` from `StudioHost`
- adapting the Theia welcome page to call that temporary endpoint and display the returned value
- ensuring the propagated value is suitable for browser-based calls back to minimal APIs hosted by `StudioHost`
- documenting expected normalization, failure behavior, and validation for the configuration flow

This specification does not cover:

- designing or implementing specific `StudioHost` business APIs beyond the endpoint propagation contract
- authentication or authorization between Theia and `StudioHost`
- non-Theia consumers of the `StudioHost` endpoint
- production deployment topology beyond the local Aspire-driven development environment
- migration of existing tools into Theia beyond enabling API connectivity

### 1.3 Stakeholders

- studio/tooling developers building Theia extensions
- maintainers of `src/Hosts/AppHost`
- maintainers of `src/Studio/StudioHost`
- engineering leads defining the future studio architecture
- developers implementing future Theia features that depend on `StudioHost` minimal APIs

### 1.4 Definitions

- `StudioHost`: the `.NET` web host at `src/Studio/StudioHost` intended to expose minimal APIs for studio scenarios
- Theia environment: the runtime context of the studio shell started from `src/Studio/Server`
- endpoint propagation: the act of resolving the effective `StudioHost` address in `AppHost` and passing it into the Theia runtime
- base URL: the absolute HTTP or HTTPS root address used by Theia extensions to call `StudioHost`, for example `http://localhost:5105`
- browser-safe configuration: a configuration shape that can be consumed by browser-hosted Theia code without assuming direct access to server-only process state

## 2. System context

### 2.1 Current state

The repository currently contains:

- an Aspire `AppHost` at `src/Hosts/AppHost/AppHost.cs`
- a `StudioHost` project resource already registered in `RunMode.Services` with `.WithExternalHttpEndpoints()`
- a Theia shell rooted at `src/Studio/Server`
- a placeholder Theia configuration contract in `src/Studio/Server/search-studio/src/browser/search-studio-future-api-configuration.ts`

`StudioHost` does not yet expose a dedicated temporary proof endpoint for this configuration flow, and the Theia welcome page does not yet display a value returned from `StudioHost`.

The current placeholder contract is:

- interface: `SearchStudioFutureApiConfiguration`
- property: `studioHostBaseUrl?`
- configuration key: `StudioHost.ApiBaseUrl`

At present, no active runtime flow populates that value from Aspire into the Theia environment. As a result, Theia extensions do not yet have a supported way to discover the correct `StudioHost` endpoint for API calls.

### 2.2 Proposed state

`AppHost` will treat the `StudioHost` endpoint as the source of truth and propagate its resolved external HTTP address into the Theia application at startup.

The Theia runtime will expose that value through a single, documented configuration contract so that native Theia extensions can read the `StudioHost` base URL without hard-coding ports, host names, or `launchSettings.json` values.

The propagated value will represent a browser-usable absolute base URL, allowing extensions running in the user-facing Theia shell to call `StudioHost` minimal APIs reliably during local Aspire orchestration.

To prove the mechanism end to end, `StudioHost` will expose a temporary `GET /echo` minimal API returning a suitable string, and the Theia welcome page will display the returned value using the propagated configuration.

### 2.3 Assumptions

- the studio shell will continue to be orchestrated from Aspire through `src/Hosts/AppHost`
- `StudioHost` will remain the backend location for minimal APIs intended for future Theia features
- the browser-hosted Theia experience requires a browser-reachable endpoint, not an internal-only container/service-discovery address
- the runtime contract should reuse the existing logical configuration key `StudioHost.ApiBaseUrl`
- this work should not require hard-coded endpoint values inside Theia extension source
- the endpoint contract should be stable enough to support additional studio settings later
- the initial consumer may be minimal, but the endpoint propagation path itself must be real and usable
- the authoritative endpoint should come from Aspire resource orchestration rather than from Visual Studio local profile settings

### 2.4 Constraints

- the configuration must support the existing browser-hosted Theia shell under `src/Studio/Server`
- the configuration must be consumable by native Theia extensions running in the browser-facing shell
- the solution must not require developers to manually edit Theia source files with machine-specific URLs
- the propagated value must identify `StudioHost`, not some alternate API host
- the work package must remain limited to endpoint propagation and related configuration behavior

## 3. Component / service design (high level)

### 3.1 Components

This work package introduces or updates the following logical components:

1. Aspire endpoint resolution in `AppHost`
   - purpose: resolve the effective `StudioHost` HTTP endpoint during local orchestration
   - responsibility: provide the source value that will be passed into the studio shell startup environment

2. JavaScript/Theia startup configuration bridge
   - purpose: carry the resolved `StudioHost` endpoint from `AppHost` into the `src/Studio/Server` runtime
   - responsibility: expose the value in a form the Theia application can consume during startup

3. Theia configuration contract
   - purpose: define the single supported logical key for extensions to read the `StudioHost` base URL
   - responsibility: map the startup-provided value into a documented extension-facing configuration object

4. Temporary `StudioHost` proof endpoint
   - purpose: provide a minimal validation API for the Theia shell integration
   - responsibility: expose `GET /echo` and return a simple suitable string proving that the shell can reach `StudioHost`

5. Theia welcome page consumer
   - purpose: use the propagated endpoint to call minimal APIs hosted by `StudioHost`
   - responsibility: call the temporary `GET /echo` endpoint and display the returned value as a temporary proof of connectivity

### 3.2 Data flows

#### Startup configuration flow

1. `AppHost` starts in `RunMode.Services`
2. `AppHost` registers both `StudioHost` and the Theia shell
3. Aspire resolves the effective external HTTP endpoint for `StudioHost`
4. `AppHost` passes that resolved endpoint into the JavaScript application startup environment for `src/Studio/Server`
5. the Theia application maps the incoming startup value into the documented studio configuration contract
6. Theia extensions read the resulting `StudioHost` base URL and use it for API calls

#### Extension API call flow

1. a Theia extension requests the configured `StudioHost` base URL
2. the welcome-page contribution constructs a request to the temporary `GET /echo` minimal API route hosted by `StudioHost`
3. the browser issues the HTTP request to the propagated `StudioHost` address
4. `StudioHost` handles the request and returns a suitable string response
5. the Theia welcome page displays the returned value as a temporary proof that the endpoint propagation mechanism works

### 3.3 Key decisions

- **Source of truth:** `AppHost`-resolved `StudioHost` endpoint
  - rationale: Aspire orchestration already owns runtime endpoint composition and should remain authoritative

- **Consumer contract:** retain `StudioHost.ApiBaseUrl`
  - rationale: the repository already contains a placeholder future API configuration key, so this work should activate that contract rather than invent a second one

- **Address type:** browser-usable absolute base URL
  - rationale: Theia extensions ultimately execute in a browser-hosted experience and therefore need a user-reachable address, not an internal-only service identifier

- **Configuration style:** runtime propagation rather than compile-time constant
  - rationale: the endpoint can vary by machine, port, and orchestration context and must not be baked into extension code

- **Failure posture:** fail soft for missing configuration
  - rationale: Theia should remain able to start even if the `StudioHost` endpoint is unavailable, while features that require the API should degrade clearly

- **Normalization:** provide one canonical base URL form
  - rationale: consistent normalization avoids duplicate slash handling and ad hoc endpoint concatenation in each extension

- **Scope boundary:** endpoint propagation only
  - rationale: this package is intended to enable later API-backed features, not to design those features now

- **Proof mechanism:** temporary `GET /echo` endpoint surfaced on the welcome page
  - rationale: a small visible round-trip is the simplest way to prove the propagated `StudioHost` endpoint can be used from the browser-hosted Theia shell

## 4. Functional requirements

### FR-001 Resolve `StudioHost` endpoint from Aspire

`AppHost` shall use the `StudioHost` Aspire resource registration as the authoritative source for the API endpoint value passed to the Theia shell.

The propagated value shall be derived from the effective runtime endpoint rather than copied from `launchSettings.json` or other developer-local static files.

### FR-002 Use browser-reachable endpoint

The propagated `StudioHost` address shall be a browser-reachable absolute base URL suitable for front-end HTTP requests from the Theia shell.

The selected address shall be the external HTTP endpoint intended for local developer access.

### FR-003 Pass endpoint into Theia startup environment

`AppHost` shall pass the resolved `StudioHost` base URL into the JavaScript application started from `src/Studio/Server` as part of the shell startup configuration.

This propagation shall occur automatically during normal Aspire startup.

### FR-004 Provide a single documented environment contract

The endpoint propagation path shall define one documented startup contract for the `StudioHost` base URL.

The implementation may use an environment variable or equivalent startup mechanism, but the contract shall be explicit, stable, and documented in the studio shell.

### FR-005 Map startup contract into Theia configuration

The Theia startup path shall map the incoming startup value into the existing logical configuration contract represented by `StudioHost.ApiBaseUrl`.

Extensions shall not be required to understand the raw Aspire-specific startup mechanism directly.

### FR-006 Reuse the existing typed configuration shape

The browser-facing configuration exposed to studio extensions shall align with `SearchStudioFutureApiConfiguration` and its `studioHostBaseUrl` property unless implementation evidence later requires a small compatible refinement.

Any refinement shall preserve the single logical meaning of the `StudioHost` base address.

### FR-007 Normalize the propagated base URL

The `StudioHost` base URL shall be normalized before being exposed to extensions.

Normalization shall ensure the value is suitable for appending relative API paths consistently.

The normalized form should omit a trailing slash unless a later integration requirement proves otherwise.

### FR-008 Prevent hard-coded extension endpoints

Theia extensions that call `StudioHost` minimal APIs shall read the configured base URL through the documented studio configuration contract.

They shall not hard-code localhost addresses, fixed ports, or values copied from Visual Studio launch profiles.

### FR-009 Degrade gracefully when configuration is unavailable

If the `StudioHost` endpoint cannot be propagated or resolved, the Theia shell shall still be allowed to start.

Features that depend on `StudioHost` shall detect the missing configuration and fail in a controlled way, such as disabling the feature path or surfacing a clear user/developer message.

### FR-010 Preserve future extensibility

The configuration bridge created by this work package shall be structured so that additional studio runtime settings can be propagated later without redesigning the entire mechanism.

### FR-011 Support minimal API callbacks from extensions

The propagated configuration shall be sufficient for native Theia extensions to issue HTTP requests back to minimal APIs hosted by `StudioHost`.

No additional manual URL assembly knowledge beyond the normalized base URL and the API route path shall be required.

### FR-012 Keep `StudioHost` and Theia loosely coupled

`StudioHost` shall not need to know implementation details of individual Theia extensions in order for endpoint propagation to work.

The contract shall remain a generic runtime configuration handoff from `AppHost` to the studio shell.

### FR-013 Expose temporary test endpoint from `StudioHost`

`StudioHost` shall expose a temporary test minimal API at `GET /echo`.

That endpoint shall return a suitable string value intended only to prove that the Theia shell can successfully call back into `StudioHost`.

### FR-014 Display temporary echo value on the Theia welcome page

The Theia welcome page shall be adapted to call the temporary `GET /echo` endpoint using the propagated `StudioHost` base URL.

The returned string shall be displayed in the welcome page UI as a temporary proof of the endpoint propagation mechanism.

### FR-015 Keep the echo proof temporary and lightweight

The `GET /echo` endpoint and its welcome-page display shall be treated as temporary proof behavior only.

They shall not be considered final studio functionality or a long-term business feature design.

## 5. Non-functional requirements

### NFR-001 Environment portability

The endpoint propagation mechanism shall work across local developer environments without requiring per-machine source edits.

### NFR-002 Deterministic startup behavior

The configuration handoff from `AppHost` to the Theia shell shall occur consistently on each Aspire startup.

### NFR-003 Browser safety

The configuration exposed to extensions shall be safe for browser-hosted Theia code to consume and shall not depend on server-only process access from front-end components.

### NFR-004 Observability

Configuration resolution or propagation failures shall be diagnosable through normal Aspire or application startup logs.

### NFR-005 Minimal operational complexity

The solution shall avoid unnecessary additional configuration files or manual developer setup where the value can be derived directly from Aspire orchestration.

### NFR-006 Lightweight proof implementation

The temporary `GET /echo` endpoint and welcome-page display shall remain intentionally lightweight so they can later be replaced by real studio features with minimal churn.

## 6. Data model

This work package introduces a configuration value rather than a domain entity.

### 6.1 Configuration data

- logical key: `StudioHost.ApiBaseUrl`
- consumer property: `studioHostBaseUrl`
- value type: string
- value shape: absolute HTTP or HTTPS base URL
- expected semantics: root address for browser calls from Theia extensions to `StudioHost`

## 7. Interfaces & integration

### 7.1 Aspire to JavaScript contract

`AppHost` shall provide the `StudioHost` base URL to the JavaScript application startup path for `src/Studio/Server`.

The exact transport mechanism may be an environment variable or equivalent JavaScript app startup setting, provided it is explicit and documented.

### 7.2 Theia internal configuration contract

The Theia shell shall expose the propagated value through the existing studio configuration concept keyed by `StudioHost.ApiBaseUrl`.

The extension-facing object should remain aligned to:

- `SearchStudioFutureApiConfiguration`
- `studioHostBaseUrl`

### 7.3 Extension consumption contract

Extensions should consume the `StudioHost` base URL through a shared configuration path rather than each feature introducing its own environment parsing logic.

The contract should support future expansion for additional studio host settings while keeping the `StudioHost` base URL as the first mandatory field.

For this work package, the primary consumer is the welcome-page contribution, which should use the shared configuration path to call `GET /echo` and display the returned value.

### 7.4 `StudioHost` endpoint ownership

`StudioHost` remains responsible only for exposing its HTTP endpoints.

`AppHost` remains responsible for resolving and supplying the endpoint address to dependent applications.

Theia remains responsible for consuming the resulting runtime configuration.

## 8. Observability (logging/metrics/tracing)

This work package does not require custom metrics or tracing.

Minimum observability expectations are:

- `AppHost` startup should make endpoint propagation failures diagnosable during local runs
- Theia startup should make missing or invalid `StudioHost` configuration diagnosable for developers
- extension-level feature failures caused by missing configuration should surface clear messages rather than silent breakage
- failure to call or display the temporary `GET /echo` response should be diagnosable during local development

## 9. Security & compliance

This work package propagates a service endpoint address only.

Security expectations are:

- do not treat the propagated endpoint as a secret
- do not embed credentials or tokens into the propagated base URL
- keep authentication and authorization out of scope for this package unless a later work package explicitly adds them
- ensure browser-visible configuration contains only what is needed for endpoint discovery

## 10. Testing strategy

The work package shall be validated through configuration and connectivity verification.

Minimum validation shall include:

1. Aspire startup resolves a `StudioHost` endpoint while running `RunMode.Services`
2. the Theia shell receives the propagated value during startup
3. the Theia configuration contract exposes the expected `StudioHost` base URL
4. `StudioHost` exposes a temporary `GET /echo` endpoint that returns a suitable string
5. the Theia welcome page can call `GET /echo` using the propagated base URL and display the returned value
6. missing configuration or failed echo-call behavior is observable and non-fatal to shell startup

## 11. Rollout / migration

Rollout expectations:

1. activate the currently placeholder `StudioHost` future API configuration contract in the Theia shell
2. wire `AppHost` to pass the resolved `StudioHost` endpoint into the JavaScript app startup path
3. add a temporary `GET /echo` proof endpoint to `StudioHost`
4. adapt the Theia welcome page to display the returned echo value
5. validate end-to-end availability from Theia runtime configuration through to the temporary minimal API call path
6. use later work packages to replace the temporary proof behavior with actual studio features that consume the endpoint

This work package is an enabling step for future Theia-to-`StudioHost` integration.

## 12. Open questions

No further open questions are currently recorded.
