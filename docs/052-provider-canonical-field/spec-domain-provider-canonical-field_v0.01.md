# Specification: Add `Provider` field to `CanonicalDocument`

Version: `v0.01`  
Status: `Draft`  
Work Package: `docs/052-provider-canonical-field/`  
Based on: `docs/026-s57-parser/spec-template_v1.1.md`

## 1. Overview

### 1.1 Purpose
Add a system-managed `Provider` field to `CanonicalDocument` so every canonical document records which ingestion provider created it.

### 1.2 Scope
This work package covers:

- adding an immutable `Provider` field to `CanonicalDocument`
- ensuring the provider value is known at queue-read time and propagated through the ingestion pipeline until it is set during canonical document construction
- introducing a mandatory generic `ProviderParameters` carrier for `Provider` and future provider-scoped values that need to flow through generic pipeline nodes
- updating `CanonicalDocumentBuilder` and any related construction paths so `Provider` is supplied at construction time only
- updating the canonical index definition so `Provider` is mapped as a `keyword`
- updating relevant documentation, including `wiki/`, to explain what `Provider` is for and why callers cannot set it directly
- amending existing tests and adding new tests to prove `Provider` is always set
- running the full test suite after the change

Out of scope:

- migration, backfill, or reindex planning for pre-existing indexed documents
- redesigning generic pipeline nodes to make them provider-specific
- allowing users or rules to supply or override `Provider`

### 1.3 Stakeholders
- ingestion pipeline maintainers
- ingestion provider implementers
- search index/schema maintainers
- developers using or extending `CanonicalDocument`
- developers relying on repository documentation and wiki guidance

### 1.4 Definitions
- **Provider**: the system-owned identifier of the ingestion provider that created a `CanonicalDocument`, for example `file-share`
- **ProviderParameters**: a generic parameter carrier used to move provider-scoped values through generic pipeline stages without compromising node generality
- **CanonicalDocument**: the provider-independent ingestion document model used prior to indexing
- **CanonicalDocumentBuilder**: the builder responsible for constructing canonical documents for provider ingestion flows

## 2. System context

### 2.1 Current state
`CanonicalDocument` does not currently expose a first-class `Provider` field. Provider identity is implicit in the ingestion path rather than preserved on the canonical model itself.

The provider is already known when an ingestion request is read from the queue, but that value is not currently carried through to the point where the canonical document is constructed.

As a result:

- indexed canonical documents do not explicitly record their originating provider
- downstream developers cannot easily reason about provider provenance from the canonical model
- documentation does not clearly distinguish between user-authored fields and system-managed provider metadata

### 2.2 Proposed state
Every newly created `CanonicalDocument` will contain a required immutable `Provider` value set at construction time only.

The provider value will be determined from data already available when the ingestion request is read from the queue, then passed through the ingestion pipeline using a generic `ProviderParameters` carrier until it reaches the point where `CanonicalDocumentBuilder` creates the document.

The canonical index definition will expose `Provider` as a `keyword` field so it can be filtered and matched exactly.

Documentation and wiki content will explicitly describe `Provider` as a system-managed field that users cannot set directly.

### 2.3 Assumptions
- ingestion cannot begin without reading a request from a queue, so provider identity is always available at the start of the ingestion flow
- provider identifiers already exist and should be reused rather than introducing a new naming convention
- generic pipeline nodes must remain generic and must not become specialized around provider-specific behavior
- all newly created canonical documents must have `Provider` set before they can continue through the pipeline
- tests may currently construct canonical documents or related pipeline inputs without provider context and will need to be updated accordingly

### 2.4 Constraints
- `Provider` must be immutable after construction
- `Provider` must not be user-settable, rule-settable, or mutable by later pipeline stages
- failure for missing provider context must be fail-fast at the earliest possible opportunity
- the design must not compromise the generality of existing pipeline nodes
- `ProviderParameters` is mandatory for this work item
- no migration scenarios are to be considered in this specification

## 3. Component / service design (high level)

### 3.1 Components
The change affects the following areas:

- `CanonicalDocument`
- `CanonicalDocumentBuilder`
- queue-read / ingestion request dispatch path
- generic pipeline node contracts where provider-scoped parameters are propagated
- `ProviderParameters`
- canonical index definition and projection
- unit, integration, and pipeline tests
- developer documentation and wiki pages describing canonical fields and pipeline behavior

### 3.2 Data flows
The intended high-level flow is:

1. an ingestion request is read from the queue
2. the provider identity is determined from the queue/request context
3. that identity is placed into `ProviderParameters`
4. `ProviderParameters` is passed through generic pipeline stages without making those stages provider-specific
5. `CanonicalDocumentBuilder` receives the provider value and constructs a `CanonicalDocument` with `Provider` set
6. the resulting canonical document is enriched and indexed
7. the canonical index stores `Provider` as a `keyword`

If provider context is unexpectedly missing, processing must fail immediately at the earliest possible point rather than allowing an incomplete canonical document to be created or indexed.

### 3.3 Key decisions
- `Provider` is a canonical provenance field, not user metadata
- `Provider` is required for all newly created canonical documents
- `Provider` must be set only at construction time
- provider context is known at queue-read time and must be passed down until document construction
- generic pipeline node abstractions must remain generic
- `ProviderParameters` is the mandatory extensibility mechanism for carrying `Provider` now and similar provider-scoped values in future
- fail-fast behavior is required if provider context is absent
- documentation must clearly explain why `Provider` cannot be set directly by users

## 4. Functional requirements

1. The system shall add a `Provider` field to `CanonicalDocument`.
2. The `Provider` field shall be immutable and set at construction time only.
3. The system shall set `Provider` to the provider identifier already known when the ingestion request is read from the queue.
4. The system shall propagate provider context through the ingestion pipeline until it can be passed into `CanonicalDocumentBuilder`.
5. The system shall use a mandatory `ProviderParameters` carrier to move `Provider` through generic pipeline stages.
6. The system shall preserve the generality of pipeline nodes and shall not introduce provider-specific node abstractions purely to support this field.
7. `CanonicalDocumentBuilder` shall require provider context when constructing a canonical document.
8. Newly created canonical documents shall always contain a non-empty `Provider` value.
9. The `Provider` value shall use the existing provider identifier, for example `file-share` for the File Share provider.
10. The system shall not allow callers to set or mutate `Provider` after document construction.
11. The canonical index definition shall map `Provider` as a `keyword` field.
12. Relevant repository documentation shall be updated to explain the purpose of `Provider`, where it comes from, and why users cannot set it directly.
13. Relevant `wiki/` content shall be updated to explain `Provider` as a system-managed canonical provenance field.
14. Existing tests affected by the new required field shall be updated.
15. New tests shall be added to demonstrate that `Provider` is always set on newly created canonical documents.
16. The implementation validation for this work item shall include a full test suite run.

## 5. Non-functional requirements

- The design must preserve the existing provider-agnostic architecture of the ingestion pipeline.
- The change must remain explicit and intention-revealing in APIs, especially around document construction.
- The change must not introduce post-construction mutability into `CanonicalDocument`.
- The change should be extensible so future provider-scoped values can be propagated using the same generic mechanism.
- Documentation must reduce confusion for developers by clearly stating that `Provider` is system-managed and not user-authored.
- Failures caused by absent provider context must be detected as early as possible.

## 6. Data model

### 6.1 `CanonicalDocument`
`CanonicalDocument` shall gain a required `Provider` property with the following characteristics:

- type: string
- required for all newly created canonical documents
- immutable after construction
- set only by the construction path
- populated from provider context originating at queue-read time

### 6.2 `ProviderParameters`
A new `ProviderParameters` model shall be introduced as the generic carrier for provider-scoped pipeline parameters.

Minimum requirement for this work item:

- it shall carry `Provider`

Extensibility requirement:

- it shall be suitable for carrying additional provider-scoped values in future without requiring pipeline-node specialization

### 6.3 Index mapping
The canonical index definition shall include a `Provider` field mapped as:

- `keyword`

The field is intended for exact matching, filtering, and provider provenance inspection rather than analyzed full-text search.

## 7. Interfaces & integration

### 7.1 Queue ingress and request dispatch
The queue-read and ingestion request dispatch flow shall surface provider identity into pipeline execution as part of the provider-scoped parameter flow.

### 7.2 Pipeline propagation
Pipeline stages that need to pass request context onward shall accept and forward `ProviderParameters` without embedding provider-specific logic.

### 7.3 Canonical document construction
`CanonicalDocumentBuilder` and any related creation path shall accept provider context explicitly and shall not construct a canonical document without it.

### 7.4 Index projection and schema
Any canonical-to-index projection and index schema validation logic shall be updated so `Provider` is represented consistently and validated as a `keyword` mapping.

### 7.5 Documentation integration
Documentation updates shall include, at minimum, the relevant canonical-model and ingestion-pipeline guidance, including corresponding `wiki/` pages. The updated documentation shall explain:

- what `Provider` represents
- that it is system-managed
- that it is set from queue/provider context
- that users cannot set it directly
- why the field exists in the canonical contract

## 8. Observability (logging/metrics/tracing)
This change does not require new metrics or traces.

However, fail-fast behavior for missing provider context should remain diagnosable through existing error and dead-letter mechanisms where applicable. Any error surfaced for missing provider context should make clear that provider information was required but absent.

## 9. Security & compliance
- `Provider` is provenance metadata and does not introduce new user-controlled input.
- The field must not be exposed as a user-settable value in any documented ingestion contract.
- Documentation must avoid implying that callers can spoof provider identity through document payload mutation.

## 10. Testing strategy
The implementation shall be validated through a combination of test updates and new coverage.

Required coverage:

1. update existing `CanonicalDocument` tests impacted by the new required field
2. update existing `CanonicalDocumentBuilder` tests impacted by the new required field
3. add pipeline-level tests covering propagation from queue-read/request-dispatch context through to `CanonicalDocumentBuilder`
4. update any other existing tests that fail because provider context is now mandatory
5. add tests proving newly created canonical documents always contain `Provider`
6. add tests proving `Provider` is immutable after construction
7. add tests covering `ProviderParameters` as the required propagation carrier
8. add tests proving missing provider context fails fast at the earliest opportunity
9. execute a full test suite run after implementation and resolve any failures caused by this change

Provider-specific coverage beyond currently failing or directly impacted tests is not mandated unless needed to demonstrate the propagation contract for existing providers.

## 11. Rollout / migration
- This change applies to newly created canonical documents only.
- No migration, backfill, or historical reindex strategy is required for this work item.
- Existing documents without `Provider` are out of scope for this specification.

## 12. Open questions
None. The required design decisions for this work item have been clarified for this draft.

## Target output path
- `docs/052-provider-canonical-field/spec-domain-provider-canonical-field_v0.01.md`
