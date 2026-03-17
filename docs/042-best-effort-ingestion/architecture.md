# Architecture

**Target output path:** `./docs/042-best-effort-ingestion/architecture.md`

## Overall Technical Approach
- The ingestion runtime mode is configured at deploy/run time via environment variable `ingestionmode`.
- The ingestion host resolves this to an internal `IngestionMode` value at startup and registers it as DI-resolvable options.
- The FileShare provider enrichment pipeline consumes the configured mode.
  - **Strict** mode preserves existing failure semantics.
  - **BestEffort** mode treats FileShare ZIP `NotFound` as an expected condition, logs it, and skips ZIP-dependent enrichment steps while allowing ingestion to succeed.

```mermaid
flowchart LR
  env[(Environment
"ingestionmode")] --> host[IngestionServiceHost
(Startup/DI)]
  host --> opt[IngestionOptions
(IngestionMode)]
  opt --> provider[UKHO.Search.Ingestion.Providers.FileShare
Enrichment Pipeline]

  provider --> zip[FileShare ZIP download]
  zip -->|Found| enrich[ZIP-dependent enrichment steps]
  zip -->|NotFound + BestEffort| skip[Log + skip ZIP enrichment]
  zip -->|NotFound + Strict| fail[Throw/fail ingestion]
  zip -->|Other errors| fail
```

## Frontend
- Not applicable (no Blazor/UI changes).

## Backend

### Hosts
- Responsibilities:
  - Read and parse `ingestionmode`.
  - Default to `Strict` when missing/invalid.
  - Register resolved mode into DI (options) so provider components can make runtime decisions.

### Provider: `UKHO.Search.Ingestion.Providers.FileShare`
- Responsibilities:
  - Attempt ZIP download using the existing FileShare client.
  - Detect the missing ZIP condition (`NotFoundHttpError` observed today).
  - In BestEffort:
    - Log missing ZIP with `BatchId`.
    - Short-circuit/skip any enrichment steps that require ZIP content.
  - In Strict:
    - Keep current exception behavior and logging.
  - For non-missing failures:
    - Continue to fail ingestion in both modes (unless later explicitly categorized as skippable).

### Tests
- Provider unit tests validate:
  - Strict mode: NotFound still fails.
  - BestEffort mode: NotFound is skipped without throwing.
  - BestEffort mode: non-NotFound errors still fail.
- Integration test (if harness exists) validates end-to-end ingestion success with missing ZIP when running in BestEffort.
