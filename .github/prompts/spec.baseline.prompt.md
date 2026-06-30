# Prompt: SPEC Family / RESEARCH Phase (Baseline Extraction from Existing Solution)

## Objective
Create initial (draft) specification documents that accurately describe the current state of the existing solution WITHOUT proposing changes.

## Scope Coverage
Include:
- Solution/project structure (.NET9, Blazor, Aspire usage)
- Architecture layers (UI, API, shared, infra, mocks, workers/functions)
- Technology stack components
- Domain entities & relationships
- Public API surface (routes, verbs, purpose, auth, models)
- Blazor UI components (pages, shared components, navigation, state patterns)
- Cross-cutting concerns (DI, logging, error handling, caching, configuration, serialization, security, accessibility)
- Infrastructure & deployment descriptors (AZD, Bicep, hosting models)
- Testing status (existing test project layout & patterns)

## Required Inputs (gather via inspection)
1. List of projects (names, paths, target frameworks).
2. Namespaces & folders reflecting domains/features.
3. Minimal API or controller endpoints summary.
4. Reusable services & interfaces.
5. Blazor component catalog (Pages, Shared, Layouts, Forms, Results).
6. Shared models / DTOs / contracts.
7. Configuration & environment management approach.
8. Serialization strategy (source-generated contexts?).
9. Error handling patterns (middleware, boundaries).
10. Authentication/authorization mechanisms.
11. Theming/styling approach (Bootstrap + CSS variables).
12. Testing coverage indicators (presence of tests per area).

## Output Documents (initial draft files)
Create the following spec files as applicable:
- `spec-system-overview.md`
- `spec-architecture-components.md` (if system overview would become too large)
- `spec-domain-[context].md` (one per domain context if needed)
- `spec-api-functional.md`
- `spec-frontend-functional.md`
- `spec-infra-deployment.md` (if infra files exist)

If a section lacks implementation evidence, include the heading with "No current implementation" or "Unverified".

## Document Structure Template (apply to each)
1. Title
2. Status: Draft / Baseline Extraction
3. Date
4. Scope / Purpose
5. Context & Overview
6. Components / Modules
7. Detailed Elements (domain entities, endpoints, components, infra items)
8. Cross-Cutting Concerns
9. Non-Functional Characteristics (performance, scalability, security, accessibility, reliability)
10. Gaps & Unknowns
11. Future Indicators (only existing TODOs/placeholders; no invention)
12. Traceability (source paths, related docs)

## Rules & Constraints
- Do NOT invent future features.
- Use concise bullet lists; avoid redundancy.
- Reference relative paths (e.g., `src/Shell/UKHO.ADDS.Management/`).
- Mark uncertain areas as `(Unverified)`.
- Maintain consistent terminology across docs.
- Ensure every item from high-level instruction files is either confirmed or marked as gap.

## Gap & Risk Enumeration
List:
- Missing tests for critical services/components.
- Lack of documentation for APIs/components.
- Security/auth unclear or absent.
- Inconsistent naming/style patterns.

## Completion Checklist
- All planned spec files created under the active Work Package folder in `dev/work-packages/`.
- Naming rules followed.
- Each spec includes required sections.
- Cross-references added between specs where appropriate.
- Gaps clearly identified.

## Follow-On Planning Suggestions
After specs: propose plan document stubs, e.g.:
- `plan-backend-refactor-auth.md`
- `plan-frontend-accessibility.md`
- `plan-tests-coverage-improvement.md`

---
Responder Instructions: Use workspace exploration tools to collect real data before writing specs. Produce files iteratively starting with system overview.
