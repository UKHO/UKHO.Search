# Copilot Instructions (High-Level)

You are an agent. Continue working until queries are fully resolved.  
Be concise but complete. Prefer current research (Microsoft Learn) for Microsoft technologies.

## Quick Principles
- Verify each command succeeds before proceeding; run commands sequentially.
- Prefer latest C#/.NET features; async/await; nullable reference types.
- Do not interact with git (no branch creation, no git commands) unless explicitly requested.

## MCP Tool Selection
- Azure DevOps intent: use Azure DevOps tools.
- GitHub intent: use GitHub tools.
- Microsoft tech (Blazor, ASP.NET Core, Azure, .NET): use Microsoft Learn tools.

## Documentation Workflow (Summary)
- For each new Work Package/piece of work: create a new numbered folder under `./docs/` named `xxx-<descriptor>` (e.g. `001-Initial-Shell`).
- Store ALL related documents (specs, plans, architecture notes, etc.) together inside that Work Package folder.
- Do not overwrite prior work packages; create the next incremental folder (e.g. `002-...`).
- Use appropriate prompt family & phase from `.github/prompts/`.

## Emulator Constraints
- All emulator code must reside within the existing emulator project; do not add new projects due to Docker constraints.

## Testing Guidelines
- Prefer Playwright end-to-end tests over bUnit/component tests for Blazor UI verification in this repository.

## .csproj File Editing Guidelines
- When editing `.csproj` files, keep `PackageReference` entries in `ItemGroup` blocks that contain only `PackageReference` entries (do not mix `ProjectReference` and `PackageReference` in the same `ItemGroup`).

## Detailed Topic Guides
Refer to specialized instruction files for full detail:
- Architecture: `.github/instructions/architecture.instructions.md`
- Frontend (Blazor/UI): `.github/instructions/frontend.instructions.md`
- Backend (APIs/Services): `.github/instructions/backend.instructions.md`
- Testing: `.github/instructions/testing.instructions.md`
- Documentation Authoring: `.github/instructions/documentation.instructions.md`
- Coding Standards: `.github/instructions/coding-standards.instructions.md`

All original guidance now resides in one of these files. Do not duplicate; update the relevant file when changing practices.
