# UKHO.Search.Tests.Common

Shared helper-only test infrastructure for the repository.

## Scope

- Broadly reusable helpers that do not reference production projects.
- Shared sample-data resolution helpers.

## Usage

Reference this project only from test projects that need shared helper functionality.

- Keep this project helper-only; do not add real tests here.
- Prefer it for cross-cutting test infrastructure such as `SampleDataFileLocator`.
- Use it when resolving files from `test/sample-data` across multiple test projects.
