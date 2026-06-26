# Copilot Instructions: Documentation

## Scope
Guidelines for authoring and maintaining specifications, plans, API docs, component docs, and archive/versioning workflow.

## Work Package documentation

### Work Package location
- Create a single Work Package folder under `./dev/work-packages/` for all outputs.
- Folder naming: `xxx-<descriptor>` where `xxx` is the next incremental number (e.g. `001`, `002`, ...).
- Store the overview spec, component/service specs, plans, and architecture notes inside the same Work Package folder.

### Collaboration pattern (spec.research)
- Separate spec per service/component; overview references each.

## Specifications (Requirements)
- Filename pattern: `spec-<scope>-<descriptor>_vX.XX.md`.
- Versioning: never overwrite existing spec; create new file with incremented version suffix and update internal `Version:` field.
- Drafts start at `v0.01`; first stable baseline release `v1.0`. Post-implementation increments toward `v2.0`.
- Include `Supersedes: <previous-file>` and a Change Log section noting prior version(s).

## Spec Versioning & Archiving
Within a Work Package folder, specs are still versioned and treated as immutable snapshots.

- Always create new spec versions; never overwrite existing files.
- Filename pattern: `spec-<scope>-<descriptor>_vX.XX.md`.
- Place the "current" latest versions in the Work Package folder root.
- If you supersede a spec in a Work Package folder:
  - Create an `archive/` subfolder under that Work Package folder.
  - Move superseded versions into `<work-package>/archive/` preserving filenames.
  - Never edit archived files.
  - Ensure an `<work-package>/archive/ARCHIVE_README.md` exists describing the policy (create if missing).

Additional safeguards (extended behavior):
- Determine current latest version per spec name (prefix before `_v`).
- Increment minor (e.g., v0.02 -> v0.03) or follow release cadence (v0.09 -> v0.10, v0.99 -> v1.0).
- Preserve `(Unverified)` markers where evidence not yet collected.

## Modules
Within a Work Package folder:
- Module specs should be grouped logically (optional) under `modules/<module-name>/`.
- Required initial file: `spec-domain-<module-name>_v0.01.md` capturing purpose, scope, gaps.
- Optional per-module specs: `spec-api-<module-name>_v0.01.md`, `spec-frontend-<module-name>_v0.01.md`.
- Each module spec independently versioned & archived using the Work Package `archive/` rules above.

## Plans (Implementation / Execution)
- Store plans under the Work Package folder (recommended: `<work-package>/plans/<area>/`).
- Filename pattern: `plan-<area>-<purpose>_vX.XX.md`.
- Each plan references source spec versions: `Based on: spec-api-functional_v1.2.md`.
- Include Baseline (current implemented), Delta (planned changes), Carry-over (incomplete / deferred items).
- Use Work Item / Task / Step hierarchy from plan prompt.
- Archive policy may be extended to plans (currently enforced for specs only).

Plans (extended note):
- Plans follow similar versioning and should live under the Work Package folder.
- If archiving of plans is adopted, mirror the Work Package `archive/` pattern.

## Workflow (Authoring Sequence)
1. Inspect codebase / gather evidence.
2. Create or increment spec version (apply archiving rules).
3. Generate or update plan referencing latest specs.
4. Implement code changes; update docs in same feature branch.
5. Merge with branch checks ensuring spec & plan consistency.

## Validation Checklist
- Correct Work Package folder placement (`dev/work-packages/xxx-<descriptor>/...`).
- Filename matches pattern and `Version:` field matches suffix.
- `Supersedes:` line present (except initial v0.01).
- Change Log includes new entry referencing previous version.
- Overview spec references only current component/module spec versions.
- Plans reference latest spec versions and contain Baseline/Delta/Carry-over.
- Archive folder contains only superseded versions; no duplicates in active folders.

## Documentation Maintainability
- Avoid duplication; reference canonical spec rather than copying text.
- Keep API examples synchronized with implementation.
- Treat documentation updates as part of Definition of Done for each change set.

## Validation
- Ensure archive folder exists before moving.
- Never modify archived files.
- Preserve `(Unverified)` markers for unevidenced areas.

## File Naming Summary
- Spec: `spec-<scope>-<descriptor>_vM.NN.md`
- Module spec: `spec-<domain|api|frontend>-<module>_vM.NN.md`
- Plan: `plan-<area>-<purpose>_vM.NN.md`

## Archiving Safeguards
- Ensure archive folder exists; create if missing before moves.
- Do not treat files in `archive/` as candidates for version increment.
- Never delete archived files.

End of File.
