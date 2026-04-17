# Copilot Instructions (High-Level)

You are an agent. Continue working until queries are fully resolved.  
Be concise but complete. Prefer current research (Microsoft Learn) for Microsoft technologies.

## Quick Principles
- Verify each command succeeds before proceeding; run commands sequentially.
- Prefer latest C#/.NET features; async/await; nullable reference types.
- Always use block-scoped namespaces (e.g., `namespace X.Y { ... }`) rather than file-scoped namespaces when creating or updating C# files in this workspace.
- Use Allman braces style for C# code.
- Add `//` comments on their own line for non-obvious logic.
- Do not interact with git (no branch creation, no git commands) unless explicitly requested.
- When adding or modifying code in this repo, always follow `.github/instructions/coding-standards.instructions.md`: Allman braces, block-scoped namespaces, one public type per file, and underscore-prefixed private fields. Double-check new files for these conventions before finishing.
- In this workspace/PowerShell environment, do not use the `rg` (ripgrep) command; assume it isn't available.
- Avoid clutter in the repository root by placing per-project config files alongside the relevant test or project directories when practical.
- Do not run Stryker again in this workspace, and remove all Stryker-related configuration/setup files when asked.
- Ask clarification questions one at a time rather than batching multiple questions together.
- Never write log files or other temporary files to the repo root; always use suitable temporary storage instead.
- Avoid getting stuck repeating the same status line; continue implementation progress directly after identifying a needed follow-up fix.

## Documentation Workflow (Summary)
- For each new Work Package/piece of work: create a new numbered folder under `./docs/` named `xxx-<descriptor>` (e.g. `001-Initial-Shell`).
- Store ALL related documents (specs, plans, architecture notes, etc.) together inside that Work Package folder.
- Do not overwrite prior work packages; create the next incremental folder (e.g. `002-...`).
- When asked to create specification documents for a work package, create only one document containing everything needed; do not split across multiple documents. If multiple were created, merge into one and delete the extras.
- Use appropriate prompt family & phase from `.github/prompts/`.
- When asking open questions from a spec, record each answer directly in that same spec file and do not create a new version.
- When collaborating on specifications in this repository, do not repeat the draft spec in chat before clarification questions; ask the next question directly and keep the evolving draft in the spec file instead. If remaining questions are about look and feel only, use sensible defaults and revisit later instead of continuing to ask those presentation questions.
- When documentation references repository wiki pages, prefer proper markdown links rather than inline code-formatted URLs or plain page names.
- Repository documentation standards should be captured in `.github/instructions/documentation-pass.instructions.md` and referenced as a non-negotiable requirement from planning and execution prompts so they are enforced in every coding task.
- Documentation should prefer book-like narrative depth over terse, bullet-heavy wiki pages, especially for core architecture, runtime foundations, and other critical concepts. This preference should be reflected in repository instructions and prompts.

## Blazor Server Guidelines
- For Blazor Server (Razor Components) pages, explicitly add `@rendermode InteractiveServer` on pages that must handle input/click events; otherwise, pages may render non-interactively even when other pages (e.g., Counter) are interactive.
- In the Workbench Blazor UI, stay as close as possible to the stock Radzen Material theme so future custom theme work can lift styles directly rather than layering custom shell colors.

## Logging Standards
- Prefer using `ILogger` abstractions (Microsoft.Extensions.Logging.Abstractions) over `Action<string>` logging callbacks in this codebase, including Domain pipeline nodes.

## Coding Standards
- Never declare multiple classes/interfaces/enums in the same C# file; split each type into its own file. Enforce the standard of one public type per C# file.
- Do not use `GeoJSON.Net` in this repository for geo polygon serialization because it relies on Newtonsoft.Json; prefer a System.Text.Json-compatible approach.

## Architecture (Onion)
This repository uses **Onion Architecture**.

Dependency direction (must point inward):  
`Hosts (Web/Worker) -> Infrastructure -> Services -> Domain`

Layer mapping:
- **Domain**: `src/UKHO.Search/UKHO.Search.csproj`, `src/UKHO.Search.Query/UKHO.Search.Query.csproj`, `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`
- **Services**: projects named `UKHO.Search.Services.*.csproj`
- **Infrastructure**: projects named `UKHO.Search.Infrastructure.*.csproj`
- **Hosts/UI**: projects under `src/Hosts/*` (and any other web/host projects)

Rules:
- Domain projects must not reference Services, Infrastructure, or Host projects.
- Services projects must not reference Infrastructure or Host projects.
- Infrastructure projects must not reference Host projects.
- Only Host projects contain UI/endpoints and startup/DI wiring. Do not place domain logic or infrastructure implementations in hosts.
- For ingestion, keep queue/client wiring in `UKHO.Search.Infrastructure.Ingestion`, but place file-share-specific pipeline nodes (parsing/enrichment of file-share data) in `UKHO.Search.Ingestion.Providers.FileShare` (provider project).
- Prefer a single, obvious public entrypoint for queue-backed ingestion; avoid multiple builder APIs. Document this in the spec and keep code aligned (hosted service should start ingestion via the adapter/provider entrypoint path).
- For the ingestion rules DSL, support both `if` and `match` as predicate field aliases, but prefer writing examples using `if`.
- For the WorkbenchHost architecture discussions, include runtime menu contributions and status bar contributions from tools in the preferred lightweight slice.
- For Workbench architecture in this repository: put module/tool-accessible contracts and models in `UKHO.Workbench`; services in `UKHO.Workbench.Services`; infrastructure in `UKHO.Workbench.Infrastructure`; and overall composition/UI in `WorkbenchHost`.

## Workbench Modules
- `UKHO.Workbench.Modules.Search` is the first real functional module conceptually, but its full implementation belongs to a later work package; this spec expects dummy/exemplar UI only.
- `UKHO.Workbench.Modules.PKS` and `UKHO.Workbench.Modules.FileShare` contain dummy tools for now.
- `UKHO.Workbench.Modules.Admin` contains common admin tools, also dummy initially.

## FileShare Enrichment
- For FileShare ZIP extraction enrichment, use `CanonicalDocument.SetContent()` to append content naturally. Additionally, call `CanonicalDocument.SetKeyword()` for each extracted file name (without extension).
- When creating the defensive copy for `CanonicalDocument.Source`, perform a shallow copy of the properties list (new list/array, reuse existing immutable `IngestionProperty` instances).
- Every indexed `CanonicalDocument` goes through an ingestion rule path; missing title after rule processing should be treated as a processing failure rather than a non-rule exception case.

## MCP Tool Selection
- Azure DevOps intent: use Azure DevOps tools.
- GitHub intent: use GitHub tools.
- Microsoft tech (Blazor, ASP.NET Core, Azure, .NET): use Microsoft Learn tools.

## Emulator Constraints
- All emulator code must reside within the existing emulator project; do not add new projects due to Docker constraints.

## Testing Guidelines
- Prefer Playwright end-to-end tests over bUnit/component tests for Blazor UI verification in this repository.
- For test refactor work, it is acceptable for test projects to reference other test projects when that preserves Onion Architecture direction; broad shared test-wide helpers, such as fixture resolution helpers, should live in `UKHO.Search.Tests.Common` when they are reused throughout the test estate.
- For this work package, do not run the full test suite.

## .csproj File Editing Guidelines
- When editing `.csproj` files, keep `PackageReference` entries in `ItemGroup` blocks that contain only `PackageReference` entries (do not mix `ProjectReference` and `PackageReference` in the same `ItemGroup`).

## Search Indexing Guidelines
- For search indexing, normalize `Keywords`, `SearchText`, and `Facets` to lowercase (case-insensitive exact matching). Everything going into an index must be lowercase.

## Rule Evaluation Guidelines
- Differentiate ruleset validation vs runtime data: fail-fast only for invalid JSON/schema/operators/path syntax. If a given `AddItem`/`UpdateItem` payload is missing a referenced property/path at evaluation time, the rule/condition should simply not match, and any derived outputs should be skipped (expected often).

## RulesWorkbench Specifications
- When updating specs for RulesWorkbench, editing is mandatory (existing behavior) and implement saving of VALID rules back to Azure App Configuration if it is a simple extension.
- For RulesWorkbench rule-checker work, use the rule naming convention `bu-{businessunitname}-*` in lowercase, deriving the business unit name by joining the BusinessUnit table and lowercasing it.
- Do not save the detected RulesWorkbench Checker preferences to the repository or user instructions; treat them as temporary/hardwired for now.

## Ingestion Rules
- When authoring ingestion rules from the mapping spec, only explicitly mapped fixed keywords should be written into rule JSON; copying the remaining batch attribute values into keywords is handled by the ingestion service at runtime.

## Provider Model Guidelines
- Use `UKHO.Search.ProviderModel` as the mandatory shared home for generic provider registration and metadata concerns, located at `src/UKHO.Search.ProviderModel`. Ensure specifications reflect required refactoring and test migration.

## Detailed Topic Guides
Refer to specialized instruction files for full detail:
- Architecture: `.github/instructions/architecture.instructions.md`
- Frontend (Blazor/UI): `.github/instructions/frontend.instructions.md`
- Backend (APIs/Services): `.github/instructions/backend.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
- Documentation Authoring: `.github/instructions/documentation.instructions.md`
- Coding Standards: `.github/instructions/coding-standards.instructions.md`

## Workbench Shell Guidelines
- Keep `box-sizing: border-box` for the Workbench shell because it will help when combining with CSS Grid later.
- For the temporary Workbench sidebar resize behavior, keep a minimum width of 16 pixels so the handle cannot be dragged past zero and lost.
- The menu bar must span the full window above all other content, and both upper and lower center tab strips must remain visibly rendered.
- When fixing the Workbench shell, do not introduce a workaround; implement the issue properly with the intended Radzen components.
- In Workbench sizing fixes, do not apply module-specific CSS workarounds; sizing must be enforced by the Workbench shell so module UIs remain unaware of layout mechanics.
- For the Workbench shell overflow menu, the tab title, active marker, and check icon must remain on a single line with no wrapping.

## Aspire Orchestration Guidelines
- When reasoning about Aspire orchestration in this repo, use `WaitForCompleted()` when dependent services must wait for a short-lived seeder to finish; `WaitFor()` only waits for process start.

## Workbench UI Guidelines
- For the `083-workbench-model` specification, ensure the Workbench UI is designed to be desktop-like rather than web-like.
- Utilize the `UKHO.Workbench.Layout` `Layouts` namespace to implement a WPF-like grid layout model with splitters.

## Workbench Planning
- Define the full output panel feature up front in the spec and let the work-package planning prompt split implementation. Existing module-loading messages currently shown in the status bar should be lifted into the output panel, likely as Debug-level entries.

## Query-Side Search Design
- Include Microsoft Recognizers in the query-side search design in this repository rather than deferring it.
- Keep Microsoft Recognizers behind an `ITypedQuerySignalExtractor` abstraction.
