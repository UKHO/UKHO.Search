# Prompt: SPEC Family / UPLIFT Phase (Delta from Baseline to Next Version)

## Objective
Produce updated specification documents for the next version of the system by deriving deltas from the existing latest specifications using an interactive Q&A with the requestor. Do not inspect the repository or code in this mode. Capture only what changed, was added, removed, or deprecated. Preserve prior versions and increment spec versions. All uplift outputs MUST follow the Functional Specification Document (FSD) style defined in the repository `spec-template_v1.1.md` template.

## Scope Coverage (Delta-Focused)
Focus exclusively on proposed changes since the baseline across:
- Solution/project structure (e.g., .NET version, Blazor, Aspire usage)
- Architecture layers (UI, API, shared, infra, mocks, workers/functions)
- Technology stack components & versions
- Domain entities & relationships
- Public API surface (routes, verbs, purpose, auth, models)
- Blazor UI components (pages, shared components, layouts, navigation, state patterns)
- Cross-cutting concerns (DI, logging, error handling, caching, configuration, serialization, security, accessibility)
- Infrastructure & deployment descriptors (AZD, Bicep, hosting models)
- Testing (projects, coverage indicators, notable gaps)

## Required Inputs (Conversation-Driven)
Gather only from the requestor and existing documents (only inspect the repo if you need to understand the change from the current position):
1. Baseline spec files under the active Work Package folder in `dev/work-packages/` (latest vX.Y for: system overview, architecture components, domain specs, API functional, frontend functional, infra/deployment).
2. Initial intent/goal statement for the next version (requestor�s prompt).
3. Q&A responses detailing deltas (Added/Changed/Removed/Deprecated) across scope areas.
4. Non-functional goals and constraints (performance, security, availability, compliance, accessibility, timelines).
5. Out-of-scope items and known risks.
6. Optional references (tickets/PRs/links) if provided by the requestor; treat absent items as �Unverified�.

## Collaboration Process (Interactive Uplift)
- Start by asking clarification questions (one at a time) to understand the desired next version.
- Ask one question at a time; try to number answer options when possible.
- Before each new question, output the current uplift spec snapshot (overview + any component/service deltas captured so far) in markdown.
- Organize deltas by area (Architecture, API, Frontend/Blazor, Infra, Cross-cutting, Testing).
- Suggest considerations the requestor may have missed (e.g., versioning, auth impacts, data migrations) and flag important decisions.
- Continue iterating until deltas are complete and unambiguous or explicitly marked �(Unverified)�. 
- After specs are finalized, hand off to the planning prompt `spec.plan.prompt.md` to create implementation plans.

## Output Documents (Uplift Versions)
Create new versions without overwriting baseline. Increment version suffix (e.g., from v0.01 to v0.02):
- `spec-system-overview_v{next}.md`
- `spec-architecture-components_v{next}.md` (if necessary)
- `spec-domain-[context]_v{next}.md` (one per domain context as applicable)
- `spec-api-functional_v{next}.md`
- `spec-frontend-functional_v{next}.md`
- `spec-infra-deployment_v{next}.md` (if infra files exist)

Each uplift document MUST:
- Conform to the FSD style and section ordering of the repository `spec-template_v1.1.md` template.
- Place the �Supersedes: <relative-path-to-previous-v{prev}.md>� line immediately after the FSD header block.
- Include a �Delta Summary� table near the top (Executive Summary section) classifying changes (Added / Changed / Removed / Deprecated / Unverified) with evidence links (conversation Q&A references, provided paths/links).
- Mark sections with �No change since vX.Y� when applicable (do not restate unchanged content).

## Document Structure Template (FSD style with delta annotations)
Follow `spec-template_v1.1.md` exactly. Populate only deltas per section; otherwise write �No change since v{prev}�.

Header (from template):
- Functional Specification Document (FSD)
- Project: [name]
- Version: v{next}
- Date: [yyyy-mm-dd]
- Author: [name/team]
- Supersedes: `<relative-path-to-previous-v{prev}.md>`

1. Executive Summary
- Brief delta-focused summary of the uplift intent.
- Delta Summary table (Added/Changed/Removed/Deprecated/Unverified; Before vs After; Evidence; Notes/Impact).

2. System Overview
- State deltas to capabilities/components; else �No change since v{prev}�.

3. Architecture Overview
- Use the architecture table from the template; indicate only changed cells or add �No change since v{prev}�.

4. Functional Requirements
- Use the FR table format from the template. List only Added/Changed/Deprecated/Removed items; keep original IDs where applicable. Note removals explicitly.

5. Non-Functional Requirements
- Use the NFR table format; show only changed/added items. For unchanged categories, write �No change since v{prev}�.

6. Security Requirements
- Use the Security table format; capture only deltas.

7. Data Model Overview
- Use the Entities table plus �Key Relationships�; list only added/changed/removed entities/attributes; annotate relationship changes.

8. Deployment Strategy
- Use the Deployment table format; capture only deltas (CI/CD, IaC, environments, monitoring).

9. Known Issues / Decisions Pending
- Populate new or updated items relevant to the uplift; include migration decisions and open questions. Link to Q&A references.

## Delta Classification Guide
- Added: New item introduced since baseline
- Changed: Existing item modified (behavior, contract, config, structure)
- Removed: Item no longer present
- Deprecated: Item still present but flagged for removal/replacement
- Unverified: Mentioned or suspected change without concrete evidence or explicit confirmation

Recommended Delta Summary table columns:
- Area (Architecture/API/Frontend/Infra/Testing/Cross-cutting)
- Item (Name/Path)
- Status (Added/Changed/Removed/Deprecated/Unverified)
- Before (v{prev})
- After (v{next})
- Evidence (Q&A ref, provided paths/links)
- Notes / Impact

## Rules & Constraints
- Do NOT inspect the repository or code in this mode; rely solely on requestor-provided inputs and Q&A confirmations.
- Do NOT invent future features; only capture changes confirmed in Q&A or explicitly provided.
- Preserve baseline docs; never overwrite. Create uplift docs with incremented versions under the active Work Package folder in `dev/work-packages/`.
- Use concise bullet lists; avoid redundancy; prefer delta statements and �No change since v{prev}�.
- Reference relative paths when provided (e.g., `src/Shell/UKHO.ADDS.Management/`).
- Maintain consistent terminology; highlight Blazor-specific aspects where relevant.
- Mark uncertain areas as �(Unverified) �.
- Ensure strict alignment with the repository `spec-template_v1.1.md` template for layout, tables, and phrasing.

## Completion Checklist
- All uplift spec files created under the active Work Package folder in `dev/work-packages/` with correct version increments.
- Each uplift doc references its baseline predecessor and includes a Delta Summary table in the Executive Summary.
- Sections conform to `spec-template_v1.1.md` with delta annotations or explicit �No change since v{prev}�.
- All changes cross-referenced to Q&A references and any provided evidence.
- Gaps and risks enumerated.
- Cross-references added between uplift docs where appropriate.

## Responder Instructions
Operate in interview mode.
- Do not use workspace exploration tools or inspect code.
- Drive an iterative Q&A, one question at a time; show the current spec snapshot before the next question.
- Use the repository `spec-template_v1.1.md` template for all uplift outputs.
- Prefer Microsoft Learn for authoritative references on .NET/Blazor/Azure features when clarifications are needed.
- Prioritize Blazor details over Razor Pages or MVC when interpreting UI requirements.
- Stop Q&A when deltas are complete or explicitly marked �(Unverified)�, then produce uplift specs (FSD style) and hand off to the planning prompt `spec.plan.prompt.md` for implementation planning.
