# Specification: S-101 parsing refactor into `S101Parser`

- **Work package**: `025-s101-parser`
- **Document**: `docs/025-s101-parser/spec-s101-parser_v0.01.md`
- **Version**: `v0.01`
- **Date**: 2026-03-12

## 1. Overview
The repository currently contains S-100 batch handling logic in `S100BatchContentHandler`. As S-1xx product support expands, the handler risks becoming overly complex.

This work package introduces a lightweight internal parser partitioning approach:
- Move S-101 specific parsing logic into a new/internal `S101Parser` class.
- Introduce a minimal `IS100Parser` interface implemented by `S101Parser`.
- `S100BatchContentHandler` delegates S-101 parsing to `S101Parser`.

This is intended as a *code partitioning strategy* only. Parsers are not DI-managed.

## 2. Scope
### In scope
1. Factor out the S-101 parsing code from `S100BatchContentHandler` into a suitable method on `S101Parser`.
2. Update `S100BatchContentHandler` to call the `S101Parser` method.
3. Create a suitable `IS100Parser` interface implemented by `S101Parser`.
4. Do **not** register parsers with DI; parsers are internal/lightweight and can be created with `new`.
5. Create/refactor tests accordingly.

### Out of scope
- Adding support for other S-1xx product types beyond S-101.
- Changing canonical document schema/index mapping.
- Introducing a plugin model, dynamic discovery, or DI registration for parsers.

## 3. Requirements
### 3.1 Functional requirements
- **FR1**: When a `catalog.xml` is present in an extracted batch, S-101 parsing/enrichment MUST be executed by `S101Parser`, invoked from `S100BatchContentHandler`.
- **FR2**: S-101 parsing logic MUST be removed from `S100BatchContentHandler` and isolated within `S101Parser`.
- **FR3**: Only S-101 catalogues MUST be enriched (gated by the product specification value in the XML).

### 3.2 Technical requirements
- **TR1**: Introduce an internal `IS100Parser` interface.
- **TR2**: `S101Parser` MUST implement `IS100Parser`.
- **TR3**: Parsers MUST NOT be registered with DI.
- **TR4**: `S100BatchContentHandler` MUST instantiate parser(s) directly (e.g., `new S101Parser(...)`).
- **TR5**: The refactor MUST preserve existing parsing behaviour (namespaces, enrichment mapping, error handling).
- **TR6**: Parser API SHOULD be small and aligned to handler needs; avoid premature generalisation.

### 3.3 Non-functional requirements
- **NFR1**: The ingestion pipeline MUST NOT fail the whole enrichment step due to S-101 parsing issues; errors should be handled gracefully (log + continue).
- **NFR2**: The refactor MUST simplify handler complexity and improve maintainability.

## 4. High-level design
### 4.1 Components impacted
- `S100BatchContentHandler`
  - continues to locate/select `catalog.xml`.
  - delegates parsing to an `IS100Parser` implementation.

- `S101Parser`
  - contains all S-101-specific XML parsing and `CanonicalDocument` enrichment.

- `IS100Parser`
  - minimal abstraction for S-100-family parsers.

### 4.2 Parser invocation model (no DI)
- `S100BatchContentHandler` directly creates the parser instance (e.g., once per `HandleFiles` invocation).
- No service registration or DI wiring is introduced for the parser.

### 4.3 Public surface / visibility
- `IS100Parser` and `S101Parser` SHOULD be `internal` and live within the file-share ingestion provider project unless cross-project reuse becomes necessary.

## 5. Detailed design
### 5.1 `IS100Parser`
A lightweight interface for catalogue parsing.

Suggested shape (final signature may vary based on existing handler flow):
- Input(s): the selected catalogue path (or stream/content), plus `CanonicalDocument` to enrich.
- Output: optional boolean indicating whether enrichment was applied, or void with side effects on the document.

### 5.2 `S101Parser` factoring
- Create a method responsible for:
  1. Loading/parsing `catalog.xml`.
  2. Determining whether the `catalog.xml` pertains to S-101.
  3. Applying enrichments to `CanonicalDocument`.

### 5.3 Error handling
- Parsing exceptions (excluding cancellation) should be caught and logged.
- Invalid/partial XML content should not prevent other enrichers from operating.

## 6. Testing
### 6.1 Refactor expectations
Tests should continue to validate outcomes observed on `CanonicalDocument`, not internal methods.

### 6.2 Test cases (minimum)
1. **S-101 catalogue enriches expected fields**
   - Given a valid S-101 `catalog.xml`
   - Expect keywords/search text/polygons match prior behaviour.

2. **Non S-101 catalogue does not enrich**
   - Given a `catalog.xml` where product spec is not S-101
   - Expect no S-101 keywords/search text/polygons added.

3. **Invalid polygon data is handled**
   - Given a catalogue with invalid `gml:posList`
   - Expect no exception; other enrichments still applied.

## 7. Risks / decisions
- **Decision**: no DI registration for parsers.
  - Rationale: parsers are lightweight helpers; this is a code partitioning strategy.

- **Risk**: `IS100Parser` may become too generic too early.
  - Mitigation: keep the interface minimal; revisit when second parser is introduced.

