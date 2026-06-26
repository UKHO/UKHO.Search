# Production Documentation Uplift Specification

- Work Package: `071-documentation`
- Version: `v0.01`
- Status: `Draft`
- Target Output Path: `dev/work-packages/mvp/071-documentation/spec-domain-code-documentation_v0.01.md`

## 1. Overview

This work package defines a documentation-only quality uplift for a constrained set of production and test projects in the repository. The objective is to bring the selected codebase areas to production documentation quality without altering runtime behaviour, signatures, logic, formatting, or structure beyond the addition of source-code comments.

The specification covers a repository-wide pass over the listed projects to ensure consistent XML documentation on public APIs and consistent developer-facing inline comments within methods, with additional scrutiny for tests.

This work is explicitly documentation-only. No code logic changes, refactors, renames, formatting-only edits, dependency changes, or behavioural changes are permitted as part of this work item.

## 2. In-Scope Components

The following projects are in scope for this work item only:

- `UKHO.Search`
- `UKHO.Search.Tests`
- `UKHO.Search.Ingestion`
- `UKHO.Search.Ingestion.Tests`
- `UKHO.Search.ProviderModel`
- `UKHO.Search.ProviderModel.Tests`
- `UKHO.Search.Query`
- `UKHO.Search.Query.Tests`
- `UKHO.Search.Services.Ingestion`
- `UKHO.Search.Services.Ingestion.Tests`
- `UKHO.Search.Ingestion.Providers.FileShare`
- `UKHO.Search.Ingestion.Providers.FileShare.Tests`
- `UKHO.Search.Infrastructure.Ingestion`
- `UKHO.Search.Infrastructure.Ingestion.Tests`
- `AppHost`
- `AppHost.Tests`
- `IngestionServiceHost`
- `IngestionServiceHost.Tests`

## 3. High-Level Requirements Summary

### 3.1 Documentation Outcomes

The implementation work governed by this specification must:

1. Inspect every `.cs` file in the in-scope projects.
2. Add or complete XML documentation comments for every public class, public method, and public property where appropriate.
3. Ensure every documented public method includes XML comments for every parameter.
4. Ensure every public constructor is XML documented, including every constructor parameter.
5. Add developer-level inline comments within every method, including private methods, describing intent, algorithm, important steps, and rationale.
6. Add especially careful developer comments in test methods so the test scenario, setup, action, assertion intent, and behavioural significance are clear.
7. Preserve existing code behaviour exactly.

### 3.2 Non-Negotiable Constraint

This work item must not change, remove, or otherwise alter any code behaviour. Any change beyond comments is out of scope and constitutes a failure against the stated requirement.

## 4. Source Context Used

This draft has been aligned with current repository guidance from:

- `wiki/Solution-Architecture.md`
- `wiki/Documentation-Source-Map.md`
- `.github/prompts/spec.research.prompt.md`
- `.github/instructions/documentation.instructions.md`
- `.github/copilot-instructions.md`

## 5. Initial Assumptions

- The deliverable for this work package is a single specification document in `dev/work-packages/mvp/071-documentation/`.
- The future implementation effort will be allowed to add comments only, with no semantic code edits.
- Repository and wiki references are used for architectural and historical context, not to widen scope beyond the listed projects.

## 6. Clarified Decisions

- Generated or machine-maintained C# files must be excluded from the documentation pass.
- The intended exclusions include generated output such as `obj` artifacts, designer/generated output, and source-generator output.
- The documentation uplift applies to hand-maintained source files within the listed projects.
- Every public property in scope must have XML documentation comments, including trivial DTO, record, options, and configuration properties.
- Hand-written test host, bootstrap, fixture, and setup code must receive the same developer-comment standard as other hand-written code in scope.
- The same XML-comment standard applies to additional public API surface types when present, including public interfaces, records, enums, delegates, and extension classes.
- Non-public types do not require XML documentation comments by default; they must still receive the mandatory developer-level inline method comments within their methods.
- Every method must contain a clear explanatory comment block, and multi-step methods must also include step-by-step inline comments through the body where needed to explain the flow and rationale.
- Delivery must include the comment updates only; no separate completion checklist or audit summary is required as a formal sign-off artifact.
- Inherited XML documentation is not sufficient for this work item; every eligible public member must contain explicit local XML comments in the source file.
- Existing weak or inconsistent comments should be rewritten so the in-scope code reads in one consistent documentation style.
- XML documentation must be high-depth: require `summary`, `param`, and `returns` wherever applicable, and add `remarks` and `exception` tags wherever they can be reliably inferred from the code.
- Top-level host bootstrap files such as `Program.cs` must receive developer-level comments throughout top-level statements and local functions, with XML comments used only where C# supports them.
- Every partial declaration file must carry XML comments where that partial declares public API members.
- Public enum members and public delegate parameters must receive explicit XML documentation wherever they are part of the public API surface.
- Other public API elements must also be documented to the same XML standard when present, including public events, indexers, operators, and public fields.
- XML `<example>` tags are in scope only for especially complex public APIs in production projects; they are not required for test projects.
- All existing non-comment formatting must be preserved exactly, aside from the inserted comments themselves.
- Existing `TODO`, `FIXME`, and placeholder comments should not be broadly rewritten or removed; acceptable existing comments should be left in place unless directly superseded by the required production-quality documentation.
- Every method still requires developer-level explanatory comments, even where the implementation is empty, trivial, or apparently self-evident.
- Obsolete or deprecated public APIs must still be uplifted to the same full XML-documentation standard while they remain in source.
- Local functions and non-trivial lambda expressions inside methods must also receive developer-level explanatory comments when they contain meaningful logic.
- Test methods must be explained carefully, but the documentation pass must not force a rigid `Arrange` / `Act` / `Assert` comment structure.
- Private properties, fields, and constants should receive explanatory comments where needed to explain non-obvious purpose, coupling, constraints, or behaviour.
- Added comments may explain both technical intent and relevant domain/business intent where that materially improves understanding and can be supported by the repository code and documentation context.
- Generic public APIs must include XML `<typeparam>` documentation for every public generic type and every public generic method type parameter.
- Asynchronous public methods must explicitly document cancellation expectations and notable externally visible side effects wherever they are relevant.
- Public extension methods must document the `this` parameter fully like any other parameter, including important constraints and expectations.
- Public tuple-returning APIs must explicitly document the meaning of individual tuple elements whenever element names alone are not sufficient.
- Where sources disagree, added comments must treat current code behaviour as the primary source of truth, then current wiki guidance, then older work-package documentation.
- Where exact intent is not directly explicit, the documentation pass should infer the most likely intent from the surrounding code and current repository documentation rather than defaulting to minimal wording.
- XML `<exception>` tags must document only exceptions that are explicit or clearly intentional in the code, rather than every incidental framework exception that could occur indirectly.
- XML comments should explicitly call out meaningful nullability expectations whenever type annotations alone are not sufficient to explain important null-handling semantics.
- If material ambiguity still remains after using the code and repository documentation, the implementation may explicitly indicate that uncertainty in source comments rather than presenting unsupported certainty.
- After the documentation-only pass, validation must include a full solution build and a full test suite run to confirm that no accidental non-comment changes were introduced.

## 7. Open Questions

None at present.
