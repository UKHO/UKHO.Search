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

## Logging Standards
- Prefer using `ILogger` abstractions (Microsoft.Extensions.Logging.Abstractions) over `Action<string>` logging callbacks in this codebase, including Domain pipeline nodes.

## Coding Standards
- Never declare multiple classes/interfaces/enums in the same C# file; split each type into its own file. Enforce the standard of one public type per C# file.

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

## MCP Tool Selection
- Azure DevOps intent: use Azure DevOps tools.
- GitHub intent: use GitHub tools.
- Microsoft tech (Blazor, ASP.NET Core, Azure, .NET): use Microsoft Learn tools.

## Documentation Workflow (Summary)
- For each new Work Package/piece of work: create a new numbered folder under `./docs/` named `xxx-<descriptor>` (e.g. `001-Initial-Shell`).
- Store ALL related documents (specs, plans, architecture notes, etc.) together inside that Work Package folder.
- Do not overwrite prior work packages; create the next incremental folder (e.g. `002-...`).
- When asked to create specification documents for a work package, create the markdown files in the repository under the requested `docs/xxx-.../` folder (not just provide the content in chat).
- Use appropriate prompt family & phase from `.github/prompts/`.

## Emulator Constraints
- All emulator code must reside within the existing emulator project; do not add new projects due to Docker constraints.

## Testing Guidelines
- Prefer Playwright end-to-end tests over bUnit/component tests for Blazor UI verification in this repository.

## .csproj File Editing Guidelines
- When editing `.csproj` files, keep `PackageReference` entries in `ItemGroup` blocks that contain only `PackageReference` entries (do not mix `ProjectReference` and `PackageReference` in the same `ItemGroup`).

## Search Indexing Guidelines
- For search indexing, normalize `Keywords`, `SearchText`, and `Facets` to lowercase (case-insensitive exact matching).

## Detailed Topic Guides
Refer to specialized instruction files for full detail:
- Architecture: `.github/instructions/architecture.instructions.md`
- Frontend (Blazor/UI): `.github/instructions/frontend.instructions.md`
- Backend (APIs/Services): `.github/instructions/backend.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
- Documentation Authoring: `.github/instructions/documentation.instructions.md`
- Coding Standards: `.github/instructions/coding-standards.instructions.md`

All original guidance now resides in one of these files. Do not duplicate; update the relevant file when changing practices.
