# Architecture

**Target output path:** `docs/004-ingestion-model-uplift/architecture-ingestion-model-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## Overall Technical Approach

This work package is primarily a **domain + serialization contract** uplift. The implementation is expected to:
- keep request models and JSON serialization logic in the ingestion domain project (`src/UKHO.Search.Ingestion`),
- use `System.Text.Json` for serialization/deserialization,
- enforce invariants and validation at model boundaries (constructor / JSON converter validation),
- provide unit tests as the executable contract for the JSON wire format.

### High-level data flow (conceptual)

```mermaid
flowchart LR
  Producer[Producer (API/Queue publisher)] -->|JSON| Envelope[IngestionRequest envelope]
  Envelope --> Consumer[Ingestion pipeline consumer]
  Consumer --> Domain[Request handlers / indexing processes]
```

- `Producer` and `Consumer` are out of scope for the model uplift, but the envelope is designed so both sides can:
  - read `RequestType`,
  - select the matching one-of payload property,
  - process without runtime type metadata.

## Frontend

No frontend (Blazor) changes are required for this work package.

## Backend

### Domain model placement
- Domain types live in `src/UKHO.Search.Ingestion/Requests/`.
- JSON converters live in `src/UKHO.Search.Ingestion/Requests/Serialization/`.

### Key design choices
- **Polymorphism**: implemented via `RequestType` + one-of named payload properties to avoid reliance on serializer polymorphism features.
- **Null omission**: achieved using `System.Text.Json` ignore condition (`WhenWritingNull`) and/or per-property settings.
- **Contract-as-tests**: `test/UKHO.Search.Ingestion.Tests` provides round-trip and negative tests that lock down the JSON contract.

### Envelope routing
Once deserialized, consuming code is expected to follow a simple routing pattern:
1. switch on `RequestType`.
2. access the corresponding payload property.
3. process the payload.

This supports deterministic behaviour and clear error handling when invariants are broken.
