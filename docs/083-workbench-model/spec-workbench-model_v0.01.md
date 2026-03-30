# Work Package: `083-workbench-model` — Workbench module and tool model

**Target output path:** `docs/083-workbench-model/spec-workbench-model_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created to turn the existing Workbench tool model basis into a repository-specific specification for implementation planning.
- `v0.01` — Consolidates the conceptual model from `docs/workbench-tool-model-specification-basis.md` with the agreed refinements for the first practical Workbench slice.
- `v0.01` — Confirms that the preferred first slice is lightweight-tool-first, with explorer-led discovery, command-centric actions, central hosted tool surfaces, runtime menu contributions, and status bar contributions.
- `v0.01` — Confirms that module and tool accessible contracts and models belong in `UKHO.Workbench`, orchestration services in `UKHO.Workbench.Services`, infrastructure concerns in `UKHO.Workbench.Infrastructure`, and overall composition and UI in `WorkbenchHost`.
- `v0.01` — Confirms that `WorkbenchHost` loads module assemblies dynamically at startup from host-controlled probe roots declared in `modules.json`, before finalizing the DI container.
- `v0.01` — Confirms that initial modules are `UKHO.Workbench.Modules.Search`, `UKHO.Workbench.Modules.PKS`, `UKHO.Workbench.Modules.FileShare`, and `UKHO.Workbench.Modules.Admin`.
- `v0.01` — Confirms that `UKHO.Workbench.Modules.Search` initially contains tools for search ingestion, search query, and ingestion rule editing.
- `v0.01` — Confirms that although `UKHO.Workbench.Modules.Search` is the first intended fully implemented functional module, that full implementation belongs to a later work package and this specification expects dummy or exemplar UI only.
- `v0.01` — Confirms that `UKHO.Workbench.Modules.PKS`, `UKHO.Workbench.Modules.FileShare`, and `UKHO.Workbench.Modules.Admin` initially expose dummy tools to prove the module-loading and shell-composition model.
- `v0.01` — Confirms that the Workbench UI direction is desktop-like rather than web-like and should use the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid layout model with splitters.
- `v0.01` — Confirms that the current Workbench theme is provided by Radzen tooling, and that theme customization is out of scope for this work package unless it is essential to make a feature work; any essential visual adjustment shall be applied at the Razor component level without changing the Radzen theme itself.

## 1. Overview

### 1.1 Purpose

This specification defines the initial formal Workbench model for this repository.

Its purpose is to convert the existing conceptual architecture into a practical, implementation-oriented specification for a modular Workbench that hosts independently authored tools, loads modules dynamically from assemblies, and composes those tools into a shared shell.

The specification is intended to support the next Workbench implementation phases without collapsing the architecture into either:

- a simple host for arbitrary pages, or
- an over-engineered IDE-class platform in the first slice.

### 1.2 Scope

This specification currently includes:

- the conceptual Workbench, module, and tool model for this repository
- the preferred initial architectural slice for lightweight tools
- the shell composition model for menus, explorers, toolbars, central hosted tools, and status bar participation
- the intended desktop-like UI direction for the Workbench shell
- use of the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid layout model with splitter support for Workbench shell layout composition
- the distinction between static and runtime contributions
- the startup discovery and loading model for module assemblies
- the configuration role of `modules.json`
- the project responsibility split across `UKHO.Workbench`, `UKHO.Workbench.Services`, `UKHO.Workbench.Infrastructure`, and `WorkbenchHost`
- the initial set of module assemblies and their first tool responsibilities
- continued use of the current Radzen-provided theme as the baseline visual theme for this work package
- functional and technical requirements needed to move from concept to implementation planning

This specification currently excludes:

- concrete API signatures and code-level interfaces
- detailed dependency injection implementation mechanics
- detailed docking algorithms and advanced tab-group behavior
- detailed persistence formats
- full implementation of the `Search` module tools beyond dummy or exemplar UI used to validate the Workbench model
- final definitions for all future admin, PKS, or FileShare tools
- final visual design details beyond the agreed shell shape and contribution surfaces
- customization or replacement of the Radzen-provided theme, except for narrowly scoped Razor component-level adjustments that are essential to make an agreed feature function
- replacement of the Workbench layout model with generic web-page layout patterns outside the `UKHO.Workbench.Layout` `Layouts` namespace approach

### 1.3 Stakeholders

- Workbench platform developers
- module authors building Workbench-hosted tools
- maintainers of `WorkbenchHost` and Workbench shared libraries
- Search feature teams delivering the first real module implementation
- repository maintainers responsible for architecture consistency and future extensibility

### 1.4 Definitions

- `Workbench`: the host platform that discovers modules, composes the shell, and provides shared services
- `WorkbenchShell`: the runtime shell model responsible for visible regions, hosted tool instances, and composition of shell surfaces
- `module`: an assembly-level packaging and registration boundary that contributes services, tools, and static workbench definitions
- `tool`: the primary hosted application unit that the Workbench opens and integrates into the shell
- `tool instance`: a running hosted copy of a tool within the shell
- `ToolContext`: the bounded runtime contract through which a running tool instance interacts with the Workbench
- `contribution`: a declarative integration element such as an explorer contribution, command contribution, menu contribution, toolbar contribution, or status bar contribution
- `runtime contribution`: a contribution surfaced by the active tool instance only while it is running or focused
- `probe root`: a configured directory from which `WorkbenchHost` may discover module assemblies at startup
- `modules.json`: a host-owned configuration file that declares module probe roots and related startup discovery settings

## 2. System context

### 2.1 Current state

The repository already contains a conceptual Workbench architecture basis in `docs/workbench-tool-model-specification-basis.md`.

That document establishes the key architectural direction:

- the Workbench hosts tools rather than arbitrary unmanaged UI fragments
- tools are the primary plugin and hosted application unit
- the shell owns composition and hosting
- commands are the central action abstraction
- tools integrate through bounded contracts rather than direct shell control

The current conceptual model is intentionally broad and future-facing. It supports both lightweight utilities and substantial hosted applications, but it does not yet define the repository-specific implementation slice, the startup module-loading pattern, or the agreed project responsibilities.

### 2.2 Proposed state

After this work package is adopted as the specification baseline:

- the Workbench is understood as a modular shell that hosts tools contributed from dynamically loaded module assemblies
- the first practical implementation slice is explicitly biased toward lightweight tools rather than full IDE-class behavior
- the first practical implementation slice uses singleton tool hosting in the shell, while still allowing each tool to contain rich internal UI and workflows
- the Workbench UI is intentionally desktop-like rather than conventional web-page-like
- the initial shell supports:
  - a full-width menu bar
  - an activity rail or explorer selector
  - a left explorer area with sections and items
  - a central hosted tool surface
  - a contextual active-view toolbar
  - a status bar
- the shell layout should use the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid model with splitter support as the intended layout foundation
- the current visual theme should continue to be provided by Radzen tooling, and any essential feature-specific visual adjustment should be implemented at the Razor component level without modifying the underlying Radzen theme
- the initial contribution model supports:
  - explorer contributions
  - command contributions
  - static menu contributions
  - static toolbar contributions
  - static status contributions
  - runtime toolbar contributions for the active-view toolbar only
  - runtime menu contributions from the active tool instance
  - runtime status contributions from the active tool instance
  - a small fixed initial context key set for shell-driven visibility and enablement
- `WorkbenchHost` reads a host-owned `modules.json` file at startup
- `modules.json` supports probe roots plus per-module enable or disable control in the first implementation
- `WorkbenchHost` scans configured probe roots for assemblies matching `UKHO.Workbench.Modules.*`
- valid module assemblies are loaded before the host finalizes the service provider so modules can participate in DI registration and Workbench contribution registration
- modules expose bounded startup registration rather than uncontrolled shell access
- module and tool accessible contracts and contribution models live in `UKHO.Workbench`
- orchestration and composition services live in `UKHO.Workbench.Services`
- technical loading, probing, reflection, and persistence adapters live in `UKHO.Workbench.Infrastructure`
- overall application composition, startup wiring, and shell UI live in `WorkbenchHost`
- the first real functional module is `UKHO.Workbench.Modules.Search`
- the `Search` module is the first intended fully implemented module, but that delivery is deferred to a later work package and is represented only by dummy or exemplar UI in the scope of this specification
- `UKHO.Workbench.Modules.PKS`, `UKHO.Workbench.Modules.FileShare`, and `UKHO.Workbench.Modules.Admin` are present initially with dummy tools so the Workbench can prove module discovery and composition before all domain functionality exists
- the initial dummy tool map is `Search ingestion`, `Search query`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration`
- startup diagnostics identify the probe root, assembly path, and startup stage for module discovery and load failures while user-facing notifications remain high level

### 2.3 Assumptions

- the Workbench remains a generic platform and must not be collapsed into a Search-specific shell
- module authors will reference `UKHO.Workbench` for stable contracts and contribution models
- the host must retain control over discovery, loading, shell composition, and approved runtime capabilities
- the first practical shell should feel credible and extensible without requiring full docking, layout persistence, multi-instance policies, or rich inspector behavior immediately
- the shell should feel like a desktop workbench rather than a conventional web application page flow
- runtime menu contributions and runtime status bar contributions are important enough to include in the first slice
- the first implementation should allow runtime toolbar contributions only for the active-view toolbar rather than broader runtime toolbar participation
- the first implementation should use singleton shell-level tool hosting, where reopening a tool focuses the existing hosted instance rather than opening another hosted copy
- the first implementation should use a small fixed context key set rather than a fully generic extensible context publication model
- the first implementation should allow host-controlled per-module enable or disable configuration in `modules.json`, while deferring environment-specific override behavior
- the Search module is the best first real module because it contains several related tools that naturally share shell groupings and domain services
- although the `Search` module is the first intended real module, this work package is limited to Workbench model definition and dummy or exemplar UI rather than full Search tool implementation
- dummy tools are a valid mechanism for proving module loading, tool opening, contribution registration, and runtime composition
- the current Radzen-provided theme is sufficient for this work package and should remain unchanged unless a minimal component-level adjustment is essential to make a feature work
- future richer shell behavior will be added incrementally on top of the bounded concepts defined here rather than by bypassing them

### 2.4 Constraints

- the Workbench must preserve the Workbench-versus-tool boundary from the conceptual basis document
- the shell must own composition and hosting; tools and modules must not take direct uncontrolled ownership of shell internals
- dynamic module loading must occur before DI container finalization if modules are to contribute services to the host container
- `modules.json` must remain host-owned configuration, not a module-owned manifest or arbitrary code-loading mechanism
- `modules.json` shall support per-module enable or disable settings in addition to probe-root configuration for the first implementation
- module discovery must be constrained to approved probe roots and approved assembly matching rules
- module-facing contracts must remain in `UKHO.Workbench` rather than being spread through host-only implementation projects
- the first slice must remain lighter-weight than a full Theia or Visual Studio-style implementation
- the first slice must still support runtime menu and status participation from active tool instances
- the first slice shall support runtime toolbar participation only for the active-view toolbar
- the first slice shall use singleton shell-level tool hosting for each tool definition
- the first slice shall use a small fixed context key set for initial context-driven behavior
- Workbench shell layout composition shall use the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid model with splitter support rather than ad hoc page-style layout patterns
- the current Radzen-provided theme shall not be customized, replaced, or restyled as part of this work package unless a change is essential to make an agreed feature function
- any essential visual customization required to make a feature function shall be implemented locally in Razor components and shall not modify the Radzen theme itself

## 3. Component / service design (high level)

### 3.1 Components

1. `Workbench core contract assembly`
   - `UKHO.Workbench`
   - contains the stable host-module contract surface
   - owns module-facing concepts such as tools, tool instances, tool context, contribution models, command models, explorer models, status and menu contribution models, and module registration contracts

2. `Workbench service layer`
   - `UKHO.Workbench.Services`
   - contains orchestration and composition services for tool registration, command routing, explorer composition, menu composition, status composition, and other Workbench-level service behavior

3. `Workbench infrastructure layer`
   - `UKHO.Workbench.Infrastructure`
   - contains startup probing, configuration reading, reflection-based discovery, assembly loading, persistence adapters, and other technical infrastructure required by the Workbench host

4. `Workbench host and shell`
   - `WorkbenchHost`
   - owns startup, DI composition root, module loading, shell UI composition, and overall Blazor host behavior
   - uses the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid layout model with splitters as the intended shell layout foundation

5. `Module assemblies`
   - assemblies named `UKHO.Workbench.Modules.*`
   - each assembly provides a bounded registration entry point and contributes one or more tools plus static registrations

6. `Tool instances`
   - runtime hosted copies of tools inside the Workbench shell
   - may publish runtime menus, runtime status items, context, and selection while active

### 3.2 Data flows

#### Startup and registration flow

1. `WorkbenchHost` starts.
2. The host reads `modules.json`.
3. The host resolves configured module probe roots.
4. The host scans those probe roots for candidate assemblies matching `UKHO.Workbench.Modules.*`.
5. The host loads valid assemblies.
6. The host discovers a bounded module registration entry in each valid assembly.
7. Each module registers services and Workbench contributions through the approved registration contract.
8. The host finalizes the DI container only after module registration is complete.
9. The shell composes explorers, commands, menus, toolbars, and status surfaces from registered contributions.

#### Tool activation flow

1. A user interacts with an explorer item, menu entry, toolbar action, or other shell surface.
2. The interaction resolves to a command or activation target.
3. The shell opens or focuses the relevant tool.
4. A tool instance is created if required.
5. The tool instance receives a bounded `ToolContext`.
6. The tool instance renders its own UI within the central hosted surface.
7. While active, the tool instance may publish runtime menu contributions, runtime status contributions, selection, and context.
8. The Workbench updates visible menus, toolbar state, and status bar composition accordingly.

### 3.3 Key decisions

- the Workbench remains tool-centric rather than view-centric
- the first implementation slice is intentionally biased toward lightweight tools
- the first implementation slice targets a desktop-like shell experience rather than a web-page-centric experience
- explorer-led discovery and command-centric behavior are part of the first slice
- runtime toolbar contributions are included only for the active-view toolbar in the first implementation
- runtime menu contributions and runtime status bar contributions are included from the start
- the initial context model is intentionally small and fixed for the first implementation
- the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid layout model with splitters is the intended foundation for shell layout composition
- the current Radzen-provided theme remains the baseline theme for this work package, and visual refinement should happen only through minimal Razor component-level adjustments when essential for feature delivery
- `modules.json` defines module probe roots rather than brittle lists of exact assembly files
- module loading is a host responsibility and occurs before final DI finalization
- the first shell-level hosting rule is singleton per tool definition, while preserving the separate `Tool` and `ToolInstance` concepts for future extensibility
- `UKHO.Workbench.Modules.Search` is the first real functional module
- `PKS`, `FileShare`, and `Admin` modules exist initially to prove modular composition even when their real tools are deferred

## 4. Functional requirements

1. The Workbench shall host tools as the primary plugin and runtime unit.
2. The Workbench shall preserve the distinction between a static tool definition and a runtime tool instance.
3. The Workbench shell shall own composition and hosting of all shell regions.
4. Tools shall interact with the Workbench through bounded contracts rather than direct shell control.
5. The initial shell shall include a full-width menu bar above all other content.
6. The initial shell shall include an activity rail or explorer selector.
7. The initial shell shall include a left explorer area capable of rendering explorer sections and explorer items.
8. The initial shell shall include a central hosted tool surface for the active tool instance.
9. The initial shell shall include an active-view toolbar surface.
10. The initial shell shall include a status bar surface.
11. The initial Workbench UI shall be desktop-like rather than web-like.
12. Workbench shell layout composition shall use the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid model with splitter support as the intended layout foundation.
13. The initial Workbench implementation shall retain the current Radzen-provided theme as the baseline theme for this work package.
14. Theme customization or replacement shall be out of scope for this work package unless a change is essential to make an agreed feature function.
15. Any essential visual adjustment required to make a feature function shall be implemented at the Razor component level and shall not modify the Radzen theme itself.
16. The initial slice shall support explorer contributions.
17. The initial slice shall support command contributions.
18. The initial slice shall support static menu contributions.
19. The initial slice shall support static toolbar contributions.
20. The initial slice shall support static status bar contributions.
21. The initial slice shall support runtime toolbar contributions from the active tool instance only for the active-view toolbar surface.
22. The initial slice shall support runtime menu contributions from the active tool instance.
23. The initial slice shall support runtime status bar contributions from the active tool instance.
24. Explorer items shall activate Workbench behavior through declarative activation targets rather than direct UI instantiation.
25. Commands shall be the primary action abstraction across menu, explorer, toolbar, and other shell surfaces.
26. The Workbench shall distinguish conceptually between host commands and tool commands.
27. The Workbench shall provide a bounded `ToolContext` for runtime tool-shell interaction.
28. `ToolContext` shall support opening or focusing another tool through approved host capabilities.
29. `ToolContext` shall support invoking commands through approved host capabilities.
30. `ToolContext` shall support publishing or querying selection through approved host capabilities.
31. `ToolContext` shall support publishing context values through approved host capabilities.
32. `ToolContext` shall support updating title, icon, or badge state for the current tool instance.
33. `ToolContext` shall support notifications and dialogs through approved host capabilities.
34. The Workbench shall support selection and context as first-class concepts for shell behavior.
35. The first implementation shall use a small fixed context key set for context-driven visibility and enablement.
36. The initial fixed context key set shall include shell-defined keys such as `activeTool`, `activeExplorer`, `selectionType`, `selectionCount`, and `activeRegion`, or clear equivalents.
37. The first implementation shall not require a fully generic arbitrary context publication model.
38. The Workbench shall support static registrations at module registration time.
39. The Workbench shall support runtime contributions from the active tool instance while it is active.
40. `WorkbenchHost` shall load module assemblies dynamically at startup.
41. Dynamic module loading shall be driven by a host-owned `modules.json` configuration file.
42. `modules.json` shall define module probe roots rather than requiring exact assembly-file enumeration.
43. `WorkbenchHost` shall scan configured probe roots for assemblies matching `UKHO.Workbench.Modules.*`.
44. Only assemblies discovered from approved probe roots and matching approved module conventions shall be eligible for module loading.
45. The host shall load modules before finalizing the DI container.
46. Loaded modules shall be able to register services into the host DI container through a bounded startup contract.
47. Loaded modules shall be able to register Workbench contributions through a bounded startup contract.
48. The system shall separate module startup registration from runtime tool participation.
49. Each module assembly shall provide one bounded module registration entry point suitable for startup discovery.
50. `UKHO.Workbench.Modules.Search` shall initially contribute three tools: search ingestion, search query, and ingestion rule editing.
51. `UKHO.Workbench.Modules.PKS` shall initially contribute the dummy `PKS operations` tool.
52. `UKHO.Workbench.Modules.FileShare` shall initially contribute the dummy `File Share workspace` tool.
53. `UKHO.Workbench.Modules.Admin` shall initially contribute the dummy `Administration` tool.
54. Dummy tools shall still be openable and composable so module discovery and shell behavior can be verified before final domain implementation exists.
55. The Workbench shall treat `module` as the packaging and registration boundary and `tool` as the hosted application unit.
56. The Workbench shall support a single module contributing multiple tools.
57. The Workbench shall permit future capability-based behavior so the host can adapt to what a given tool supports.
58. The initial slice shall prefer fixed shell regions over full docking and freeform panel movement.
59. The first slice shall not require full multi-instance policies, full layout persistence, right-side inspector composition, or advanced docking behavior.
60. The first implementation shall use singleton shell-level hosting for each tool definition.
61. Reopening a singleton tool shall focus the existing hosted tool instance rather than creating an additional hosted shell instance.
62. Singleton shell-level hosting shall not restrict a tool from containing multiple internal views, panes, tabs, or workflows within its own hosted UI.

## 5. Non-functional requirements

1. The Workbench architecture shall remain understandable to both Workbench platform developers and module authors.
2. Module-facing contracts shall be stable and intentionally bounded.
3. The startup loading model shall support incremental addition of new module assemblies without redesign of the shell model.
4. The first slice shall optimize for maintainability and incremental growth rather than maximum initial shell complexity.
5. The Workbench shall preserve clean separation between host concerns and tool concerns.
6. Module discovery and loading shall remain host-controlled to reduce accidental or unsafe extensibility.
7. The Workbench shall support efficient startup by allowing tools themselves to remain dormant until opened or otherwise activated.
8. The specification shall remain compatible with later growth into richer context evaluation, broader runtime contributions, and more advanced tool capabilities.
9. The Workbench shall remain generic and reusable rather than Search-specific.
10. Dummy tools and placeholder modules shall be allowed in early phases provided they still participate through the same bounded contracts as real tools.
11. The Workbench UI shall optimize for a desktop-like interaction model rather than a conventional web-page flow.
12. Layout composition shall remain aligned to the `UKHO.Workbench.Layout` `Layouts` namespace WPF-like grid model with splitters so future shell work builds on a consistent layout foundation.
13. The current Radzen-provided theme shall remain the baseline theme for this work package so shell delivery can focus on composition and hosted-tool behavior rather than theme work.
14. Any visual adjustment that is essential to make a feature function shall be localized to the relevant Razor component and shall not modify the Radzen theme itself.

## 6. Data model

### 6.1 Core conceptual entities

#### Workbench

The top-level host platform responsible for:

- discovery
- composition
- layout ownership
- shared services
- shell mediation

#### WorkbenchShell

The runtime composition model responsible for:

- active explorer
- active tool instance
- hosted tool surfaces
- visible shell regions
- menu, toolbar, and status composition

#### Module

The assembly-level packaging and registration unit responsible for:

- participating in startup discovery
- registering services
- registering tools
- registering static contributions

#### Tool

The static hosted application definition responsible for:

- metadata
- hosted entry point
- static contribution ownership
- capability declaration

#### ToolInstance

The runtime hosted copy of a tool responsible for:

- current identity
- current input or activation payload
- current title, icon, and badge state
- current runtime menu contributions
- current runtime status contributions
- current selection and context publication

#### ToolContext

The bounded runtime host-tool contract responsible for:

- approved shell interaction
- command invocation
- notifications and dialogs
- context and selection publication
- tool-to-tool opening requests

#### InitialContextModel

The initial shell context model responsible for:

- exposing a small fixed set of shell-defined context keys
- supporting context-driven visibility and enablement for early command, menu, and toolbar behavior
- avoiding a fully generic arbitrary key publication model in the first implementation
- leaving room for broader extensibility in later phases

### 6.2 Contribution entities

The Workbench model shall support at least the following contribution entities:

- `ExplorerContribution`
- `ExplorerSectionContribution`
- `ExplorerItem`
- `ActivationTarget`
- `CommandContribution`
- `MenuContribution`
- `ToolbarContribution`
- `StatusBarContribution`
- `SettingsContribution`

### 6.3 Module configuration entity

`modules.json` shall conceptually contain:

- one or more probe-root entries
- per-module enable or disable flags
- optional startup ordering or activation hints where justified later
- no required environment-specific override structure in the first implementation

The specification does not require a concrete JSON schema in this work package.

## 7. Interfaces & integration

### 7.1 Project responsibility split

#### `UKHO.Workbench`

This project shall contain:

- module-facing contracts
- tool and contribution models
- module registration contracts
- tool context contracts
- stable shell vocabulary required by module authors

This project shall not become a dumping ground for host implementation code.

#### `UKHO.Workbench.Services`

This project shall contain:

- Workbench orchestration services
- composition services
- registry and manager implementations
- application-level command, explorer, menu, and status orchestration services

Interfaces that module authors must reference shall live in `UKHO.Workbench` rather than only in this project.

#### `UKHO.Workbench.Infrastructure`

This project shall contain:

- `modules.json` reading
- probe-root resolution
- reflection-based module discovery
- assembly loading
- persistence adapters
- other technical infrastructure needed by the host

#### `WorkbenchHost`

This project shall contain:

- application startup
- DI composition root
- module loading orchestration
- shell UI composition
- Blazor host behavior
- host-owned shell policies
- use of the existing Radzen-provided theme without modifying the theme itself as part of this work package

### 7.2 Dependency direction

The intended dependency shape is:

- `WorkbenchHost -> UKHO.Workbench.Infrastructure`
- `WorkbenchHost -> UKHO.Workbench.Services`
- `UKHO.Workbench.Infrastructure -> UKHO.Workbench`
- `UKHO.Workbench.Services -> UKHO.Workbench`
- `UKHO.Workbench.Modules.* -> UKHO.Workbench`

Modules should not require direct access to `WorkbenchHost` internals.

### 7.3 Initial module map

The initial module map is:

- `UKHO.Workbench.Modules.Search`
- `UKHO.Workbench.Modules.PKS`
- `UKHO.Workbench.Modules.FileShare`
- `UKHO.Workbench.Modules.Admin`

The intended initial responsibilities are:

- `Search`: real first functional module containing search ingestion, search query, and ingestion rule editing tools
- `Search`: first intended fully implemented functional module, but represented only by dummy or exemplar UI in the scope of this specification
- `PKS`: placeholder module with dummy tools
- `FileShare`: placeholder module with dummy tools
- `Admin`: placeholder module with common admin tools, initially dummy

## 8. Observability (logging/metrics/tracing)

1. Module discovery, probe-root scanning, assembly loading, and module registration shall be observable through host logging.
2. Module load failures shall be surfaced with enough diagnostic detail to identify the probe root, assembly, and failure stage.
3. Tool opening and activation failures shall be observable through host logging and user-facing notification where appropriate.
4. Runtime menu and runtime status contribution composition shall be diagnosable enough to understand active-tool effects on shell state.
5. The specification does not require a final metrics or tracing schema in this work package, but future implementation should preserve the ability to add startup and activation instrumentation.

## 9. Security & compliance

1. Module loading shall remain host-controlled and constrained to approved probe roots.
2. The Workbench shall not allow modules or tools unrestricted shell access through `ToolContext`.
3. `ToolContext` shall expose only intentionally approved capabilities.
4. Runtime contribution mechanisms shall not be treated as shell back doors.
5. The Workbench shall distinguish between approved host capabilities and arbitrary runtime control.
6. This specification does not define final sandboxing or permissions, but the architecture shall not preclude future permission controls.

## 10. Testing strategy

1. The implementation derived from this specification shall verify startup discovery and loading across multiple module assemblies.
2. The implementation shall verify that modules can register services before host DI finalization.
3. The implementation shall verify static composition of explorer, command, menu, toolbar, and status contributions.
4. The implementation shall verify that active tool instances can contribute runtime menu entries and runtime status items.
5. The implementation shall verify that dummy tools can be discovered, opened, and hosted using the same path as real tools.
6. The implementation shall verify that `Search` module tools appear as distinct hosted tools within the shell.
7. The implementation shall verify that invalid module assemblies or invalid startup registrations fail in a diagnosable way.
8. The implementation shall verify that host-project responsibility boundaries are preserved and that module-facing contracts remain in `UKHO.Workbench`.

## 11. Rollout / migration

1. The Workbench should adopt this model incrementally rather than attempting full future-state implementation in one work package.
2. Early implementation should first establish the host-owned module startup model and the bounded contract surface.
3. The next implementation increments should then layer on shell composition, explorer-led discovery, tool hosting, and runtime menu and status participation.
4. Placeholder modules and dummy tools should be retained until the equivalent real domain tools are ready.
5. Future richer features such as advanced context grammar, runtime toolbar expansion, persistence, inspector regions, and broader capability models should build on this baseline rather than replacing it.

## 12. Open questions

No open questions are currently recorded.
