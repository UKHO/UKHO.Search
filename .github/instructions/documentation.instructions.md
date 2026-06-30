# Copilot Instructions: Documentation

## Scope
Guidelines for authoring and maintaining specifications, plans, API docs, component docs, and single-file specification workflow.

## Work Package documentation

### Work Package location
- Create a single Work Package folder under `./dev/work-packages/` for all outputs.
- Folder naming: `xxx-<descriptor>` where `xxx` is the next incremental number (e.g. `001`, `002`, ...).
- Store the overview spec, component/service specs, plans, and architecture notes inside the same Work Package folder.

### Collaboration pattern (spec.research)
- Separate spec per service/component; overview references each.

## Specifications (Requirements)
- Filename pattern: `spec-<scope>-<descriptor>.md`.
- Maintain a single canonical spec file per scope in the active Work Package folder.
- Update the existing spec file in place when the same scope evolves.

## Spec File Model
Within a Work Package folder, each scope should normally have one active spec file.

- Create a new spec file only when the scope is genuinely different.
- Update an existing spec file in place when refining the same scope.
- Keep filenames stable so links and contributor workflows always point at the same canonical document.

Additional safeguards (extended behavior):
- Do not duplicate the same scope across multiple active spec files unless the user explicitly asks for separate documents.
- Preserve `(Unverified)` markers where evidence not yet collected.

## Modules
Within a Work Package folder:
- Module specs should be grouped logically (optional) under `modules/<module-name>/`.
- Required initial file: `spec-domain-<module-name>.md` capturing purpose, scope, gaps.
- Optional per-module specs: `spec-api-<module-name>.md`, `spec-frontend-<module-name>.md`.
- Each module spec should remain a single canonical file for that module scope.

## Plans (Implementation / Execution)
- Store plans under the Work Package folder (recommended: `<work-package>/plans/<area>/`).
- Filename pattern: `plan-<area>-<purpose>.md`.
- Each plan references source spec files: `Based on: spec-api-functional.md`.
- Include Baseline (current implemented), Delta (planned changes), Carry-over (incomplete / deferred items).
- Use Work Item / Task / Step hierarchy from plan prompt.

Plans (extended note):
- Plans follow the same single-file approach as specs and should live under the Work Package folder.

## Workflow (Authoring Sequence)
1. Inspect codebase / gather evidence.
2. Create the spec file if it does not exist, or update the existing spec file in place if it does.
3. Generate or update plan referencing latest specs.
4. Implement code changes; update docs in same feature branch.
5. Merge with branch checks ensuring spec & plan consistency.

## Validation Checklist
- Correct Work Package folder placement (`dev/work-packages/xxx-<descriptor>/...`).
- Filename matches the unversioned spec naming pattern.
- Overview spec references only current component/module spec versions.
- Plans reference latest spec versions and contain Baseline/Delta/Carry-over.

## Documentation Maintainability
- Avoid duplication; reference canonical spec rather than copying text.
- Keep API examples synchronized with implementation.
- Treat documentation updates as part of Definition of Done for each change set.

## Validation
- Ensure the correct canonical spec file was updated for the requested scope.
- Preserve `(Unverified)` markers for unevidenced areas.

## File Naming Summary
- Spec: `spec-<scope>-<descriptor>.md`
- Module spec: `spec-<domain|api|frontend>-<module>.md`
- Plan: `plan-<area>-<purpose>.md`

## Spec File Safeguards
- Reuse the existing spec file for the same scope unless the user asks for a split.
- Avoid creating duplicate files that represent the same scope.
- Keep links, references, and examples aligned to the canonical unversioned filenames.

## Plan File Safeguards
- Reuse the existing plan file for the same scope unless the user asks for a split.
- Avoid creating duplicate plan files that represent the same scope.
- Keep plan references and examples aligned to the canonical unversioned filenames.

End of File.
