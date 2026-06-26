# Work Package: `080-workbench-initial` — Initial Workbench shell scaffolding

**Target output path:** `dev/work-packages/mvp/080-workbench-initial/spec-workbench-initial_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for the Workbench foundation work package.
- `v0.01` — Captures the requirement to introduce an extensible, modular Workbench application shell inspired by the platform capabilities previously valued in Theia.
- `v0.01` — Confirms that this first work package is limited to scaffolding, Aspire wiring, hosted Blazor WebAssembly setup, placeholder tests, and a minimal `Hello UKHO Workbench` client screen.
- `v0.01` — Confirms that Workbench projects must remain entirely independent of UKHO Search-specific behavior so the shell can be lifted into other repositories later.
- `v0.01` — Records the intended server and client Onion architecture project layout under `src/workbench/server` and `src/workbench/client`, with shared `UKHO.Workbench.Common` under `src/workbench`.
- `v0.01` — Confirms that placeholder manifest contract types should be deferred until a later work package because module definitions have not yet been defined.
- `v0.01` — Confirms that test coverage structure for this work package should use one mirrored test project per production project.
- `v0.01` — Confirms that the mirrored test-project rule applies to every production project, including `WorkbenchHost`, `WorkbenchClient`, and `UKHO.Workbench.Common`.
- `v0.01` — Confirms that `WorkbenchHost` should provide static hosting for `WorkbenchClient` plus default Aspire and health wiring only, with no extra placeholder API surface in this work package.
- `v0.01` — Confirms that `UKHO.Workbench.Common` should exist as a project shell only in this work package, with no concrete shared types yet.
- `v0.01` — Confirms that the initial Blazor client should use the simplest possible shape: a minimal layout plus a single home page that displays `Hello UKHO Workbench`.
- `v0.01` — Confirms that all new Workbench production and test projects should be added to the main repository solution immediately in this work package.
- `v0.01` — Confirms that runnable verification means the solution builds, tests pass, and Aspire can be started manually so the default Workbench link shows the initial page; this startup verification is manual rather than automated.
- `v0.01` — Confirms that placeholder tests should remain compile-only with trivial assertions in this work package, even though Blazor UI test projects still include a suitable `bUnit` package reference.
- `v0.01` — Confirms that the `AppHost` requirement should be expressed simply: the default Aspire link for `WorkbenchHost` must open the hosted `WorkbenchClient` at `/`.
- `v0.01` — Confirms that tests are non-material in this work package beyond basic scaffold presence, so the specification should not over-constrain placeholder test content or naming detail.
- `v0.01` — Confirms that non-host projects should include minimal composition-root wiring placeholders now, including lightweight DI extension methods in services and infrastructure projects.
- `v0.01` — Confirms that the specification should not standardize naming for placeholder DI entry points in this work package and should leave that detail to implementation.
- `v0.01` — Confirms that the initial host and client experience should be anonymous only for this work package, with no active authentication flow or auth placeholder hooks required.
- `v0.01` — Confirms that the hosting model should be specified only at the behavior level: Aspire needs to know about `WorkbenchHost`, and `WorkbenchHost` must serve the client from `/`; the specification should otherwise keep hosting details as simple and unconstrained as possible.
- `v0.01` — Confirms that `AppHost` is the only required run and startup path for this work package; direct standalone execution of `WorkbenchHost` does not need to be specified or verified yet.
- `v0.01` — Confirms that package and version management should simply follow whatever conventions already exist in the repository, without adding a new centralization requirement in this work package.
- `v0.01` — Confirms that the initial server host should not define any explicit API endpoints beyond the minimum host plumbing already implied by the requirement.
- `v0.01` — Confirms that placeholder tests should still pass where present, but they must remain trivial `Assert.True()`-style placeholders and nothing more.
- `v0.01` — Confirms that no additional README or developer notes outside this specification are required in this work package.
- `v0.01` — Confirms that this work package should not require any explicit CI or workflow changes beyond whatever existing automation naturally picks up from adding the new projects to the main solution.
- `v0.01` — Confirms that the specification should explicitly require Onion-aligned project reference direction between the new Workbench projects, not just folder naming and placement.
- `v0.01` — Confirms that the hosted client route should be only `/` in this work package so clicking the Aspire dashboard link always opens the initial Workbench screen directly.
- `v0.01` — Confirms that the client requirement should stay outcome-based and minimal: Blazor must render `Hello UKHO Workbench` at `/`, with no further route, page, or template-cleanup requirements specified in this work package.
- `v0.01` — Confirms that unused default Blazor template pages, navigation, and components should be removed so the initial client remains intentionally minimal.
- `v0.01` — Corrects the infrastructure project naming and reference direction to use the `UKHO.Workbench.*` pattern consistently, with `WorkbenchHost -> UKHO.Workbench.Infrastructure -> UKHO.Workbench.Services -> UKHO.Workbench -> UKHO.Workbench.Common` and the mirrored client-side equivalent.
- `v0.01` — Confirms that `UKHO.Workbench.Common` should be referenced directly by the domain projects only, with higher layers reaching it through chained Onion project references.
- `v0.01` — Confirms that Aspire should register only `WorkbenchHost` in this work package, with no separate `WorkbenchClient` Aspire resource.
- `v0.01` — Confirms for the revised server-interactive Workbench direction that the existing server-side Workbench structure should remain exactly as it is, except that `UKHO.Workbench.Common` should be removed entirely because it is no longer needed.
- `v0.01` — Confirms that this work package should not prescribe detailed server-side test updates beyond requiring that the solution builds and the remaining tests pass after the obsolete client and common projects are removed.
- `v0.01` — Confirms that conversion to interactive server Blazor should include full removal of obsolete WebAssembly-specific packages, files, static assets, and configuration that are no longer needed.
- `v0.01` — Confirms that no new automated UI verification is required for this work package beyond requiring build success and that the remaining tests pass.
- `v0.01` — Confirms that the specification should not constrain the `WorkbenchHost` route surface beyond requiring that `/` serves the minimal `Hello UKHO Workbench` page.
- `v0.01` — Corrects the draft scope to remain strictly minimal: aside from the explicitly requested project removals and serving a Blazor page from `WorkbenchHost` at `/`, nothing else should change unless explicitly confirmed.

## 1. Overview

### 1.1 Purpose

This specification defines the first incremental work package for a new extensible application shell named `Workbench`.

The purpose of this work package is to establish the initial project structure, architectural boundaries, Aspire registration, hosted client/server wiring, and test placeholders needed to make the shell buildable and runnable inside this repository without yet implementing module discovery, module loading, or real feature composition.

### 1.2 Scope

This specification currently includes:

- removing the existing client-side Workbench projects and their mirrored test projects
- removing `UKHO.Workbench.Common` and its mirrored test project
- preserving the existing server-side Workbench project structure under `src/workbench/server`
- registering `WorkbenchHost` with the existing Aspire `AppHost`
- converting `WorkbenchHost` to use server interactive Blazor rendering
- serving a minimal `Hello UKHO Workbench` page from `WorkbenchHost` at `/`
- ensuring the solution builds and runs successfully after the simplification

This specification currently excludes:

- changes to the existing Workbench server-side layer boundaries beyond the explicit removal of `UKHO.Workbench.Common`
- module discovery, loading, activation, or runtime composition
- Search-specific UI, services, workflows, or domain coupling
- feature-complete workbench capabilities such as docking, commands, menus, layout persistence, explorers, or context-awareness
- additional README files or broader developer documentation updates outside this work package specification
- explicit CI or workflow changes beyond any incidental effect of removing projects from the main solution

### 1.3 Stakeholders

- developers building the future Workbench platform
- developers integrating Workbench-hosted modules
- UKHO Search contributors who will deliver the first module on top of the Workbench shell
- maintainers of repository architecture, test structure, and Aspire orchestration

### 1.4 Definitions

- `Workbench`: the new reusable application shell intended to provide platform concepts previously valued in Theia, such as commands, views, layout composition, and context-aware extension points
- `module`: a future composable feature unit that will have a manifest and server-side implementation, with any future client concerns to be defined later if needed
- `interactive server Blazor`: a deployment model where Razor components are rendered and interacted with through the server-hosted Blazor runtime
- `Onion architecture`: the repository architecture pattern where dependencies point inward from hosts to infrastructure to services to domain

## 2. System context

### 2.1 Current state

The repository currently contains UKHO Search solutions and infrastructure, plus an established architectural preference for Onion architecture and Aspire-based orchestration.

A replacement is needed for the richer shell characteristics previously experienced through Theia. The desired Workbench direction is to provide a modular shell in which:

- views and widgets are first-class concepts
- navigation follows predictable built-in patterns
- commands can be reused across menus, toolbars, keybindings, command palettes, and buttons
- menus and toolbars are contribution-based
- layout and docking are platform concepts
- frontend and backend concerns are cleanly separated
- extension and module composition is natural
- lifecycle hooks are predictable
- trees, selections, and explorers fit the platform model
- context-aware enablement and visibility are built in

However, none of those runtime behaviors are required to be fully implemented in this first work package.

### 2.2 Proposed state

In the proposed state after this work package:

- the existing `Workbench` solution area remains under `src/workbench`
- the server side remains structurally unchanged and contains:
  - `src/workbench/server/UKHO.Workbench`
  - `src/workbench/server/UKHO.Workbench.Services`
  - `src/workbench/server/UKHO.Workbench.Infrastructure`
  - `src/workbench/server/WorkbenchHost`
- the client-side Workbench projects are removed entirely
- `UKHO.Workbench.Common` is removed entirely because it is no longer needed
- mirrored test projects for the removed client and common production projects are removed entirely
- the updated Workbench production and test projects remain represented in the main repository solution
- `WorkbenchHost` is registered with the existing Aspire `AppHost`
- Aspire registers only `WorkbenchHost`
- the Aspire dashboard default link for `WorkbenchHost` opens `WorkbenchHost` at `/`
- the hosted surface required by this work package is only that `/` lands directly on the initial Blazor-rendered screen
- `WorkbenchHost` exposes only the minimum server-rendered Blazor and default Aspire or health surface needed for runtime wiring
- no explicit Workbench-specific API endpoints are introduced in this phase
- the initial hosted Workbench experience is anonymous only, with no active authentication flow in this phase
- Aspire only needs to know about `WorkbenchHost`, and `WorkbenchHost` serves the Workbench experience from `/`
- `AppHost` is the only required developer run path in this phase
- non-host services and infrastructure projects include minimal DI registration or composition-root placeholder wiring for future host integration
- the visible UI uses the simplest possible server-rendered Blazor shape, consisting of a minimal layout and a single page showing `Hello UKHO Workbench`
- obsolete WebAssembly-specific packages, files, static assets, and configuration are removed entirely
- manual Aspire startup verification confirms that the default Workbench link opens the initial page successfully
- no new automated UI verification is prescribed for the root page beyond normal build and remaining-test validation
- no additional route constraints are prescribed beyond requiring that `/` serves the initial page
- no detailed test refactoring requirements are prescribed beyond keeping the remaining test estate passing
- Workbench projects are intentionally ignorant of UKHO Search-specific concerns

### 2.3 Assumptions

- the new Workbench projects will be added to the existing repository solution and orchestration model rather than maintained as a completely separate repo or independent orchestration surface
- the hosting approach should remain as simple as possible and should not be over-constrained beyond requiring `WorkbenchHost` to serve the interactive server-rendered experience from `/`
- Aspire orchestration in this phase only needs to know about `WorkbenchHost` because the experience is hosted entirely through it
- direct standalone launch requirements for `WorkbenchHost` are deferred because the initial developer workflow is centered on `AppHost`
- placeholder tests are acceptable for this first work package as long as the solution remains buildable and runnable
- mirrored testing means one test project per production project rather than aggregate server/client-only test containers, and this includes hosts and the shared common project
- repository solution inclusion is part of the initial scaffolding rather than a follow-on package concern
- the host endpoint surface for this first work package is intentionally minimal and limited to server-rendered UI delivery plus default platform wiring
- no placeholder API area is needed in `WorkbenchHost` for this work package
- authentication is deferred entirely from this first work package rather than introduced as active behavior or placeholder hook wiring
- the shared common project is being created ahead of need and may initially contain no concrete shared types
- minimal composition-root placeholders in services and infrastructure projects are acceptable even before those projects contain meaningful runtime behavior
- naming of placeholder DI entry points does not need to be standardized by this work package as long as the composition wiring is present and usable
- package references and version declarations should follow the repository's existing conventions rather than introducing new package-management rules in this work package
- Onion intent in this work package should be reflected in actual project references, not only in naming or folder structure
- `UKHO.Workbench.Common` should sit at the innermost edge of the chain and not be referenced directly by higher layers when chained references already provide access
- the initial Blazor requirement is limited to rendering `Hello UKHO Workbench`, with removal of obsolete WebAssembly-specific implementation artifacts that are no longer needed
- runtime verification through Aspire is expected to include a manual startup check rather than an automated end-to-end launch sequence
- the first test baseline is non-material and exists only to satisfy scaffold expectations through trivial passing assertions rather than meaningful behavioral coverage
- module functionality and manifest contract definitions will be added in later work packages rather than front-loading abstractions that are not yet needed
- UKHO Search will be the first module built on Workbench, but no Workbench project may take a dependency on Search-specific code or concepts

### 2.4 Constraints

- every Workbench project must remain generic and reusable outside UKHO Search
- this work package must remain strictly limited to the explicitly requested removals and serving the Blazor page at `/`, with no other changes unless explicitly confirmed
- the existing server side must remain structurally unchanged under `src/workbench/server`, except for removal of `UKHO.Workbench.Common`
- this work package must remove the obsolete client and common projects and their mirrored test projects
- `WorkbenchHost` must be registered with Aspire `AppHost`
- `WorkbenchHost` must use server interactive Blazor rendering
- the initial hosted client route must be available directly at `/`
- the initial UI must remain minimal and limited to `Hello UKHO Workbench`
- obsolete WebAssembly-specific packages, files, static assets, and configuration must be removed entirely
- no additional route constraints are required beyond serving `Hello UKHO Workbench` from `/`
- no module definition loading or runtime module composition is required in this work package
- no new automated UI verification is required beyond build success and remaining tests passing
- the specification must not prescribe detailed server-side test refactoring beyond requiring that the remaining tests pass
- the solution must build and run successfully

### 2.5 Open questions

No open questions are currently recorded.

## 3. Component / service design (high level)

### 3.1 Components

1. `Server Workbench domain`
   - `UKHO.Workbench`
   - contains server-side Workbench domain models and abstractions
   - must remain independent of UKHO Search

2. `Server Workbench services`
   - `UKHO.Workbench.Services`
   - contains server-side application or domain services supporting the host

3. `Server Workbench infrastructure`
   - `UKHO.Workbench.Infrastructure`
   - contains infrastructure implementations for server-side concerns

4. `WorkbenchHost`
   - host for the server-side Workbench runtime
   - renders the interactive server Blazor experience
   - participates in Aspire orchestration

5. `Workbench test projects`
   - the remaining server-side test estate after removal of obsolete client and common test projects
   - no further detailed test reshaping is required by this work package

### 3.2 Data flows

#### Initial runtime flow

1. Aspire starts `WorkbenchHost`
2. the default host link opens the hosted Workbench web experience
3. `WorkbenchHost` renders the interactive server Blazor experience
4. the root page displays `Hello UKHO Workbench`

#### Initial development flow

1. developers open the repository solution
2. Workbench projects build as part of the repository solution
3. obsolete client-side and common projects are no longer present in the solution or on disk
4. the solution can be launched and verified through the existing Aspire developer workflow

### 3.3 Key decisions

- Workbench is being introduced as a reusable shell, not a Search-specific shell
- Search will be the first module, but Workbench itself must remain ignorant of Search
- the preserved server side continues to follow the existing Onion architecture structure
- `UKHO.Workbench.Common` and all client-side Workbench projects are removed rather than replaced with new shared or WebAssembly layers in this work package
- the shell will be built incrementally through multiple work packages
- the first work package is intentionally limited to simplification, server-hosted UI wiring, and removal of obsolete WebAssembly artifacts rather than implementing the full module model or shell capabilities

## 4. Functional requirements

1. The system shall remove and delete entirely `UKHO.Workbench.Client.*` production projects and `UKHO.Workbench.Client.Tests.*` test projects.
2. The system shall remove and delete entirely `UKHO.Workbench.Common` and `UKHO.Workbench.Common.Tests`.
3. The system shall remove and delete entirely `WorkbenchClient` and `WorkbenchClient.Tests`.
4. The server-side Workbench structure shall otherwise remain exactly as it is and shall continue to include `UKHO.Workbench`, `UKHO.Workbench.Services`, `UKHO.Workbench.Infrastructure`, and `WorkbenchHost`.
5. The updated set of remaining Workbench production and test projects shall be represented correctly in the main repository solution.
6. `WorkbenchHost` shall be registered with the repository's Aspire `AppHost`.
7. Aspire shall register only `WorkbenchHost` for this Workbench experience.
8. The default Aspire dashboard link for `WorkbenchHost` shall open `WorkbenchHost` at `/`.
9. `WorkbenchHost` shall use server interactive Blazor rendering.
10. `WorkbenchHost` shall expose only the minimum interactive server Blazor and default Aspire or health wiring required for runtime operation in this work package.
11. `AppHost` shall be the only required run and startup path for this work package.
12. `WorkbenchHost` shall not define any explicit Workbench-specific API endpoints in this work package.
13. The initial hosted Workbench experience shall be anonymous only, with no active authentication flow required in this work package.
14. Non-host services and infrastructure projects shall remain in place for future host integration without requiring detailed restructuring in this work package.
15. The initial `WorkbenchHost` UI shall use a minimal layout with a single home page that displays `Hello UKHO Workbench` and no broader shell functionality.
16. Obsolete WebAssembly-specific packages, files, static assets, and configuration shall be removed entirely.
17. The first work package shall not implement runtime module definition loading or module composition.
18. The resulting solution shall build successfully, the remaining tests shall pass, and Aspire startup shall be manually verifiable such that the default Workbench link shows the initial page successfully.
19. The work package shall not require any new automated UI verification for the `/` page beyond the existing build and remaining-test expectations.
20. The work package shall not constrain the `WorkbenchHost` route surface beyond requiring that `/` serves `Hello UKHO Workbench`.

## 5. Non-functional requirements

1. Workbench projects must remain reusable outside this repository's Search-specific context.
2. Dependency direction and actual project references must follow the repository Onion architecture rules for the remaining server-side Workbench structure.
3. The initial implementation should keep the shell intentionally small so later work packages can evolve it incrementally.
4. Naming and folder placement should align with the conventions already used in this repository.
5. The developer startup flow through Aspire should be straightforward and predictable.
6. Removal of the client-side and common Workbench projects should leave no obsolete references, packages, or configuration behind.

## 6. Data model

The initial work package is not expected to introduce a production data model.

No shared common project or manifest contract definitions are required in this phase.

## 7. Interfaces & integration

1. `AppHost` integration
   - `WorkbenchHost` must be registered so it can be launched from Aspire
   - no separate client-side Workbench Aspire resource is required in this work package
   - the default Aspire link must open the hosted Workbench experience at `/`

2. `WorkbenchHost`
   - `WorkbenchHost` must render the interactive server Blazor experience
   - no broader API surface is required beyond the default platform wiring needed for hosting or health
   - no explicit Workbench-specific API endpoints are required in this phase
   - the initial Workbench surface must be reachable directly at `/`
   - the root page only needs to render `Hello UKHO Workbench` via Blazor for this work package
   - obsolete WebAssembly-specific packages, files, static assets, and configuration must be removed

## 8. Observability (logging/metrics/tracing)

The initial work package does not require a rich observability model.

Any minimum logging added for startup or host diagnostics should align with repository logging standards and remain generic to Workbench.

## 9. Security & compliance

1. No Search-specific data or privileged business behavior should be introduced into Workbench foundations.
2. The first work package should avoid unnecessary privileged operations and keep the runtime surface minimal.
3. The initial hosted Workbench experience should remain anonymous only, with authentication and authorization deferred to later work packages.
4. Removal of obsolete projects and WebAssembly artifacts must not leave behind repository-specific secrets, endpoints, or business rules in residual configuration.

## 10. Testing strategy

1. Remove test projects that belong to deleted client-side and common Workbench production projects.
2. Verify the repository solution builds successfully.
3. Verify the remaining tests pass.
4. Verify the Workbench hosted experience is runnable through a manual Aspire startup check.
5. Do not require any new automated UI verification for the `/` page in this work package.

## 11. Rollout / migration

1. Simplify the existing Workbench area by removing obsolete client-side and common projects rather than introducing new parallel Workbench structures.
2. Update the main repository solution so only the remaining Workbench projects and tests participate in normal build and developer workflows.
3. Defer migration of Search functionality into modules to later work packages.
4. Use this work package as the baseline for future incremental shell capability work on the server-hosted Workbench.
5. Treat manual Aspire startup verification through `AppHost` as sufficient rollout validation for the first runnable baseline.

## 12. Open questions

No open questions are currently recorded.
