# Prompt: SPEC Single-File Rules

## Objective
Ensure specification work keeps one canonical unversioned file per scope in the active Work Package folder under `dev/work-packages/`.

## Single-File Policy
- Each spec scope keeps one canonical file in the active Work Package folder.
- Update the existing file in place when the same scope evolves.
- Create a new spec file only when the scope is genuinely different.
- Excluded domains: ADDS Mock detailed internals (do not regenerate or update).
- Include module specs under `<work-package>/modules/*` in the same policy.

## Steps for Agent
1. Discover existing spec files matching the requested scope in the active Work Package folder and `<work-package>/modules/*/`.
2. If the requested scope already exists, update that file in place.
3. If the requested scope does not exist, create a new unversioned spec file using the repository naming pattern.
4. Update cross-reference sections so they point at the canonical unversioned filenames.

## Safeguards
- Do not create duplicate files that represent the same scope.
- Keep the canonical spec file simple and unversioned unless the user explicitly asks for extra metadata.
- Keep links and examples aligned to canonical unversioned filenames.

## Module-Specific Notes
- Module specs reside at `<work-package>/modules/<module-name>/`.
- Keep domain, api, frontend module specs separate by scope, not by version.

## Gaps & Unknowns Handling
- Preserve `(Unverified)` markers where evidence is missing.
- Do not invent test coverage or infrastructure not scanned.

## Completion Checklist
- Canonical spec file created or updated.
- Cross-references updated to the canonical filename.
- No redundant version-suffixed spec files created.