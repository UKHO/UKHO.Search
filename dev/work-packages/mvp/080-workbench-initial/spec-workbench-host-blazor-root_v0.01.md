# Work Package: `080-workbench-initial` — `WorkbenchHost` temporary Blazor root page

**Target output path:** `dev/work-packages/mvp/080-workbench-initial/spec-workbench-host-blazor-root_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for converting `WorkbenchHost` to serve a single temporary Blazor page at `/`.
- `v0.01` — Confirms this specification belongs in the existing work package folder `dev/work-packages/mvp/080-workbench-initial`.
- `v0.01` — Captures the requirement that the temporary page should display `Hello UKHO Workbench`.
- `v0.01` — Captures the requirement to remove WebAssembly-related implementation concerns from the `WorkbenchHost` solution area.
- `v0.01` — Assumes the previously deleted client-side projects are already out of scope for this specification and will not be restated as implementation work here.
- `v0.01` — Confirms that the temporary root page should explicitly use `InteractiveServer`.
- `v0.01` — Confirms that WebAssembly removal should be strict and should cover all WebAssembly-specific packages, configuration, assets, and startup wiring that are no longer used by `WorkbenchHost`.
- `v0.01` — Confirms that no new automated verification is required for the temporary root page; manual verification is sufficient.
- `v0.01` — Confirms that `AppHost`/Aspire should remain the expected launch and manual verification path for `WorkbenchHost`.

## 1. Overview

### 1.1 Purpose

This specification defines a focused change to make `WorkbenchHost` serve a temporary Blazor page from `/`.

The purpose of this work is to simplify the current Workbench hosting model so that the host itself serves a minimal temporary user interface while removing WebAssembly-related implementation concerns that are no longer needed.

### 1.2 Scope

This specification currently includes:

- converting `WorkbenchHost` to serve a single temporary Blazor page at `/`
- rendering the text `Hello UKHO Workbench` on that page
- using `InteractiveServer` for the temporary root page
- removing all WebAssembly-specific packages, configuration, assets, and startup wiring that are no longer needed for the host-based approach
- keeping the work package focused on the temporary host-served page only

This specification currently excludes:

- broader Workbench shell functionality
- module discovery, loading, or composition
- Search-specific UI or workflows
- unrelated restructuring outside what is needed for `WorkbenchHost` to serve the temporary page

### 1.3 Stakeholders

- developers working on the Workbench host
- maintainers of repository architecture and startup behavior
- contributors who will build later Workbench functionality on top of the host

### 1.4 Definitions

- `WorkbenchHost`: the retained Workbench host application responsible for serving the temporary UI
- `temporary Blazor page`: a minimal Razor component-based page used as a transitional placeholder for the Workbench UI
- `InteractiveServer`: the Blazor render mode used for server-side interactive Razor components in this repository
- `WebAssembly-related`: packages, configuration, assets, and host wiring that only exist to support the removed client-side WebAssembly approach

## 2. System context

### 2.1 Current state

The Workbench area has been simplified by removing the separate client-side projects.

A new focused specification is needed for the remaining host so that it serves a temporary placeholder page directly from `/` and no longer carries WebAssembly-oriented behavior.

### 2.2 Proposed state

In the proposed state after this work:

- `WorkbenchHost` serves a single temporary Blazor page from `/`
- the page displays `Hello UKHO Workbench`
- the root page explicitly uses `InteractiveServer`
- all WebAssembly-specific packages, configuration, assets, and startup wiring that are no longer needed are removed
- the Workbench host remains the active entry point for this temporary UI
- no new automated verification is introduced for this temporary page
- `AppHost`/Aspire remains the expected launch and manual verification path

### 2.3 Assumptions

- the deleted client-side projects are already removed and are not part of this specification's implementation scope
- the temporary page is intentionally minimal and transitional
- `InteractiveServer` is the required render mode for the temporary page
- WebAssembly cleanup should be comprehensive for items that are specific to the removed approach and no longer used by `WorkbenchHost`
- only the minimum host changes needed for the page and the required WebAssembly cleanup should be specified
- manual verification is sufficient for this temporary page in this work package
- `AppHost`/Aspire remains the expected developer run path for this temporary host-served page

### 2.4 Constraints

- the page must be served from `/`
- the page content must be `Hello UKHO Workbench`
- the page must use `InteractiveServer`
- all WebAssembly-specific packages, configuration, assets, and startup wiring that are no longer needed must be removed
- no new automated verification is required for this temporary page
- `AppHost`/Aspire must remain the expected launch and manual verification path
- the specification must stay focused on `WorkbenchHost`

## 3. Component / service design (high level)

### 3.1 Components

1. `WorkbenchHost`
   - serves the temporary Blazor page
   - contains the host startup and UI wiring needed for the temporary experience

2. `Temporary root page`
   - reachable at `/`
   - uses `InteractiveServer`
   - renders `Hello UKHO Workbench`

### 3.2 Data flows

#### Runtime flow

1. the developer starts the host through `AppHost`/Aspire
2. the root route `/` is opened
3. `WorkbenchHost` renders the temporary Blazor page
4. the page displays `Hello UKHO Workbench`

### 3.3 Key decisions

- the temporary experience is host-served rather than WebAssembly-hosted
- the initial route in scope is `/`
- the page is intentionally minimal and transitional

## 4. Functional requirements

1. `WorkbenchHost` shall serve a single temporary Blazor page from `/`.
2. The root page shall display `Hello UKHO Workbench`.
3. The root page shall explicitly use `InteractiveServer`.
4. All WebAssembly-specific packages, configuration, assets, and startup wiring that are no longer needed for the host-served approach shall be removed.
5. The scope of this work shall remain limited to what is required for `WorkbenchHost` to serve the temporary page.
6. The work package shall not require new automated verification for the temporary root page.
7. `AppHost`/Aspire shall remain the expected launch and manual verification path for `WorkbenchHost` in this work package.

## 5. Non-functional requirements

1. The implementation should remain minimal and easy to replace in later Workbench work.
2. The solution should remain aligned with repository hosting and architecture conventions.
3. Unrelated changes outside the focused host-page conversion should be avoided.
4. The developer run path should remain straightforward by continuing to use `AppHost`/Aspire for launch and manual verification.

## 6. Data model

No new data model is required for this temporary page.

## 7. Interfaces & integration

1. `WorkbenchHost`
   - must expose the temporary page at `/`
   - must support `InteractiveServer` for the temporary page
   - must not retain any WebAssembly-specific packages, configuration, assets, or startup wiring once no longer needed

2. `AppHost`/Aspire
   - remains the expected way to launch and manually verify `WorkbenchHost`

## 8. Observability (logging/metrics/tracing)

No additional observability requirements are currently defined for this temporary page.

## 9. Security & compliance

1. The temporary page should not introduce Search-specific data or privileged behavior.
2. The host surface should remain minimal for this transitional state.

## 10. Testing strategy

1. Verify the solution builds successfully.
2. Verify the remaining tests pass.
3. Verify manually through `AppHost`/Aspire that opening `/` renders `Hello UKHO Workbench`.
4. Do not require new automated verification specifically for the temporary root page.

## 11. Rollout / migration

1. Use this work to establish the simplified temporary host-served Workbench entry point.
2. Continue to use `AppHost`/Aspire as the expected launch path for this temporary state.
3. Defer broader Workbench UI capability to later work packages.

## 12. Open questions

No open questions are currently recorded.
