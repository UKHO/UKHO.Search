# Implementation Plan: QueryServiceHost Keycloak authentication alignment

- **Work package**: `093-query-auth`
- **Document**: `docs/093-query-auth/plan-host-query-auth_v0.01.md`
- **Version**: `v0.01`
- **Date**: `2026-04-15`
- **Based on**: `docs/093-query-auth/spec-host-query-auth_v0.01.md`
- **Mandatory instruction files**:
  - `./.github/instructions/documentation-pass.instructions.md`
  - `./.github/instructions/wiki.instructions.md`

## Overall delivery approach

This work package should be delivered as a small number of vertical slices that each leave the repository in a runnable, reviewable state. The central implementation strategy is to make `QueryServiceHost` consume the same shared browser-host authentication composition already used by the other browser-facing hosts, then pin the exact local Keycloak callback contract in both the checked-in realm export and automated tests.

The work should avoid introducing any second authentication composition path. `QueryServiceHost` must reuse `UKHO.Search.ServiceDefaults` so that sign-in challenge behavior, cookie session restoration, route protection, and claims handling remain consistent across the repository's interactive browser hosts.

Because this work updates source code, **every code-writing task in this plan must follow `./.github/instructions/documentation-pass.instructions.md` in full as a mandatory Definition of Done gate**. That means the implementation is required to add developer-level documentation comments for all newly created or modified classes, constructors, and methods, including internal and other non-public types, and to add sufficient inline comments where needed so the flow remains understandable to future contributors.

Because this work changes developer-facing authentication behavior and the local setup contract, **the wiki review defined by `./.github/instructions/wiki.instructions.md` is also mandatory**. The final execution record must state which wiki or repository guidance pages were updated, created, retired, or why no update was required after review.

## Work package structure and expected outputs

This work package uses a single documentation folder:

- `docs/093-query-auth/`

Planned documents for this work package:

- `docs/093-query-auth/spec-host-query-auth_v0.01.md`
- `docs/093-query-auth/plan-host-query-auth_v0.01.md`

## Planned implementation areas

The code and repository areas expected to be updated during execution are:

- `src/Hosts/QueryServiceHost/Program.cs`
- `src/Hosts/QueryServiceHost/Components/Routes.razor`
- `src/Hosts/QueryServiceHost/Components/Authentication/*` if a redirect component is needed
- `src/Hosts/AppHost/Realms/ukho-search-realm.json`
- `.gitignore`
- `test/QueryServiceHost.Tests/*`
- `test/AppHost.Tests/*`
- relevant wiki or repository guidance pages if the mandatory wiki review identifies needed updates

## Feature Slice 1: QueryServiceHost authenticated browser entry path

- [x] **Work Item 1: Protect QueryServiceHost with the shared Keycloak browser-host flow** - Completed
  - **Purpose**: Deliver the smallest end-to-end authentication slice so `QueryServiceHost` stops behaving as an anonymous browser host and instead challenges through the shared Keycloak model already used elsewhere in the repository.
  - **Acceptance Criteria**:
    - `QueryServiceHost` registers `AddKeycloakBrowserHostAuthentication("search-workbench")`.
    - `QueryServiceHost` maps `MapKeycloakBrowserHostAuthenticationEndpoints()`.
    - `QueryServiceHost` uses `UseAuthentication()` followed by `UseAuthorization()` before the interactive Razor component mapping.
    - `QueryServiceHost` routing uses an authorization-aware route view and redirects unauthenticated users to `/authentication/login` with a full page load.
    - No host-local duplicate OpenID Connect protocol wiring is introduced.
  - **Definition of Done**:
    - Code implemented for the end-to-end authenticated Query browser path.
    - Logging and error-handling expectations preserved from the existing shared authentication model.
    - Relevant source-based tests added or updated and passing.
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for all changed code, including developer-level comments on every changed class, constructor, method, and relevant property, including internal and non-public members.
    - Wiki review obligation considered for this slice; if developer-facing behavior or workflow explanation changes require documentation updates, those updates are made before completion, or the later final wiki review work item records why no page update was required.
    - Can execute end-to-end via the AppHost services-mode environment and a browser visit to the Query host URL.
  - [x] **Task 1.1: Align QueryServiceHost startup with the shared browser-host authentication composition** - Completed
    - [x] **Step 1**: Update `src/Hosts/QueryServiceHost/Program.cs` to register the shared Keycloak browser-host authentication using the existing shared client identifier `search-workbench`.
    - [x] **Step 2**: Map the shared authentication lifecycle endpoints under `/authentication` using the shared service-defaults extension.
    - [x] **Step 3**: Add `UseAuthentication()` and `UseAuthorization()` in the correct order before mapping the interactive Razor components.
    - [x] **Step 4**: Preserve the existing Query host service registrations and logging filters while limiting the change to authentication composition.
    - [x] **Step 5**: Apply the mandatory developer-level documentation standard from `./.github/instructions/documentation-pass.instructions.md` to every changed C# member, including method purpose, logical flow, and parameter meaning where applicable.
    - **Completed summary**: Updated `src/Hosts/QueryServiceHost/Program.cs` so the Query host now registers `AddKeycloakBrowserHostAuthentication("search-workbench")`, maps the shared `/authentication` lifecycle endpoints, and restores the authenticated principal with `UseAuthentication()` then `UseAuthorization()` before the protected interactive UI is mapped. Added developer-level comments to the changed bootstrap flow to satisfy `./.github/instructions/documentation-pass.instructions.md` for this task.
  - [x] **Task 1.2: Make QueryServiceHost routing authorization-aware** - Completed
    - [x] **Step 1**: Update `src/Hosts/QueryServiceHost/Components/Routes.razor` to use `AuthorizeRouteView` rather than a plain `RouteView`.
    - [x] **Step 2**: Add a host-local redirect component under `src/Hosts/QueryServiceHost/Components/Authentication/` if needed, matching the minimal pattern already proven in `IngestionServiceHost`.
    - [x] **Step 3**: Ensure the redirect component performs a full-page navigation to `/authentication/login` so the browser leaves the interactive circuit and begins the server-side challenge flow.
    - [x] **Step 4**: Keep the host-local component minimal and do not duplicate authentication constants or protocol logic already owned by `UKHO.Search.ServiceDefaults`.
    - [x] **Step 5**: Apply the mandatory comment and inline-explanation requirements from `./.github/instructions/documentation-pass.instructions.md` to all changed or new source files.
    - **Completed summary**: Updated `src/Hosts/QueryServiceHost/Components/Routes.razor` to use `AuthorizeRouteView`, added Query host authentication imports in `src/Hosts/QueryServiceHost/Components/_Imports.razor`, and created `src/Hosts/QueryServiceHost/Components/Authentication/RedirectToLogin.razor` plus `RedirectToLogin.razor.cs` so unauthorized interactive navigation now forces a full-page redirect to `/authentication/login` using the shared authentication defaults.
  - [x] **Task 1.3: Add source-level regression tests for Query host authentication composition** - Completed
    - [x] **Step 1**: Add or update tests in `test/QueryServiceHost.Tests/` to pin the expected startup composition in `Program.cs`.
    - [x] **Step 2**: Add or update tests to pin route-level use of `AuthorizeRouteView` and redirect-to-login behavior.
    - [x] **Step 3**: Ensure the tests verify ordering expectations for lifecycle endpoint mapping, authentication middleware, authorization middleware, and component mapping.
    - [x] **Step 4**: Follow `./.github/instructions/documentation-pass.instructions.md` for any changed or added test code, including explanatory comments for test intent and scenario significance.
    - **Completed summary**: Replaced the placeholder Query host test with `test/QueryServiceHost.Tests/QueryHostAuthenticationCompositionTests.cs`, which pins the shared authentication registration, middleware ordering, authorization-aware routing, and absence of host-local anonymous endpoint mappings. Validation performed: `QueryServiceHost.Tests` passed, `IngestionHostAuthenticationCompositionTests` passed as a regression check against the existing shared pattern, and `run_build` completed successfully.
  - **Completed summary**: Work Item 1 aligned `QueryServiceHost` with the shared Keycloak browser-host authentication model by updating `src/Hosts/QueryServiceHost/Program.cs`, `src/Hosts/QueryServiceHost/Components/_Imports.razor`, `src/Hosts/QueryServiceHost/Components/Routes.razor`, and the new Query host redirect component files, and by replacing the placeholder test with focused source-level composition tests in `test/QueryServiceHost.Tests/QueryHostAuthenticationCompositionTests.cs`. Validation performed: targeted Query host tests passed, targeted ingestion authentication regression tests passed, and the workspace build succeeded. Wiki review result: reviewed `wiki/keycloak-workbench-integration.md`; no wiki page update was made at this stage because that page also documents the exact shared realm redirect/origin contract, and Work Item 2 is required before a precise QueryServiceHost current-state narrative can be published without creating partial or misleading setup guidance.
  - **Files**:
    - `src/Hosts/QueryServiceHost/Program.cs`: consume the shared Keycloak browser-host authentication composition.
    - `src/Hosts/QueryServiceHost/Components/Routes.razor`: switch to authorization-aware routing.
    - `src/Hosts/QueryServiceHost/Components/Authentication/RedirectToLogin.razor`: minimal redirect component markup if needed.
    - `src/Hosts/QueryServiceHost/Components/Authentication/RedirectToLogin.razor.cs`: host-local redirect behavior if needed.
    - `test/QueryServiceHost.Tests/*`: source-level regression coverage for startup and route composition.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - Start the services-mode environment through the AppHost project.
    - Browse to the Query host HTTPS URL from `src/Hosts/QueryServiceHost/Properties/launchSettings.json`.
    - Verify that an unauthenticated visit starts the Keycloak login challenge.
    - Run only the relevant test project or targeted tests for Query host authentication composition; do **not** run the full test suite for this work package.
  - **User Instructions**:
    - If the local Keycloak environment was already imported previously, be aware that later realm JSON changes may require a fresh import before end-to-end sign-in validation is meaningful.

## Feature Slice 2: Shared Keycloak client contract and repository tracking rules

- [x] **Work Item 2: Pin the QueryServiceHost Keycloak endpoint contract and launch-settings tracking rules** - Completed
  - **Purpose**: Extend the runnable authentication slice so the checked-in Keycloak realm export and repository ignore behavior both reflect the real local callback contract that Query and Ingestion hosts depend on.
  - **Acceptance Criteria**:
    - The shared `search-workbench` client in `src/Hosts/AppHost/Realms/ukho-search-realm.json` includes the Query host redirect URI, logout callback URI, and browser origin.
    - Existing Workbench and Ingestion client values remain intact.
    - `.gitignore` explicitly leaves the checked-in Query and Ingestion `launchSettings.json` files trackable and reviewable.
    - Automated tests pin the realm export values for Query host and, if needed, the AppHost Keycloak reference for the Query host slice.
  - **Definition of Done**:
    - Keycloak realm export updated without removing existing host values.
    - Repository ignore rules updated or explicitly confirmed in a durable, reviewable way.
    - Relevant AppHost and realm configuration tests passing.
    - `./.github/instructions/documentation-pass.instructions.md` followed in full for all changed code and tests.
    - Wiki review obligation considered for the local setup and troubleshooting contract; if the contributor guidance needs refreshing, the update is made before completion or recorded in the final wiki review item.
    - Can execute end-to-end via a fresh or re-imported local Keycloak environment in which Query host sign-in no longer fails due to `redirect_uri` mismatch.
  - [x] **Task 2.1: Update the shared Keycloak realm export for QueryServiceHost** - Completed
    - [x] **Step 1**: Extend `src/Hosts/AppHost/Realms/ukho-search-realm.json` so the shared `search-workbench` client allows `https://localhost:7161/signin-oidc`.
    - [x] **Step 2**: Extend the same client so the allowed post-logout redirect values include `https://localhost:7161/signout-callback-oidc` and `https://localhost:7161`.
    - [x] **Step 3**: Extend the same client so the allowed browser origins include `https://localhost:7161`.
    - [x] **Step 4**: Verify that existing Ingestion and Workbench callback, logout, and origin values remain present.
    - [x] **Step 5**: Preserve the repository's role and client structure; do not introduce a new Query-specific Keycloak client.
    - **Completed summary**: Updated `src/Hosts/AppHost/Realms/ukho-search-realm.json` so the shared `search-workbench` client now includes the Query host redirect URI, post-logout redirect URIs, and web origin while preserving the existing Workbench and Ingestion entries.
  - [x] **Task 2.2: Make the launch-settings tracking contract explicit** - Completed
    - [x] **Step 1**: Review `.gitignore` for any current or future-brittle patterns that could hide `src/Hosts/QueryServiceHost/Properties/launchSettings.json` or `src/Hosts/IngestionServiceHost/Properties/launchSettings.json`.
    - [x] **Step 2**: Add explicit negation rules if needed so both files are clearly trackable in Git even if broader ignore rules exist nearby.
    - [x] **Step 3**: Keep the change narrowly scoped to the two named files and avoid unrelated ignore-rule cleanup.
    - [x] **Step 4**: Ensure the final repository state makes the launch-settings contract visible and reviewable because those files define the local Keycloak callback endpoints.
    - **Completed summary**: Updated `.gitignore` with explicit negation rules for the checked-in Query and Ingestion host `launchSettings.json` files so their HTTPS port contract remains visible and durable in source control.
  - [x] **Task 2.3: Expand AppHost and realm regression tests** - Completed
    - [x] **Step 1**: Update `test/AppHost.Tests/KeycloakBrowserHostClientConfigurationTests.cs` so the Query host redirect URI, logout callback URI, and browser origin are pinned alongside the existing browser hosts.
    - [x] **Step 2**: Add or update a source-level AppHost test to pin the Query host Keycloak reference and startup dependency if this is not already covered sufficiently.
    - [x] **Step 3**: Keep the tests source-based and lightweight so they catch composition drift without requiring the full distributed environment to boot.
    - [x] **Step 4**: Apply the mandatory comment and explanation requirements from `./.github/instructions/documentation-pass.instructions.md` to all changed test code.
    - **Completed summary**: Expanded `test/AppHost.Tests/KeycloakBrowserHostClientConfigurationTests.cs` to pin the Query host URLs and added `test/AppHost.Tests/QueryHostKeycloakReferenceTests.cs` to pin the AppHost Keycloak reference and startup dependency for the Query host slice.
  - [x] **Task 2.4: Validate the refreshed local sign-in contract** - Completed
    - [x] **Step 1**: Re-run the targeted AppHost and Query host tests only; do not run the full test suite.
    - [x] **Step 2**: Validate manually that the Query host sign-in flow works when the Keycloak realm import is fresh.
    - [x] **Step 3**: If local testing still uses an old imported realm, delete the persisted Keycloak Docker volume and restart the AppHost environment before retrying.
    - [x] **Step 4**: Record this fresh-import requirement in the final implementation notes because it is a common source of false negative validation.
    - **Completed summary**: Validation performed in-tool: targeted `AppHost.Tests` and `QueryServiceHost.Tests` both passed and the workspace build succeeded. Manual browser validation remains the required user-facing follow-up for a fresh AppHost/Keycloak import, and the fresh-import requirement was carried forward into the updated wiki guidance so the local `redirect_uri` troubleshooting path is now explicit.
  - **Completed summary**: Work Item 2 updated the shared Keycloak client contract in `src/Hosts/AppHost/Realms/ukho-search-realm.json`, made the tracked launch-settings contract explicit in `.gitignore`, expanded the AppHost regression coverage with Query host realm and orchestration tests, and refreshed `wiki/keycloak-workbench-integration.md` so the current-state guidance now includes QueryServiceHost, the shared callback URLs, and the refreshed troubleshooting narrative for stale realm imports. Validation performed: `AppHost.Tests` passed, `QueryServiceHost.Tests` passed, and `run_build` completed successfully. Wiki review result: updated `wiki/keycloak-workbench-integration.md`; no other wiki pages required changes because that page is the repository's current operational reference for shared browser-host Keycloak behavior and now fully covers the Query host additions introduced by this work item.
  - **Files**:
    - `src/Hosts/AppHost/Realms/ukho-search-realm.json`: add Query host redirect, logout, and origin values.
    - `.gitignore`: make the two named launch settings files explicitly trackable if required.
    - `test/AppHost.Tests/KeycloakBrowserHostClientConfigurationTests.cs`: pin Query host client configuration values.
    - `test/AppHost.Tests/*`: pin Query host Keycloak orchestration/reference behavior if needed.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - Run the targeted AppHost tests covering realm export and orchestration behavior.
    - Start the AppHost services-mode environment.
    - If necessary, force a fresh Keycloak realm import by deleting the persisted Keycloak Docker volume before restart.
    - Browse to `https://localhost:7161` and verify the sign-in flow completes without a `redirect_uri` error.
  - **User Instructions**:
    - Use the AppHost-orchestrated environment rather than launching `QueryServiceHost` directly when validating the Keycloak integration.
    - If sign-in fails after a realm JSON update, refresh the Keycloak data volume before concluding that the code is incorrect.

## Feature Slice 3: Final validation, wiki review, and work package close-out

- [x] **Work Item 3: Record validation results and complete the mandatory wiki review** - Completed
  - **Purpose**: Close the work package with explicit validation evidence and the mandatory wiki-maintenance decision required by `./.github/instructions/wiki.instructions.md`.
  - **Acceptance Criteria**:
    - The final execution record identifies the targeted tests and manual verification steps that were run.
    - The final execution record explicitly states which wiki or repository guidance pages were updated, created, retired, or why no update was needed.
    - Any updated wiki or guidance content explains changed contributor-facing behavior in clear narrative prose, defines technical terms when first introduced, and includes examples or walkthrough material where that materially improves understanding.
  - **Definition of Done**:
    - Targeted validation completed and recorded.
    - Mandatory wiki review completed and recorded explicitly.
    - If wiki changes are required, they are made before this work item is marked complete.
    - If no wiki changes are required, the recorded no-change result names the pages reviewed and explains why they remain sufficient.
    - The work package close-out summary references `./.github/instructions/documentation-pass.instructions.md` and `./.github/instructions/wiki.instructions.md` as completion gates that were satisfied.
  - [x] **Task 3.1: Record targeted validation outcomes** - Completed
    - [x] **Step 1**: List the targeted Query host and AppHost tests that were executed.
    - [x] **Step 2**: Record the manual sign-in and logout verification results for the Query host path.
    - [x] **Step 3**: Record whether a fresh Keycloak import was required to verify the realm JSON changes.
    - **Completed summary**: Targeted validation recorded as follows: `AppHost.Tests` passed, `QueryServiceHost.Tests` passed, `UKHO.Search.ServiceDefaults.Tests` passed, and the workspace build passed. Manual validation was completed in two stages: first the Query host sign-in flow worked after the shared Keycloak client realm entries were refreshed, and then the user confirmed that after the shared cookie-size fixes they could authenticate successfully in `QueryServiceHost` and subsequently load `IngestionServiceHost` and `WorkbenchHost` without `HTTP 431`. A fresh Keycloak import was required for the realm JSON redirect-uri verification path introduced in Work Item 2, but it was not the relevant fix for the later `HTTP 431` issue, which was resolved by the shared cookie isolation and server-side ticket-storage changes.
  - [x] **Task 3.2: Perform the mandatory wiki review** - Completed
    - [x] **Step 1**: Review the existing authentication and setup guidance pages most likely to be affected, including `wiki/keycloak-workbench-integration.md` and any related setup or troubleshooting pages.
    - [x] **Step 2**: Decide whether the Query host authentication alignment changes the current contributor-facing explanation enough to require updates.
    - [x] **Step 3**: If updates are needed, revise the relevant pages in longer, book-like narrative prose rather than terse bullet-only notes, define technical terms when first introduced, and include a short practical walkthrough where that improves comprehension.
    - [x] **Step 4**: If no updates are needed, record the exact pages reviewed and the reason the existing guidance remains sufficient.
    - **Completed summary**: Reviewed `wiki/keycloak-workbench-integration.md` as the primary current-state operational guide for shared browser-host Keycloak behavior. Updated that page again so the `HTTP 431` guidance now reflects the final repository design, including host-specific cookie-name isolation, not persisting OIDC tokens in the auth cookie, server-side authentication ticket storage, and a new quick-diagnosis table row for the `HTTP 431` symptom. No other wiki pages required updates because no other page owns the shared browser-host authentication setup, redirect-uri troubleshooting path, or localhost cookie-behavior explanation.
  - [x] **Task 3.3: Produce the final work package close-out note** - Completed
    - [x] **Step 1**: Summarize the overall approach used to align Query host authentication with the shared browser-host model.
    - [x] **Step 2**: Summarize the key implementation considerations, especially middleware ordering, exact redirect URI matching, and the persisted Keycloak realm import behavior.
    - [x] **Step 3**: Record the wiki review result in the explicit format required by `./.github/instructions/wiki.instructions.md`.
    - **Completed summary**: The work package aligned `QueryServiceHost` with the repository's shared browser-host Keycloak composition, updated the shared realm export and tracked launch settings contract, added source-level regression tests for both host composition and AppHost realm/orchestration behavior, and then refined the shared authentication runtime so localhost browser hosts no longer overload each other with large cookies. Key implementation considerations recorded for close-out are: authentication and authorization middleware ordering must remain before protected interactive component mapping; Keycloak redirect URIs are exact contracts and must stay synchronized with checked-in launch settings; stale Keycloak volumes can hide valid realm JSON changes until a fresh import occurs; and localhost browser hosts need both host-specific cookie names and compact server-side ticket-backed cookies to avoid cross-host `HTTP 431` failures. Wiki review result: updated `wiki/keycloak-workbench-integration.md`; no wiki pages were created, retired, or renamed for this work package.
  - **Completed summary**: Work Item 3 closed the work package by recording the targeted validation results, capturing the user-confirmed manual verification that Query, Ingestion, and Workbench now work in the same browser session, and completing the mandatory wiki review required by `./.github/instructions/wiki.instructions.md`. Validation performed for close-out: `run_build` passed, `AppHost.Tests` passed, `QueryServiceHost.Tests` passed, and `UKHO.Search.ServiceDefaults.Tests` passed. Wiki review result: updated `wiki/keycloak-workbench-integration.md` to keep the repository's current-state operational guidance aligned with the final runtime behavior, including redirect-uri setup, stale realm import troubleshooting, localhost cookie isolation, server-side ticket storage, and `HTTP 431` diagnosis.
  - **Files**:
    - `wiki/keycloak-workbench-integration.md`: update if the wiki review determines the page must now describe QueryServiceHost explicitly.
    - other reviewed wiki or repository guidance pages as required by the review outcome.
  - **Work Item Dependencies**: Work Item 1 and Work Item 2.
  - **Run / Verification Instructions**:
    - Confirm targeted tests passed.
    - Confirm manual Query host sign-in and logout verification completed.
    - Confirm the wiki review result is explicitly recorded before closing the work package.
  - **User Instructions**:
    - None beyond the normal targeted validation and wiki review expectations for this repository.

## Suggested execution summary

The recommended implementation order is:

1. complete the Query host authentication composition and routing slice;
2. extend the shared Keycloak realm export and `.gitignore` contract;
3. add or update targeted regression tests for both slices;
4. validate using targeted tests and AppHost-based manual sign-in verification; and
5. finish with the mandatory wiki review and explicit close-out record.

## Key implementation considerations

- **Use the shared authentication path**: `QueryServiceHost` must consume `UKHO.Search.ServiceDefaults` rather than implementing host-local OpenID Connect wiring.
- **Protect both initial requests and interactive routing**: the server-side fallback authorization and the Blazor authorization-aware route view should work together.
- **Treat redirect URIs as exact contracts**: Keycloak matches callback URLs exactly, including the port, so the checked-in launch settings and realm export must stay synchronized.
- **Remember Keycloak persistence behavior**: a changed realm export is not automatically re-imported into an already-populated Keycloak data volume.
- **Keep repository tracking explicit**: the Query and Ingestion launch settings files are part of the developer-facing authentication contract and should remain clearly trackable.
- **Do not run the full test suite for this work package**: use targeted tests only, in line with repository guidance.
