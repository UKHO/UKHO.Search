# Work Package: `057-studio-shell` — Eclipse Theia studio shell

**Target output path:** `dev/work-packages/mvp/057-studio-shell/spec-domain-studio-shell_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for establishing the first Eclipse Theia-based studio shell for the repository.

The intent is to introduce a new Theia application shell under `src/Studio/Server`, scaffold an initial native Theia extension named `search-studio`, and orchestrate the shell through Aspire from `src/Hosts/AppHost` in `RunMode.Services`.

This package establishes only the shell foundation. Migrating or re-implementing existing tooling inside Theia is explicitly out of scope for this work package.

### 1.2 Scope

This specification covers:

- creating a Theia-based application shell in `src/Studio/Server`
- using the Theia application composition approach described in the official Theia guidance
- generating an initial native Theia extension named `search-studio`
- ensuring the generated extension and browser application build successfully
- integrating the Theia application into Aspire using the JavaScript hosting integration
- placing the Theia application resource into the `RunMode.Services` branch of `AppHost`
- documenting architecture, build, validation, and rollout expectations for the initial shell

This specification does not cover:

- migration of any existing repository functionality into Theia
- feature parity with existing tools such as `RulesWorkbench`, `FileShareEmulator`, or current studio APIs
- production branding, packaging, or desktop distribution beyond what is needed to validate the shell foundation
- authentication, authorization, or fine-grained user workflow design inside the studio shell

### 1.3 Stakeholders

- studio/tooling developers
- repository maintainers for `AppHost`
- developers who will later migrate existing tooling into Theia
- engineering leads defining the future studio architecture
- DevEx/platform maintainers responsible for local orchestration and build reliability

### 1.4 Definitions

- Theia application composition: building a custom Theia product by composing Theia extensions into an application, rather than building only a standalone extension
- native Theia extension: a Theia-specific extension authored against Theia extension APIs rather than the VS Code extension model
- shell application: the initial minimal browser-hosted studio frame that can start, render, and host future extensions
- `search-studio`: the initial custom extension package contributed to the shell in this work package
- Aspire JavaScript hosting integration: the `Aspire.Hosting.JavaScript` integration used to run JavaScript applications from `AppHost`

## 2. System context

### 2.1 Current state

The repository currently contains:

- an Aspire `AppHost` at `src/Hosts/AppHost/AppHost.cs`
- an existing `RunMode.Services` branch that orchestrates the service landscape
- a placeholder `.NET` project at `src/Studio/StudioHost`
- an existing `src/Studio/Server` directory that is currently empty

There is no Theia application currently present in the repository.

The current `AppHost` service orchestration includes a `StudioHost` project resource, but it does not yet orchestrate a JavaScript-based Theia application.

### 2.2 Proposed state

The repository will gain a browser-hosted Eclipse Theia application under `src/Studio/Server`, created using the official Theia application composition approach.

The initial design is an application-composition problem, not a VS Code extension-only problem. The solution will therefore use:

1. a custom Theia application shell
2. a native Theia extension named `search-studio`
3. a browser-oriented startup flow suitable for orchestration from Aspire

The initial shell will be added to `AppHost` in `RunMode.Services` using the Aspire JavaScript hosting integration. The Aspire resource will run the Theia browser application using the shell's JavaScript workspace scripts and expose an HTTP endpoint for local development.

### 2.3 Assumptions

- this work should be implemented as a Theia application with a native Theia extension, not as a standalone VS Code extension, because the requirement is to build a custom studio shell
- the initial delivery is browser-hosted and locally orchestrated through Aspire
- the generated shell will follow the official Theia guidance for composing applications and generated examples rather than inventing a custom structure from scratch
- the initial extension may remain functionally minimal as long as it is correctly scaffolded, included in the application, and buildable
- existing functionality migration will be handled in later work packages
- the current `StudioHost` project will remain untouched in this work package because it is intended to host APIs for later Theia integration work
- the initial delivery target is browser-hosted only; Electron support is deferred to later work unless explicitly requested
- Aspire startup should follow the official `Aspire.Hosting.JavaScript` integration guidance and use its recommended JavaScript application orchestration approach
- the Theia workspace should use the generator's default package-management approach unless there is a documented reason to diverge
- the initial `search-studio` extension may retain the generated sample contribution temporarily as a smoke test, provided it is correctly renamed and wired into the shell
- mandatory validation for this work package is build success only; runtime debugging can be handled afterwards if needed
- the initial shell should include a light UX baseline, such as a simple welcome-style contribution in `search-studio`, rather than remaining a pure uncustomized scaffold
- internal package and extension naming should remain `search-studio`, while user-facing shell naming should use `UKHO Search Studio`
- the initial shell should open with the standard Theia workbench layout plus a lightweight `UKHO Search Studio` welcome-style view or panel
- the welcome contribution should include a simple command action that proves the custom extension wiring, rather than being purely informational
- the initial shell should not bundle any VS Code extensions in this work package; those can be considered later
- the initial shell should use a fixed local HTTP port configured in `AppHost` `appsettings.json` and read consistently with other Aspire configuration items already stored there
- the generated workspace should prefer a browser-only package set, unless retaining generator-produced Electron scaffolding avoids unnecessary churn or build issues
- the work package should reserve a placeholder configuration hook for the future `StudioHost` API connection without surfacing that future integration in the initial welcome contribution
- the initial dependency baseline should remain at the generator's minimal default Theia package set, with broader shell capability deferred to later work packages
- the initial welcome action should be lightly renamed to fit `UKHO Search Studio` terminology while remaining as minimal as the generated sample behavior
- the fixed local Aspire port for the initial shell should be `3000`
- developer-facing run guidance should remain in the specification only for this work package, with no separate repository `README` required yet
- the `AppHost` configuration key for the shell port should be `Studio:Server:Port`

### 2.4 Constraints

- migrating existing functionality is out of scope for this work package
- the shell must be rooted in `src/Studio/Server`
- the initial extension name must be `search-studio`
- the implementation approach must follow the official Theia application composition guidance
- the Aspire integration must use the JavaScript hosting integration and must be placed in the `RunMode.Services` branch
- the extension and browser shell must build successfully before the work package is considered complete

## 3. Component / service design (high level)

### 3.1 Components

This work package introduces or updates the following logical components:

1. Theia application workspace
   - location: `src/Studio/Server`
   - purpose: hold the JavaScript monorepo/workspace for the composed Theia application
   - expected contents: root workspace configuration, browser application package, shared scripts, and generated extension package

2. Browser application package
   - purpose: define the runnable browser-hosted Theia application
   - expected responsibility: compose built-in Theia packages together with the `search-studio` extension and expose a local web endpoint

3. `search-studio` native Theia extension
   - purpose: provide the first custom extension package bundled into the shell
   - expected responsibility: supply the initial extension contribution used to validate the custom studio extension path

4. Aspire integration in `AppHost`
   - purpose: orchestrate the Theia shell as a JavaScript application in local development
   - expected responsibility: add the required NuGet package, register the Theia application resource, configure its startup command/script, and place it in `RunMode.Services`

5. Future studio backend relationship
   - purpose: define how the new Theia shell will eventually interact with existing or future `.NET` studio services
   - current state for this work package: deferred, except where needed to keep local orchestration coherent

### 3.2 Data flows

#### Local startup flow

1. `AppHost` starts in `RunMode.Services`
2. `AppHost` orchestrates the JavaScript-based Theia shell resource using Aspire JavaScript hosting
3. Aspire runs the configured shell script from `src/Studio/Server`
4. the Theia browser application starts its backend/frontend process pair as defined by the generated shell
5. a browser accesses the exposed local Theia endpoint
6. the Theia application loads the built-in extensions and the bundled `search-studio` extension

#### Build validation flow

1. JavaScript dependencies are restored for the Theia workspace
2. the `search-studio` extension is built as part of the workspace build
3. the browser application bundle is produced successfully
4. Aspire can start the resource using the configured script without manual patching after generation

### 3.3 Key decisions

- **Extension model:** native Theia extension
  - rationale: the requirement is to create a composed Theia product and a first bundled custom extension, which aligns with the official Theia composition and authoring guidance

- **Solution type:** application composition rather than extension-only delivery
  - rationale: the work is to create a new shell application, not only a reusable editor plug-in

- **Initial hosting mode:** browser-hosted shell orchestrated by Aspire
  - rationale: the request explicitly places the shell in `RunMode.Services` and references Aspire JavaScript hosting

- **Runtime target:** browser only for this work package
  - rationale: the initial shell is intended to provide a minimal local browser-hosted baseline, while desktop packaging is deferred

- **Aspire startup contract:** follow the official `Aspire.Hosting.JavaScript` guidance
  - rationale: the shell should use the documented JavaScript app integration pattern rather than a bespoke orchestration contract in this work package

- **Package management:** accept the generator default
  - rationale: this keeps the first shell aligned with current generated Theia composition output and reduces unnecessary setup divergence in the baseline work package

- **Initial extension behavior:** keep a generated sample contribution temporarily for smoke-test validation
  - rationale: this provides a low-cost proof that the custom extension is bundled and active before later work replaces it with studio-specific behavior

- **Validation threshold:** build only
  - rationale: the work package baseline is to establish a compilable shell foundation first, with runtime debugging and investigation to follow if needed

- **Initial shell customization:** light UX baseline
  - rationale: the first shell should provide a small amount of studio identity and a visible custom contribution without taking on migration of existing tooling

- **Naming model:** keep `search-studio` internally and use `UKHO Search Studio` user-facing
  - rationale: this preserves a concise technical package name while presenting the intended product identity in the UI and documentation

- **Default opening experience:** standard layout plus a lightweight welcome view or panel
  - rationale: this keeps the familiar Theia workbench intact while making the custom studio contribution immediately visible

- **Welcome contribution behavior:** include a simple command action
  - rationale: this provides a visible, low-risk proof that the custom extension contributes executable UI behavior without introducing real tooling workflows

- **Bundled extension scope:** native Theia packages plus `search-studio` only
  - rationale: the first shell should stay minimal and avoid bringing in VS Code extension dependencies before later work packages define the desired baseline

- **Port configuration:** fixed local port defined in `AppHost` configuration
  - rationale: the shell should align with existing Aspire configuration patterns in the repository by storing the port in `AppHost` `appsettings.json` and reading it like other local orchestration settings

- **Workspace package scope:** prefer browser-only, but tolerate retained Electron scaffolding if generator constraints make removal problematic
  - rationale: the intended runtime is browser-only, but the initial baseline should avoid avoidable churn if the generated structure is easier to keep stable with unused Electron artifacts present

- **Future API readiness:** add a placeholder configuration hook for later `StudioHost` integration
  - rationale: this keeps the shell intentionally forward-looking by reserving a clear future integration point without taking on API wiring or extra UI complexity in the current work package

- **Theia dependency baseline:** keep the generator's minimal default set
  - rationale: the first work package is intended to establish a stable shell foundation only, with broader IDE-style capabilities deferred to later work packages

- **Welcome action naming:** lightly rename the generated sample behavior to fit `UKHO Search Studio`
  - rationale: this keeps the first custom interaction minimal while aligning visible shell language with the intended product identity

- **Initial local port:** `3000`
  - rationale: a fixed, explicit local port keeps the first shell predictable for local orchestration and aligns with the chosen `AppHost` configuration pattern

- **Developer guidance location:** specification only
  - rationale: this keeps the first shell work package lightweight and avoids extra repository documentation artifacts before the shell stabilizes

- **Port configuration key:** `Studio:Server:Port`
  - rationale: this keeps the shell port aligned with the selected repository configuration naming pattern under the existing studio/server hierarchy

- **Closest built-in pattern:** generated Theia browser application plus generated example extension
  - rationale: official Theia composition guidance recommends starting from the Yeoman-generated application and adapting the generated example extension

- **Migration scope:** deferred
  - rationale: user instruction explicitly excludes migration of existing functionality from this work package

## 4. Functional requirements

### FR-001 Create Theia shell workspace

The solution shall contain a Theia application workspace rooted at `src/Studio/Server`.

### FR-002 Use official Theia composition approach

The shell workspace shall be created using the official Theia application composition approach described in the current Theia documentation, using the installed `generator-theia-extension` scaffolding workflow as the starting point.

### FR-003 Create initial custom extension

The shell workspace shall include an initial native Theia extension package named `search-studio`.

### FR-004 Bundle extension into shell

The browser application shall bundle the `search-studio` extension so that the custom extension is part of the running shell.

### FR-005 Browser application startup

The shell shall provide a browser-hosted startup path suitable for local development and Aspire orchestration.

### FR-006 Successful extension build

The generated `search-studio` extension shall build successfully as part of the workspace build.

### FR-007 Successful browser shell build

The Theia browser application shall build successfully from the generated workspace scripts.

### FR-008 Aspire JavaScript integration package

`src/Hosts/AppHost/AppHost.csproj` shall add the `Aspire.Hosting.JavaScript` package required for JavaScript application orchestration.

### FR-009 Register Theia shell in `RunMode.Services`

`src/Hosts/AppHost/AppHost.cs` shall register the Theia shell resource in the `RunMode.Services` branch.

### FR-010 JavaScript app resource configuration

The Aspire resource shall target the Theia workspace in `src/Studio/Server` and shall use the appropriate shell script configuration for local start-up.

The selected startup configuration shall follow the documented `Aspire.Hosting.JavaScript` integration pattern for JavaScript applications.

### FR-011 HTTP endpoint exposure

The Aspire resource shall expose an HTTP endpoint suitable for local browser access to the Theia shell.

### FR-012 Keep migration work out of scope

This work package shall not migrate existing tool functionality into the new Theia shell.

### FR-013 Minimal runnable shell

The delivered shell shall be considered functionally sufficient for this work package when it starts successfully, renders in the browser, and loads the bundled `search-studio` extension.

### FR-014 Preserve future extensibility

The generated structure shall remain compatible with adding further Theia extensions and later tool migration work packages.

### FR-015 Browser-only delivery

This work package shall deliver a browser-hosted Theia shell only.

Electron packaging and execution shall be out of scope for this work package.

### FR-016 Temporary sample contribution allowed

The initial `search-studio` extension may retain a generated sample contribution as a temporary smoke-test mechanism for this work package.

That contribution shall be treated as scaffolding only and shall not be considered migrated business functionality.

### FR-017 Light UX baseline

The initial shell shall include a light UX baseline through the `search-studio` extension, such as a simple welcome-oriented or similarly minimal visible contribution.

That UX baseline shall remain lightweight and shall not introduce migrated tooling workflows in this work package.

### FR-018 Naming convention

The initial shell shall use `search-studio` as the internal extension or package naming convention.

User-facing shell naming in the UI and specification shall use `UKHO Search Studio`.

### FR-019 Default opening experience

The initial shell shall open to the standard Theia workbench layout.

The `search-studio` extension shall add a lightweight `UKHO Search Studio` welcome-oriented view or panel as part of the initial visible experience.

### FR-020 Welcome action

The initial welcome-oriented contribution shall include a simple command action or button that demonstrates the `search-studio` extension is wired and active.

That action shall remain lightweight and shall not implement migrated business functionality.

The action should be lightly renamed to fit `UKHO Search Studio` terminology rather than using unchanged generated sample wording.

### FR-021 No bundled VS Code extensions

The initial shell shall not bundle any VS Code extensions in this work package.

Only the selected native Theia packages and the custom `search-studio` extension shall be included in the initial baseline unless a later work package states otherwise.

### FR-022 Fixed port from `AppHost` configuration

The initial shell shall use a fixed local HTTP port for Aspire orchestration.

That port shall be stored in `src/Hosts/AppHost/appsettings.json` and read in the same manner as other Aspire configuration items already managed there.

The initial fixed port shall be `3000`.

The configuration key name shall be `Studio:Server:Port`.

### FR-023 Browser-only package baseline with pragmatic generator tolerance

The initial shell shall prefer a browser-only package baseline for this work package.

If the generated Theia workspace includes Electron-related scaffolding and removing it would introduce avoidable churn or build issues, that scaffolding may remain present but unused.

### FR-024 Placeholder future API hook

The initial shell may include a placeholder configuration hook for a future `StudioHost` API connection.

That hook shall not implement active API integration in this work package.

The welcome-oriented contribution shall remain simple and shall not surface the planned future integration in this work package.

### FR-025 Minimal generated dependency baseline

The initial shell shall keep the generator's minimal default Theia dependency set for this work package.

Additional curated or IDE-like dependency expansion shall be deferred to a later work package unless needed to complete the minimal shell baseline.

### FR-026 Developer run guidance in spec only

Developer-facing run and setup guidance for the initial shell shall remain in this specification for the current work package.

No separate repository `README` or broader documentation update is required for this work package unless later work explicitly asks for it.

## 5. Non-functional requirements

### NFR-001 Documentation authority

The design and implementation shall be based on current official Theia and Aspire documentation, with Theia official documentation taking precedence for Theia composition decisions.

### NFR-002 Deterministic local build

The shell shall build using committed workspace scripts without requiring undocumented manual steps after repository checkout.

### NFR-003 Developer onboarding clarity

The generated workspace structure and scripts shall be understandable to repository developers without prior Theia-specific repository knowledge.

### NFR-004 Local orchestration compatibility

The Theia shell shall run as part of the existing local Aspire service orchestration without breaking unrelated `RunMode.Services` resources.

### NFR-005 Minimal initial complexity

The initial shell shall avoid unnecessary feature additions beyond those required to establish a valid, extensible baseline.

## 6. Data model

No domain data model changes are required for this work package.

The work package introduces build and orchestration configuration rather than new business entities.

## 7. Interfaces & integration

### 7.1 Theia workspace interface

The Theia workspace will expose package scripts used for local build and run operations.

The workspace package manager should remain the generator default for this initial shell unless an implementation-time incompatibility requires a documented deviation.

The exact script names should align with the generated application structure, but the expected outcome is:

- a browser build script
- a browser start script
- any supporting prepare/rebuild scripts required by the generated Theia workspace

### 7.2 Aspire integration

The `AppHost` integration will use the Aspire JavaScript hosting APIs for a JavaScript application resource.

The integration should:

- point at the Theia workspace root that contains `package.json`
- select the appropriate run script for the browser-hosted shell
- expose an HTTP endpoint for browser access
- read the configured fixed port from `AppHost` configuration using the existing local Aspire configuration pattern
- keep the resource inside `RunMode.Services`

### 7.3 Studio backend relationship

The current repository contains `src/Studio/StudioHost`, which is reserved for hosting APIs in a later work package.

For this work package, the Theia shell shall be added as a separate application and `StudioHost` shall remain unchanged.

## 8. Observability (logging/metrics/tracing)

This work package does not require custom observability features beyond what the generated shell and Aspire provide by default.

The minimum expectation is that:

- startup failures are visible through the JavaScript application logs surfaced by Aspire
- build failures are visible through standard workspace build output
- local startup state is visible from Aspire resource status

## 9. Security & compliance

This work package does not introduce user-facing security features beyond the local development shell baseline.

Security expectations for this package are:

- do not introduce secrets into committed Theia workspace files
- keep local development configuration aligned with existing repository practices
- defer authentication, authorization, and external exposure design to later work packages unless required for the shell to start locally

## 10. Testing strategy

The work package shall be validated through build and startup verification rather than feature-rich functional tests.

For this work package, the mandatory acceptance threshold is successful build validation.

Runtime debugging may be performed afterwards if needed, but it is not part of the minimum completion gate for the specification baseline.

Minimum validation shall include:

1. JavaScript dependency restore succeeds for the Theia workspace
2. the generated extension builds successfully
3. the browser application build succeeds

If a generated sample contribution is retained temporarily, a smoke check of that contribution may be used to confirm the extension wiring until later work replaces it with repository-specific behavior.

## 11. Rollout / migration

This work package establishes the platform shell only.

Rollout expectations:

1. add the Theia shell workspace under `src/Studio/Server`
2. add Aspire JavaScript hosting support to `AppHost`
3. validate local browser startup through Aspire
4. use subsequent work packages to migrate existing tooling into the shell incrementally

Migration of existing repository tools is explicitly deferred.

## 12. Open questions

No further open questions are currently recorded.
