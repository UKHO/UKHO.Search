# Plan: Legacy Workbench Deletion First

Based on: [spec-domain-delete-legacy-workbench-first.md](spec-domain-delete-legacy-workbench-first.md)

Date: 2026-07-01

## Baseline

The repository currently contains an active legacy Workbench implementation under `src/Workbench/`. AppHost still starts the old `WorkbenchHost`, the maintained solution still includes Workbench projects, and planning documents still need to distinguish the old Workbench shell from the future internal `WorkbenchHost` concept.

## Delta

This work package removes the legacy Workbench surface completely before any new internal `WorkbenchHost` is introduced. It removes active runtime references, removes maintained solution references, deletes the old Workbench source tree, and updates planning and composition documentation so the old Workbench is no longer treated as active or future-source material.

## Carry-over

This work package does not create the new internal `WorkbenchHost`, does not migrate specific legacy behavior into the new host, does not remove `tools/RulesWorkbench`, and does not change `FileShareEmulator` behavior.

## Work Item 1: Remove Active Runtime And Solution References

### Purpose

Stop the legacy Workbench from participating in the maintained runtime and solution before deleting the source tree.

### Tasks

#### Task 1.1: Remove AppHost orchestration references

- Remove the legacy `WorkbenchHost` project reference from AppHost if it is only present to launch the deleted host.
- Remove the legacy `WorkbenchHost` startup wiring from services-mode orchestration.
- Confirm no other AppHost logic still assumes the legacy Workbench is present.

#### Task 1.2: Remove maintained solution references

- Remove legacy Workbench projects from `Search.slnx`.
- Confirm no remaining maintained project references point into `src/Workbench/`.

## Work Item 2: Delete The Legacy Workbench Tree

### Purpose

Remove the old Workbench implementation completely so the future internal `WorkbenchHost` name is clean.

### Tasks

#### Task 2.1: Delete server-side legacy Workbench code

- Delete `src/Workbench/server/WorkbenchHost/`.
- Delete `src/Workbench/server/WorkbenchHost-old/`.
- Delete `src/Workbench/server/OldWorkbenchHost/`.
- Delete `src/Workbench/server/UKHO.Workbench/`.
- Delete `src/Workbench/server/UKHO.Workbench.Infrastructure/`.
- Delete `src/Workbench/server/UKHO.Workbench.Services/`.

#### Task 2.2: Delete legacy modules, samples, and vendored support code

- Delete `src/Workbench/modules/`.
- Delete `src/Workbench/samples/`.
- Delete `src/Workbench/radzen-blazor/`.

## Work Item 3: Update Planning And Composition Documentation

### Purpose

Remove documentation ambiguity after the legacy source tree is deleted.

### Tasks

#### Task 3.1: Update planning documents

- Update Arc 02 and Arc 03 planning stubs to reflect the split-host Blazor direction.
- Update active Arc 02 work-package specs so they no longer point at React or `PublicApiHost`.
- Add and retain the WP126 deletion-first specification and this execution plan.

#### Task 3.2: Update runtime-composition and discussion references

- Update the next-gen discussion and arc summary documents so they no longer describe the old Workbench as the future internal host direction.
- Update any remaining current-state references that would incorrectly imply the deleted Workbench still participates in the maintained product direction.

## Work Item 4: Validate The Deletion

### Purpose

Prove that the legacy Workbench has been removed cleanly and that planning now points to the new direction.

### Tasks

#### Task 4.1: Validate structural removal

- Confirm AppHost no longer references the legacy `WorkbenchHost`.
- Confirm `Search.slnx` no longer references projects under `src/Workbench/`.
- Confirm the `src/Workbench/` tree has been removed.

#### Task 4.2: Validate documentation direction

- Confirm the updated planning documents consistently describe:
  - `QueryServiceHost` as the public host,
  - a future new `WorkbenchHost` as the internal host,
  - the old Workbench tree as deleted legacy code.

## Delivery Notes

- Execute deletion before any new internal `WorkbenchHost` project is introduced.
- If later work needs a specific legacy behavior, recover or re-implement it deliberately from repository history rather than restoring the deleted shell wholesale.
- `tools/RulesWorkbench` remains a separate later retirement or replacement decision and is not part of this deletion-first slice.

Wiki review result:
No wiki page update is required for this planning artifact. The plan organizes a future deletion and planning transition rather than documenting a completed runtime change.