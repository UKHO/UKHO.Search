# Work Package 042 — Best Effort Ingestion (IngestionServiceHost uplift)

**Target output path:** `./docs/042-best-effort-ingestion/spec.md`

## 1. Overview

### 1.1 Purpose
Uplift the ingestion host runtime configuration and FileShare provider behavior to support a new **BestEffort** ingestion mode. In BestEffort mode, ingestion messages may legitimately reference batches that do **not** have a corresponding ZIP available for download. In this scenario, ingestion must **continue successfully**, logging the missing ZIP and skipping enrichment steps that depend on ZIP content.

### 1.2 Scope
This work package covers:
- Passing `IngestionMode` into `IngestionServiceHost` via environment variable `ingestionmode`.
- Preserving existing behavior when mode is **Strict**.
- Adjusting FileShare ZIP-dependent enrichment so that **NotFound** (and only missing-zip scenarios) do not fail ingestion when mode is **BestEffort**.

Out of scope:
- Changes to message schema/payload format.
- Changes to queue semantics or upstream producers.
- Reworking the overall onion architecture boundaries beyond the minimal uplift required.

### 1.3 Background / Current Behavior
When running the pipeline in BestEffort mode today, ingestion fails if the ZIP is not present:

```
fail: UKHO.Search.Ingestion.Providers.FileShare.Enrichment.BatchContentEnricher[0]
      Failed to download ZIP from FileShare. BatchId=51a48401-239e-4a3d-b9a7-d7861bd8fe35
      System.InvalidOperationException: Failed to download ZIP from FileShare for batch '51a48401-239e-4a3d-b9a7-d7861bd8fe35': NotFoundHttpError { Message = "The requested resource was not found" }
         at UKHO.Search.Ingestion.Providers.FileShare.Enrichment.FileShareZipDownloader.DownloadZipFileAsync(String batchId, CancellationToken cancellationToken) ...
```

The desired behavior in BestEffort mode is:
- Log that the ZIP could not be downloaded.
- Skip ZIP-dependent enrichment steps.
- Allow ingestion for that payload/batch to complete successfully.

## 2. System Context

### 2.1 Key components
- `IngestionServiceHost` (host process; configuration + DI wiring)
- `UKHO.Search.Ingestion.Providers.FileShare` (provider pipeline nodes for FileShare ingestion)
  - ZIP download/enrichment components (e.g., `FileShareZipDownloader`, `BatchContentEnricher`)

### 2.2 Configuration inputs
- Environment variable: `ingestionmode`
  - Supported values: `Strict`, `BestEffort` (case-insensitive)

### 2.3 Operational context
- In Strict mode, a missing ZIP is treated as an error and ingestion fails (current behavior).
- In BestEffort mode, a missing ZIP is treated as an expected condition for some payloads.

## 3. Proposed Design (High level)

### 3.1 IngestionMode wiring
- `IngestionServiceHost` must read environment variable `ingestionmode`.
- It must translate to an internal `IngestionMode` value used by the ingestion pipeline/provider code.
- Default behavior (if missing/unparseable) must be explicitly specified (see decisions).

### 3.2 BestEffort missing-ZIP handling
- The FileShare provider’s ZIP download/enrichment steps must be mode-aware:
  - **Strict:** keep current behavior (throw/fail when ZIP cannot be downloaded, including `NotFound`).
  - **BestEffort:** if the ZIP download fails because it doesn’t exist (e.g., **HTTP 404 / NotFoundHttpError**), then:
    - Log at an appropriate level with `BatchId`.
    - Mark enrichment as “skipped” (or produce no additional enrichment output).
    - Continue the pipeline so ingestion completes.

### 3.3 Skip semantics
ZIP-dependent steps include (non-exhaustive):
- Steps that parse/extract ZIP content.
- Steps that attach extracted content/keywords to `CanonicalDocument`.

In BestEffort mode, these steps should:
- Either short-circuit early if ZIP content is unavailable, or
- Be guarded so they are not executed unless ZIP content is present.

## 4. Functional Requirements

### FR1 — Environment-driven ingestion mode
- The host must accept ingestion mode via environment variable `ingestionmode`.
- The value must be readable from `IngestionServiceHost` process environment variables.

### FR2 — Strict mode unchanged
- When `ingestionmode=Strict` (or equivalent), missing ZIP content must behave exactly as today (fail ingestion).

### FR3 — BestEffort missing ZIP does not fail ingestion
- When `ingestionmode=BestEffort` (or equivalent), a missing ZIP must not fail ingestion.
- The system must log that the ZIP could not be downloaded.
- ZIP-dependent enrichment must be skipped.

### FR4 — Non-missing failures still fail ingestion
- In BestEffort mode, failures that are not “ZIP missing” (e.g., authorization failures, transient IO errors, corruption) must continue to fail ingestion unless explicitly categorized as skippable.

### FR5 — Observability
- Logs must include key identifiers (at least `BatchId`).
- Logging must clearly communicate:
  - Mode (`Strict` vs `BestEffort`)
  - Outcome (`Downloaded`, `Missing/Skipped`, `Failed`)

## 5. Technical Requirements

### TR1 — Onion architecture boundaries
- Host (`src/Hosts/...`) owns configuration and DI.
- Provider project (`UKHO.Search.Ingestion.Providers.FileShare`) owns FileShare-specific enrichment logic.
- The mode flag must be passed inward (host → provider) without introducing outward references.

### TR2 — Mode propagation
- The mode must be accessible to the ZIP download/enrichment components.
- Preferred mechanisms:
  1. Register an options/config object in the host and inject where needed.
  2. Provide mode via an existing provider entrypoint API (if one exists) and flow it through.

### TR3 — Missing ZIP detection
- Detect the “missing ZIP” condition reliably.
- The current failure shows `NotFoundHttpError` (likely from the FileShare client wrapper). The BestEffort path must treat this as “missing ZIP”.

### TR4 — Minimal behavioral change
- Strict path must remain identical.
- BestEffort should change only the ZIP-not-found behavior; do not mask other errors.

## 6. Detailed Behavior

### 6.1 Environment variable parsing
- Read `ingestionmode` at startup.
- Parsing rules:
  - Case-insensitive.
  - Trim whitespace.

### 6.2 Defaults and validation
- If `ingestionmode` is not set:
  - **Decision required**: default to `Strict` (recommended) to preserve current safety.
- If the value is invalid:
  - Log error/warning and default to `Strict` (recommended), OR
  - Fail-fast at startup (alternative).

### 6.3 BestEffort ZIP download flow
- When attempting to download ZIP for a `BatchId`:
  - If download succeeds: continue as today.
  - If download fails due to NotFound:
    - Log message indicating ZIP not found and enrichment will be skipped.
    - Do not throw.
    - Ensure downstream steps that require ZIP are skipped.

### 6.4 Strict ZIP download flow
- Unchanged from current implementation:
  - NotFound still throws/causes ingestion failure.

## 7. Logging and Telemetry

### 7.1 Logging levels (guidance)
- Strict mode failure: `Error` (existing).
- BestEffort missing ZIP: `Warning` or `Information` (decision), but must be searchable.

### 7.2 Example log messages
- BestEffort missing ZIP:
  - `Zip not found in FileShare; skipping ZIP enrichment. BatchId={BatchId}`

## 8. Security / Compliance
- No new secrets.
- Ensure logs do not leak sensitive information (keep payload contents out of logs; identifiers OK).

## 9. Testing

### 9.1 Unit tests
- Add/adjust tests in the FileShare provider project to validate:
  - Strict mode: NotFound propagates as failure.
  - BestEffort mode: NotFound results in skip and no exception.

### 9.2 Integration tests
- If existing integration test harness exists for ingestion pipeline:
  - Add scenario where a batch references a non-existent ZIP with `ingestionmode=BestEffort`.

## 10. Deployment / Configuration

### 10.1 Host configuration
- Add environment variable `ingestionmode` to the host deployment configuration.

### 10.2 Backward compatibility
- Defaulting to `Strict` preserves existing behavior if the variable is not set.

## 11. Open Questions / Decisions

1. What is the default mode when `ingestionmode` is missing? (Recommended: `Strict`.)
2. What log level should missing ZIP use in BestEffort? (Recommended: `Warning`.)
3. Are there additional “skippable” download errors beyond NotFound (e.g., 410 Gone)?
4. Should BestEffort record a structured metric/counter for skipped ZIP enrichments?
