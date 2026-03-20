# Architecture

**Target output path:** `docs/052-provider-canonical-field/architecture-ingestion-provider-canonical-field_v0.01.md`

**Based on:** `docs/052-provider-canonical-field/spec-domain-provider-canonical-field_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Overall Technical Approach

This change is a backend ingestion-pipeline uplift that adds provider provenance to the canonical search contract without weakening the repository's provider-agnostic pipeline design.

It follows the existing onion architecture:

- **Domain**: `UKHO.Search.Ingestion` owns `CanonicalDocument` and the provider-agnostic ingestion pipeline abstractions.
- **Provider**: `UKHO.Search.Ingestion.Providers.FileShare` owns File Share-specific queue/request handling and canonical document construction inputs.
- **Infrastructure**: `UKHO.Search.Infrastructure.Ingestion` owns Elasticsearch projection and index mapping.
- **Hosts**: hosted-service and DI wiring remain orchestration only.

The central design choice is to introduce a mandatory `ProviderParameters` carrier that moves provider-scoped values through generic pipeline stages as data. This preserves node generality while ensuring `CanonicalDocumentBuilder` always receives the provider identifier needed to stamp an immutable `Provider` field.

### High-level data flow

```mermaid
flowchart LR
    Q[Queue message read] --> R[Provider identity resolved]
    R --> P[ProviderParameters]
    P --> D[Request dispatch / generic pipeline flow]
    D --> B[CanonicalDocumentBuilder]
    B --> C[CanonicalDocument with immutable Provider]
    C --> E[CanonicalIndexDocument]
    E --> I[Canonical index mapping provider as keyword]
```

### Technical principles

- `Provider` is system-managed provenance metadata, not user-authored content.
- `Provider` is required for all newly created canonical documents.
- `Provider` is set at construction time only.
- Missing provider context is treated as a contract violation and fails fast at the earliest opportunity.
- Provider context is propagated through generic node contracts rather than encoded into provider-specific node abstractions.

---

## Frontend

No frontend or Blazor changes are required for this work package.

The repository contains a Blazor project, but this feature is confined to ingestion, canonical-model, index-mapping, test, and documentation concerns.

---

## Backend

### Canonical model

`CanonicalDocument` is extended with a required immutable `Provider` property. The property becomes part of the provider-independent canonical contract so downstream indexing and diagnostics can reason about provenance directly from the canonical document.

Responsibilities:

- expose `Provider` as part of the canonical model
- prevent post-construction mutation
- ensure serialization and test fixtures treat it as mandatory

### Provider parameter propagation

`ProviderParameters` is introduced as the mandatory generic carrier for provider-scoped values.

Responsibilities:

- capture provider identity when the ingestion request is read from the queue
- move that identity through the pipeline without altering generic node semantics
- provide a future-ready place for additional provider-scoped values if similar propagation needs arise

### Canonical document construction

`CanonicalDocumentBuilder` becomes the point where propagated provider context is bound onto the canonical model.

Responsibilities:

- require provider context as an input
- reject construction if provider context is absent
- stamp the canonical document with the existing provider identifier, for example `file-share`

### Index projection and mapping

Infrastructure updates ensure the canonical index contract exposes provider provenance consistently.

Responsibilities:

- include `Provider` in any canonical-to-index projection as required
- map `provider` as a `keyword` field in the canonical index definition
- keep the field suitable for exact-match filtering and provider provenance inspection

### Documentation and wiki

Repository documentation updates clarify the purpose and ownership of the new field.

Responsibilities:

- explain that `Provider` is system-managed
- explain that it comes from queue/provider context rather than user payload data
- explain why callers cannot set it directly
- align canonical model and ingestion pipeline wiki pages with the implemented flow

### Validation and regression strategy

Testing is part of the architecture for this change because the new field becomes a mandatory invariant.

Coverage areas:

- `CanonicalDocument` construction and immutability
- `CanonicalDocumentBuilder` requirements
- queue-read/request-dispatch propagation into builder inputs
- fail-fast behavior for missing provider context
- index projection and mapping compatibility
- full repository test-suite validation
