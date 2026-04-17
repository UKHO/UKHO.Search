# Implementation Plan

**Target output path:** `docs/092-ingestion-auth/plan-host-ingestion-auth_v0.01.md`

**Version:** `v0.01` (`Draft`)

**Based on:** `docs/092-ingestion-auth/spec-host-ingestion-auth_v0.01.md`

## Plan context

This plan implements the authentication-parity specification for `IngestionServiceHost` while preserving the repository's Onion Architecture boundaries. The work is intentionally organized into vertical slices so each completed Work Item leaves the repository in a runnable, demonstrable state.

This plan treats `./.github/instructions/wiki.instructions.md` as a mandatory completion gate for the full work package and requires the final execution record to state which wiki or repository guidance pages were updated, created, retired, or why no wiki page update was needed.

This plan also treats `./.github/instructions/documentation-pass.instructions.md` as a non-negotiable Definition of Done requirement for every code-writing task. Any implementation work must follow that instruction file in full, including developer-level comments on every class, method, and constructor in scope, including internal and other non-public types; parameter documentation for every public method and constructor parameter; property documentation where meaning is not obvious; and sufficient inline or block comments so the purpose, logical flow, and rationale remain clear to future developers.

## Baseline

- `WorkbenchHost` already uses cookie-backed OpenID Connect authentication against Keycloak and protects its Blazor UI with a fallback authenticated-user policy.
- `IngestionServiceHost` currently hosts an interactive Blazor UI without the same host-level authentication wiring.
- `WorkbenchHost` currently owns login/logout endpoint mapping and Keycloak realm-role claims transformation locally.
- Repository wiki guidance currently documents Workbench-focused Keycloak integration and will need review because this work broadens the host-auth story.

## Delta

- Extract or centralize the shared host-auth wiring needed to keep `WorkbenchHost` and `IngestionServiceHost` aligned.
- Apply the shared Keycloak, cookie, claims-transformation, and login/logout flow to `IngestionServiceHost`.
- Enforce authenticated access for every browser-accessible `IngestionServiceHost` endpoint except the login and logout lifecycle routes.
- Add targeted tests that prove unauthenticated access challenges correctly, authenticated flows remain usable, and the anonymous exception set stays minimal.
- Review and update wiki or repository guidance so contributors can understand the now-shared browser-host authentication model.

## Carry-over

- Fine-grained role authorization inside `IngestionServiceHost` remains out of scope unless needed only to preserve existing shared claims behaviour.
- Broader enterprise identity redesign, Keycloak client redesign, or new role models remain out of scope.
- Non-browser ingestion runtime behaviour remains unchanged except where host auth wiring must coexist safely with the existing pipeline host.

## Authentication parity delivery strategy

The smallest safe path is to first establish a reusable host-auth slice that still leaves `WorkbenchHost` runnable and authenticated, then wire `IngestionServiceHost` onto that slice, and finally complete the mandatory wiki review/update gate. This keeps parity sustainable instead of duplicating fragile host-local bootstrap code.

## Shared files and likely touch points

- `src/workbench/server/WorkbenchHost/Program.cs`
- `src/workbench/server/WorkbenchHost/Extensions/LoginLogoutEndpointRouteBuilderExtensions.cs`
- `src/workbench/server/WorkbenchHost/Extensions/KeycloakRealmRoleClaimsTransformation.cs`
- `src/Hosts/IngestionServiceHost/Program.cs`
- `src/Hosts/IngestionServiceHost/Components/Routes.razor`
- `src/Hosts/UKHO.Search.ServiceDefaults/...` for any shared host-auth registration or endpoint extensions introduced by the implementation
- `test/workbench/server/WorkbenchHost.Tests/...`
- `test/IngestionServiceHost.Tests/...`
- `test/UKHO.Search.ServiceDefaults.Tests/...` if shared auth helpers are introduced there
- `wiki/keycloak-workbench-integration.md` or a replacement/renamed broader host-auth guidance page if the review concludes the current title and framing are now too narrow

## Work Items

## Shared host authentication foundation
- [x] Work Item 1: Establish reusable Keycloak host-auth wiring while keeping `WorkbenchHost` fully runnable - Completed
  - **Purpose**: Create a single obvious host-auth registration path that preserves current `WorkbenchHost` behaviour and becomes the foundation for `IngestionServiceHost` parity.
  - **Acceptance Criteria**:
    - `WorkbenchHost` continues to authenticate through Keycloak using the existing client configuration after the refactor.
    - Shared host-auth wiring exists in a host-appropriate location and can be consumed by both hosts without violating Onion Architecture.
    - Login/logout endpoint behaviour and Keycloak realm-role claim normalization remain functionally equivalent for `WorkbenchHost`.
    - All code added or updated for this slice complies with `./.github/instructions/documentation-pass.instructions.md` in full.
  - **Definition of Done**:
    - Code implemented for the shared host-auth registration path and `WorkbenchHost` consumption
    - Tests passing for the targeted shared-auth and `WorkbenchHost` coverage added in this slice
    - Logging & error handling preserved or improved for host-auth startup and flow diagnosis
    - Documentation comments and developer comments added per `./.github/instructions/documentation-pass.instructions.md`
    - Wiki review completed for this slice; relevant wiki or repository guidance updated, or an explicit no-change review result recorded
    - Foundational documentation retains book-like narrative depth, defines technical terms, and includes examples or walkthrough support where the subject matter is conceptually dense
    - Can execute end-to-end via: run the existing `WorkbenchHost` path and confirm Keycloak challenge, login, and logout still behave correctly
  - [x] Task 1: Design the shared host-auth composition point - Completed
    - [x] Step 1: Compare current `WorkbenchHost` auth responsibilities in `Program.cs`, login/logout endpoint mapping, and claims transformation to identify the exact reusable units.
    - [x] Step 2: Choose a host-safe shared location, preferably under `src/Hosts/UKHO.Search.ServiceDefaults`, so both hosts can consume the same registration path without crossing Onion Architecture boundaries.
    - [x] Step 3: Keep one obvious public entry point for shared registration, such as a service-collection extension and a route-builder extension, to reduce future drift.
    - [x] Step 4: Document the chosen composition root in code with developer-level comments, following `./.github/instructions/documentation-pass.instructions.md` in full.
  - [x] Task 2: Refactor `WorkbenchHost` to consume the shared auth foundation - Completed
    - [x] Step 1: Move or recreate the cookie-backed OpenID Connect configuration, fallback authorization policy, cascading authentication state registration, claims transformation registration, and login/logout endpoint mapping in the shared location.
    - [x] Step 2: Preserve the existing Keycloak realm and client configuration used by `WorkbenchHost` so this slice does not alter behaviour.
    - [x] Step 3: Keep middleware and endpoint ordering correct so authentication runs before authorization and the explicit anonymous lifecycle routes remain available.
    - [x] Step 4: Update all changed files with full developer-level comments, including classes, methods, constructors, non-obvious properties, and public parameters, exactly as required by `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 3: Add targeted regression coverage for the shared foundation - Completed
    - [x] Step 1: Add or update tests proving the shared registration configures the expected schemes, fallback policy, and anonymous login/logout route behaviour.
    - [x] Step 2: Add or update tests proving Keycloak realm-role normalization still produces the expected role claims shape.
    - [x] Step 3: Add or update tests proving `WorkbenchHost` remains on the shared path instead of retaining hidden host-local divergence.
    - [x] Step 4: Document test intent clearly with developer-level comments per `./.github/instructions/documentation-pass.instructions.md`.
  - **Execution summary**: Added shared browser-host authentication extensions and the Keycloak realm-role claims transformation under `src/Hosts/UKHO.Search.ServiceDefaults`, refactored `WorkbenchHost` to consume that shared path, removed the superseded host-local auth helpers and direct Keycloak package references, and added focused shared-auth plus Workbench composition tests with developer-level comments.
  - **Validation summary**: `run_build` succeeded; `run_tests` for assemblies `UKHO.Search.ServiceDefaults.Tests` and `WorkbenchHost.Tests` succeeded with 74 passing tests.
  - **Wiki review result**: Updated `wiki/keycloak-workbench-integration.md` to describe the shared browser-host authentication composition root, lifecycle endpoints, and shared claims-transformation behavior. No wiki page was retired or renamed in this slice.
  - **Files**:
    - `src/Hosts/UKHO.Search.ServiceDefaults/...`: shared host-auth extensions, claims transformation, or endpoint helpers if introduced
    - `src/workbench/server/WorkbenchHost/Program.cs`: switch to the shared host-auth registration path
    - `src/workbench/server/WorkbenchHost/Extensions/LoginLogoutEndpointRouteBuilderExtensions.cs`: remove, retain, or redirect depending on the chosen shared location
    - `src/workbench/server/WorkbenchHost/Extensions/KeycloakRealmRoleClaimsTransformation.cs`: remove, retain, or redirect depending on the chosen shared location
    - `test/UKHO.Search.ServiceDefaults.Tests/...`: targeted shared-auth registration tests if shared code lives here
    - `test/workbench/server/WorkbenchHost.Tests/...`: regression coverage for preserved Workbench behaviour
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.ServiceDefaults.Tests/UKHO.Search.ServiceDefaults.Tests.csproj` if shared auth tests are added there
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
    - Run the repository through the usual local host path and confirm `WorkbenchHost` still redirects unauthenticated users to Keycloak, returns authenticated users to the shell, and supports logout
  - **User Instructions**:
    - Ensure the local Keycloak/Aspire environment is available before manual browser verification
    - Use an existing valid test user in the shared `ukho-search` realm

## Ingestion host secured vertical slice
- [x] Work Item 2: Secure `IngestionServiceHost` end to end with the shared Keycloak host-auth flow - Completed
  - **Purpose**: Deliver the user-visible capability that every browser attempt to access the ingestion Blazor UI is authenticated first, while leaving the host runnable and demonstrable.
  - **Acceptance Criteria**:
    - Unauthenticated navigation to the `IngestionServiceHost` UI results in a Keycloak challenge rather than anonymous rendering.
    - `IngestionServiceHost` uses the same Keycloak client configuration already used by `WorkbenchHost`.
    - Every browser-accessible endpoint in `IngestionServiceHost` requires authentication except the login and logout lifecycle routes.
    - The host remains interactive after sign-in and requires re-authentication after logout.
    - All code added or updated for this slice complies with `./.github/instructions/documentation-pass.instructions.md` in full.
  - **Definition of Done**:
    - Code implemented in `IngestionServiceHost` and any shared host-auth location needed for end-to-end authentication
    - Tests passing for targeted `IngestionServiceHost` and any related shared-auth coverage
    - Logging & error handling added or preserved for auth startup and challenge/logout flow diagnosis
    - Documentation comments and developer comments added per `./.github/instructions/documentation-pass.instructions.md`
    - Wiki review completed for this slice; relevant wiki or repository guidance updated, or an explicit no-change review result recorded
    - Foundational documentation retains book-like narrative depth, defines technical terms, and includes examples or walkthrough support where the subject matter is conceptually dense
    - Can execute end-to-end via: start the local stack, browse to `IngestionServiceHost`, authenticate through Keycloak, use the UI, log out, and verify fresh challenge on next access
  - [x] Task 1: Apply the shared auth path to the ingestion host bootstrap - Completed
    - [x] Step 1: Update `src/Hosts/IngestionServiceHost/Program.cs` to register the shared cookie/OpenID Connect configuration, cascading authentication state, fallback authorization policy, claims transformation, and lifecycle endpoints.
    - [x] Step 2: Preserve the host's existing ingestion runtime registrations so browser authentication does not alter ingestion pipeline bootstrapping semantics.
    - [x] Step 3: Ensure middleware and endpoint ordering stays correct for static assets, antiforgery, authentication, authorization, and interactive Razor component mapping.
    - [x] Step 4: Add developer-level comments throughout the host bootstrap file, including non-obvious flow ordering, following `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 2: Lock down the Blazor surface and anonymous exception set - Completed
    - [x] Step 1: Confirm that the fallback authorization policy protects the normal UI routes without requiring per-page duplication unless a page must be marked explicitly.
    - [x] Step 2: Ensure only the login and logout lifecycle routes remain anonymously reachable from the browser.
    - [x] Step 3: Review `Components/Routes.razor` and any other route or layout files to confirm the authenticated user context flows correctly into the Blazor component tree.
    - [x] Step 4: If any host-specific anonymous route is discovered during implementation, stop and reconcile it against the specification rather than silently preserving divergence.
  - [x] Task 3: Add targeted host tests for the secured slice - Completed
    - [x] Step 1: Add or update tests proving unauthenticated UI access produces an authentication challenge.
    - [x] Step 2: Add or update tests proving the login endpoint is anonymous and starts the sign-in flow.
    - [x] Step 3: Add or update tests proving logout clears the local session and requires a fresh challenge on later access.
    - [x] Step 4: Add or update tests proving there are no extra anonymous browser-accessible endpoints beyond the lifecycle routes.
    - [x] Step 5: Document each test scenario clearly with developer-level comments per `./.github/instructions/documentation-pass.instructions.md`.
  - [x] Task 4: Perform end-to-end manual verification for the runnable slice - Completed
    - [x] Step 1: Start the local stack through the repository's preferred host path so Keycloak and `IngestionServiceHost` are available together.
    - [x] Step 2: Browse directly to the ingestion host URL while signed out and verify redirect to Keycloak.
    - [x] Step 3: Sign in with the existing shared client path and verify the ingestion UI loads and remains interactive.
    - [x] Step 4: Use logout, then confirm a fresh visit to the UI triggers re-authentication.
    - [x] Step 5: Capture any notable operational steps or failure modes for later wiki updates if the review concludes they are useful to contributors.
  - **Execution summary**: Updated `src/Hosts/IngestionServiceHost/Program.cs` to consume the shared Keycloak browser-host authentication path, map the shared login/logout lifecycle endpoints, and place authentication and authorization before the interactive Blazor surface. Updated `Components/Routes.razor` to use `AuthorizeRouteView` and added `Components/Authentication/RedirectToLogin.razor(.cs)` so unauthorized interactive route transitions force a full navigation into the shared login endpoint. Added focused ingestion-host composition tests that lock in the shared auth path, routing behavior, and absence of extra anonymous routes. Follow-up remediation added the missing `AppHost` `.WithReference(keycloak)` and `.WaitFor(keycloak)` wiring for `IngestionServiceHost` so the Aspire service graph actually supplies the Keycloak connection required by the shared auth path at runtime. A later runtime fix updated `src/Hosts/AppHost/Realms/ukho-search-realm.json` so the shared `search-workbench` Keycloak client explicitly allows the local `IngestionServiceHost` HTTPS redirect and logout callback URLs alongside the existing Workbench URLs, and added regression coverage that locks those client settings in place.
  - **Validation summary**: `run_build` succeeded; `run_tests` for assemblies `IngestionServiceHost.Tests`, `UKHO.Search.ServiceDefaults.Tests`, and later `AppHost.Tests` succeeded with targeted passing coverage, including regressions that lock both the `AppHost` Keycloak reference for `IngestionServiceHost` and the shared Keycloak client redirect/logout URL configuration.
  - **Wiki review result**: Updated `wiki/keycloak-workbench-integration.md` to broaden the guidance to both browser hosts, document `IngestionServiceHost`'s use of the shared auth composition root and authorization-aware Blazor routing, add a practical shared verification walkthrough, explain that the shared `search-workbench` client must include both hosts' local redirect/logout URLs and may require a fresh Keycloak volume import when those URLs change, and add a troubleshooting section that distinguishes missing Aspire Keycloak wiring from redirect-URI mismatch failures. No wiki page was retired or renamed in this slice.
  - **Files**:
    - `src/Hosts/IngestionServiceHost/Program.cs`: add the shared auth registration path, challenge/authorization pipeline, and lifecycle endpoint mapping
    - `src/Hosts/IngestionServiceHost/Components/Routes.razor`: confirm or adjust authenticated route flow if required
    - `test/IngestionServiceHost.Tests/...`: host auth and anonymous-endpoint coverage
    - `src/Hosts/UKHO.Search.ServiceDefaults/...`: any shared changes needed to support ingestion-host consumption
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/IngestionServiceHost.Tests/IngestionServiceHost.Tests.csproj`
    - `dotnet test test/UKHO.Search.ServiceDefaults.Tests/UKHO.Search.ServiceDefaults.Tests.csproj` if shared tests exist
    - Start the local host stack and navigate to the `IngestionServiceHost` URL from the dashboard or configured endpoint
    - Verify sign-in, post-login UI access, and logout behaviour manually in the browser
  - **User Instructions**:
    - Start the local Aspire environment so Keycloak and dependent services are available together
    - Use a valid user in the existing shared Keycloak realm and client setup already used by `WorkbenchHost`
    - If local Keycloak data is stale, follow the existing realm reset guidance before retrying verification

## Repository guidance and completion gate
- [x] Work Item 3: Complete the mandatory wiki review/update and record the final work-package documentation outcome - Completed
  - **Purpose**: Ensure contributor-facing guidance is accurate now that browser-host authentication is shared across both hosts and that the work package closes with an explicit documentation record.
  - **Acceptance Criteria**:
    - The relevant wiki and repository guidance pages have been reviewed against the implemented auth behaviour.
    - Any needed wiki updates are made in current-state, book-like narrative prose with technical terms explained and examples or walkthrough fragments where they materially improve comprehension.
    - The final execution record explicitly states which wiki or repository guidance pages were updated, created, retired, or why no wiki update was required.
  - **Definition of Done**:
    - Wiki review completed under `./.github/instructions/wiki.instructions.md`
    - Relevant wiki or repository guidance updated, or an explicit no-change review result recorded with the pages reviewed and the reason no change was needed
    - Documentation updated
    - Foundational documentation retains book-like narrative depth, defines technical terms, and includes examples or walkthrough support where the subject matter is conceptually dense
    - Can execute end-to-end via: review the final implementation summary and documentation record alongside the working authenticated host flows
  - [x] Task 1: Review existing guidance for stale Workbench-only framing - Completed
    - [x] Step 1: Review `wiki/keycloak-workbench-integration.md` and any adjacent setup or glossary pages that describe browser-host authentication.
    - [x] Step 2: Decide whether the current page should be updated in place, broadened in scope, or replaced by a more general host-auth page because the behaviour now applies beyond Workbench alone.
    - [x] Step 3: Ensure terminology such as Keycloak realm, OpenID Connect challenge, cookie session, fallback authorization policy, and lifecycle endpoints is explained when first introduced or linked to existing glossary material.
  - [x] Task 2: Update contributor guidance if required - Completed
    - [x] Step 1: If a wiki update is required, describe the current shared host-auth model in narrative prose rather than terse bullets.
    - [x] Step 2: Include a practical walkthrough for contributors verifying sign-in behaviour locally, including where `WorkbenchHost` and `IngestionServiceHost` now fit into the shared model.
    - [x] Step 3: Record any new or changed troubleshooting steps only if they are grounded in the implemented behaviour.
    - [x] Step 4: Keep the documentation current-state and remove wording that implies the behaviour is Workbench-only if that is no longer true.
  - [x] Task 3: Record the final documentation outcome - Completed
    - [x] Step 1: Add a final execution note stating which wiki or repository guidance pages were updated, created, retired, or explicitly left unchanged.
    - [x] Step 2: If no wiki update was needed, record exactly which pages were reviewed and why the existing guidance already remained sufficient.
    - [x] Step 3: Confirm the work package summary references both the plan and the source spec so the documentation trail stays complete.
  - **Execution summary**: Completed the final documentation pass for the work package by reviewing the shared browser-host guidance against the implemented `WorkbenchHost` and `IngestionServiceHost` behaviour, confirming that the previously broadened Keycloak browser-host guidance already captures the shared authentication composition root, lifecycle endpoints, redirect/logout URI requirements, practical verification walkthrough, and troubleshooting steps introduced during Work Items 1 and 2. The final work-package documentation record now explicitly closes the implementation trail back to both `docs/092-ingestion-auth/plan-host-ingestion-auth_v0.01.md` and `docs/092-ingestion-auth/spec-host-ingestion-auth_v0.01.md`.
  - **Validation summary**: `run_build` succeeded; `run_tests` for assemblies `AppHost.Tests`, `IngestionServiceHost.Tests`, `UKHO.Search.ServiceDefaults.Tests`, and `WorkbenchHost.Tests` succeeded with 82 passing tests.
  - **Wiki review result**: No further wiki page update was required during this final completion-gate review. Reviewed `wiki/keycloak-workbench-integration.md` and the repository's adjacent browser-host documentation surface; the existing page had already been broadened beyond Workbench-only framing and remained sufficient because it already documents the shared host-auth model, local redirect/logout URI requirements, validation walkthrough, and troubleshooting guidance. No wiki page was created, retired, split, or renamed in this final slice.
  - **Final work-package summary**: Work Item 1 established the shared browser-host Keycloak foundation and moved `WorkbenchHost` onto it. Work Item 2 applied the same shared authentication model to `IngestionServiceHost`, updated the AppHost Keycloak resource wiring and realm client redirect/logout URIs required for runtime parity, and refreshed the shared Keycloak guidance accordingly. Work Item 3 completed the mandatory documentation closeout and confirmed that the final contributor guidance remains aligned with the implemented behaviour described by both this plan and `docs/092-ingestion-auth/spec-host-ingestion-auth_v0.01.md`.
  - **Files**:
    - `wiki/keycloak-workbench-integration.md`: likely update candidate because the page title and scope may become too narrow
    - `wiki/...`: any linked glossary, setup, or architecture page that becomes stale as a result of the auth-parity change
    - `docs/092-ingestion-auth/plan-host-ingestion-auth_v0.01.md`: plan reference during execution record preparation
    - `docs/092-ingestion-auth/spec-host-ingestion-auth_v0.01.md`: canonical requirements reference for the final documentation outcome
  - **Work Item Dependencies**: Work Item 2
  - **Run / Verification Instructions**:
    - Review the final authenticated browser flows for both hosts
    - Compare the implemented behaviour against the updated or reviewed wiki guidance
    - Record the wiki review result explicitly in the final work-package execution summary
  - **User Instructions**:
    - None beyond normal contributor review of the updated guidance

## Overall approach summary

The implementation should first remove duplicated host-auth risk by establishing a shared composition point, then apply that shared path to `IngestionServiceHost` so the feature lands as a fully runnable authenticated slice, and finally complete the mandatory wiki review/update gate. The main design consideration is preserving exact authentication parity with `WorkbenchHost` without weakening the current Workbench experience or coupling browser sign-in to the ingestion runtime itself.

Key considerations for implementation:

- keep shared auth code in a host-appropriate layer so Onion Architecture direction remains intact
- preserve the existing Keycloak client configuration and user-facing login/logout behaviour from `WorkbenchHost`
- ensure the fallback authorization policy and middleware ordering protect every browser-accessible ingestion-host endpoint except the login/logout lifecycle routes
- keep all code-writing tasks compliant with `./.github/instructions/documentation-pass.instructions.md`
- treat wiki review and any required wiki updates as a hard completion gate, not optional polish
