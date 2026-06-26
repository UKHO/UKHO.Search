# Elasticsearch Ingestion Pipeline Design for Mixed Metadata and Document Content

## Purpose

This document defines a complete ingestion design for a .NET Aspire service that ingests fielded metadata plus associated binary content, transforms that source material into a **canonical search document**, and indexes it into Elasticsearch for both **lexical** and **semantic** retrieval.

The design assumes:

- incoming metadata often starts as a bag of single key/value pairs
- associated files are usually ZIP archives that must be unpacked first
- inner files may include XML, PDF, DOCX, PPTX, XLSX, plain text, and other common formats
- document text extraction will be performed using **Kreuzberg** via its C# binding
- semantic sentence or paragraph generation may be performed through Python via csnakes
- the index format should be **common across all source types**
- the final search experience should feel like a **single Google-style search box** backed by both structured filtering and hybrid lexical/semantic ranking

Kreuzberg is a current multi-format document extraction library that advertises text, metadata, and structured extraction across many formats, and Apache Tika remains the classic Apache project for the same general problem space. citeturn0search2turn0search4

---

## Core Design Principle

Do **not** shape the Elasticsearch index around the raw source schema.

Instead, shape the index around a **canonical search model**:

1. preserve the original source for traceability
2. normalize raw values into typed canonical fields
3. enrich those fields from XML content, static reference data, and geo/reference lookups
4. project the result into one or more **search surfaces**
5. index the final canonical document

This is the key move from a **bag of fields** to a **coherent search record**.

Elasticsearch supports explicit field typing for strings, numbers, dates, objects, geo values, and vectors/semantic text. Strings can be indexed both as `text` and `keyword` depending on search needs. citeturn0search0turn1search22

---

## End-to-End Pipeline Overview

```text
Inbound item
  -> Read metadata envelope
  -> Unpack ZIP archive
  -> Inspect contained files
  -> Extract XML facts
  -> Extract text/metadata from PDFs, Word docs, etc. via Kreuzberg
  -> Normalize to canonical names and types
  -> Add static reference explanations
  -> Add geo/reference enrichments
  -> Build lexical projection
  -> Build semantic projection
  -> Validate canonical document
  -> Index into Elasticsearch
```

At a high level, the pipeline should be implemented as a set of deterministic stages. Each stage should accept a strongly typed input object and return a richer strongly typed output object. This keeps the design testable and makes failures easy to isolate.

---

## Pipeline Stages in Detail

## 1. Intake

### Inputs

- source metadata record
- path or stream for the associated binary payload
- source-system identity
- ingestion timestamp
- correlation identifiers

### Responsibilities

- assign a stable ingestion ID
- record provenance
- detect content type of the outer payload
- persist the original payload to durable storage if audit/replay is required

### Output

An `IngestionEnvelope` that carries all intake metadata and a handle to the raw payload.

### Notes

The original source should remain available for replay and forensic use, but it should not be the primary search surface.

---

## 2. Archive Unpacking

The binary payload is usually a ZIP archive, so unpacking is an explicit stage in the pipeline.

### Responsibilities

- enumerate archive entries
- capture entry paths, sizes, CRC/checksum where available, media type, and modified timestamps
- reject or quarantine encrypted/corrupt/unexpected archive structures
- extract nested XML and office/document files to a working area
- detect nested archives if the business process permits them

### Recommended output model

```csharp
public sealed record ArchiveEntryDescriptor(
    string EntryPath,
    string FileName,
    string MediaType,
    long Size,
    string? Sha256,
    Stream Content);
```

### Design guidance

Treat each unpacked file as a potential enrichment source. Do not index each unpacked file as an independent Elasticsearch document unless your business model explicitly requires that. In most cases, the archive contributes evidence to a single canonical search document.

---

## 3. File Classification

Each unpacked file should be classified before extraction.

### Typical classes

- XML / structured text
- PDF
- DOCX
- PPTX
- XLSX
- TXT / CSV / JSON
- images
- unsupported / ignored

### Responsibilities

- infer actual media type from content, not just file extension
- route files to the appropriate extraction path
- decide whether the file contributes only metadata, only text, or both

### Output

A list of `ClassifiedFile` records containing media type, extraction strategy, and priority.

---

## 4. Structured Extraction from XML

XML files are especially valuable because they often contain **structured facts**, not just unstructured text.

### Responsibilities

- parse XML safely
- map source-specific element/attribute names to canonical field names
- extract business facts, identifiers, codes, dates, entities, locations, statuses, amounts, and relationships
- optionally capture a short XML-derived prose summary for search enrichment

### Example

Source XML values:

- `<countryCode>GB</countryCode>`
- `<status>A1</status>`
- `<eventDate>2026-03-08T10:00:00Z</eventDate>`

Canonicalized result:

```json
{
  "normalized": {
    "codes": {
      "countryCode": "GB",
      "statusCode": "A1"
    },
    "dates": {
      "eventAt": "2026-03-08T10:00:00Z"
    }
  }
}
```

### Important rule

XML extraction should produce both:

- **typed facts** for filtering and exact queries
- **prose summaries** for free-text and semantic search

---

## 5. Binary Document Text Extraction

PDF, Word, PowerPoint, and similar files should be treated as text/metadata enrichment sources.

Kreuzberg is suitable here because it is designed as a broad document extraction layer across many file families. Apache Tika is the classic Java-centric alternative for the same job. citeturn0search2turn0search4

### Responsibilities

- extract textual content
- extract file metadata where available, such as title, author, language, page count, subject, and creation time
- emit warnings for image-only or poor-quality files
- preserve section/page/chunk boundaries if the library exposes them

### Recommended extracted representation

```csharp
public sealed record ExtractedDocument(
    string FileName,
    string MediaType,
    string? Title,
    string? Author,
    string? Language,
    string Text,
    IReadOnlyList<ExtractedChunk> Chunks,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string> Metadata);
```

### PDF caveat

Text extraction quality varies. Some PDFs contain embedded selectable text and extract cleanly; others are effectively images and need OCR. This should be surfaced as a warning in the pipeline so downstream enrichment and ranking can respond appropriately.

---

## 6. Static Reference Enrichment

Some raw field values have well-known business meanings. Those meanings should be added explicitly rather than left implicit.

### Examples

- `GB` -> `United Kingdom`
- `A1` -> `Active record`
- `X17` -> `Deferred customs release`

### Responsibilities

- resolve known codes to labels
- resolve labels to longer explanations
- add aliases and synonyms where search benefit exists
- keep the raw code as the system of record

### Output shape

```json
{
  "normalized": {
    "codes": {
      "countryCode": "GB",
      "statusCode": "A1"
    },
    "labels": {
      "countryName": "United Kingdom",
      "statusDescription": "Active record"
    }
  },
  "descriptions": {
    "fieldValueExplanations": [
      "Country code GB means United Kingdom.",
      "Status code A1 means Active record."
    ]
  }
}
```

This explanatory layer is especially important for semantic search because it turns terse system values into human meaning.

---

## 7. Geo and Other External Enrichment

When locations, IP addresses, ports, site codes, or other referenceable values appear in the input, enrich them into human-readable form.

### Typical geo enrichments

- country name
- region name
- city name
- latitude/longitude
- ISO code normalization

Elasticsearch supports `geo_point` for geographic indexing and querying. citeturn0search0

### Typical external enrichments

- party or organization display names
- product names
- jurisdiction names
- operational status descriptions
- taxonomy labels

Elasticsearch ingest pipelines support sequential processing and enrich processors that can add fields from enrich indices during ingest. citeturn1search0turn1search2

For this pipeline, however, enrichments may be performed in application code before indexing if that gives better control and easier debugging.

---

## 8. Canonicalization

This is the most important structural stage in the pipeline.

### Goal

Map heterogeneous source inputs into a stable internal schema.

### Example of source variance

Different sources may express the same concept as:

- `cntry_cd`
- `country`
- `countryCode`
- `/Envelope/Header/Country`

All of these should map into one canonical location such as:

```json
{
  "normalized": {
    "codes": {
      "countryCode": "GB"
    }
  }
}
```

### Canonicalization rules

- one business concept -> one canonical field
- preserve the raw original field elsewhere for audit/debug
- convert to the strongest possible type early
- normalize casing, whitespace, date formats, and code formats consistently
- reject or quarantine impossible values rather than silently accepting them

---

## 9. Lexical Projection

The canonical record now needs to be projected into a form that works well for ordinary term-based search.

### Main idea

Build a dedicated text surface such as `search.all_text` that contains the most searchable content from all enrichment sources.

Elasticsearch supports `copy_to` to combine values from multiple fields into a grouped field, and `combined_fields` to search multiple text fields as if they were one. Elastic explicitly recommends reducing the number of searched fields when appropriate. citeturn1search0turn1search3

### What should feed `search.all_text`

- important identifiers that users may type
- labels and display names
- expanded code meanings
- XML-derived prose summaries
- extracted document text or a curated subset of it
- aliases and synonyms
- titles/headlines generated by the pipeline

### What should usually not feed `search.all_text`

- noisy machine-only fields
- large blobs that add no retrieval value
- duplicate low-signal text repeated many times
- confidential text that should not be searchable

### Example lexical field

```text
GB United Kingdom Active record shipment arrival London consignee Acme Imports quarterly report customs declaration
```

This field does not need to read nicely. It needs to maximize lexical recall.

---

## 10. Semantic Projection

This stage transforms the canonical facts into one or more readable narrative sentences or paragraphs.

### Goal

Produce text that expresses **meaning and relationships**, not just tokens.

Elasticsearch recommends the `semantic_text` workflow as the simplest way to add semantic search because it automates much of the manual setup otherwise required for vector-based retrieval. citeturn1search1turn1search20

### Example semantic text

```text
This record describes an active shipment arrival event in London, United Kingdom. The consignee is Acme Imports. Country code GB corresponds to the United Kingdom, and status code A1 means the record is active.
```

### Recommended first implementation

Use deterministic templates rather than free-form generation.

Example template:

```text
This {documentType} record describes {statusDescription} activity in {locationDescription}. 
The main entity is {primaryEntity}. 
The attached documents discuss {documentSummary}. 
Important source values include {valueExplanations}.
```

### Why deterministic templates first

- predictable
- testable
- easier to diff between runs
- lower risk of hallucinated content
- easier to explain to stakeholders

A more advanced generated narrative can be added later once the core indexing model is stable.

---

## 11. Validation

Before indexing, validate the canonical document.

### Validation checks

- required identifiers present
- canonical types valid
- semantic text not empty when semantically relevant content exists
- lexical field not unbounded in size
- prohibited fields excluded from search surfaces
- geo values valid
- array sizes within operational limits

### Failure policy

Choose one of:

- reject entire document
- index partial document with warnings
- send to dead-letter queue for review

For most ingestion systems, a warning-bearing partial document is acceptable only if the core identity and source provenance are intact.

---

## 12. Indexing

At this stage, the canonical JSON is written to Elasticsearch.

The `_source` field in Elasticsearch stores the original JSON body that was indexed, but `_source` itself is not searchable. This is one reason the canonical document must already contain explicit search surfaces such as typed fields, lexical fields, and semantic fields. citeturn0search6

---

# Canonical Elasticsearch Document Structure

The canonical search document should be stable across all ingestion sources.

## Top-Level Sections

```json
{
  "documentId": "...",
  "source": { },
  "normalized": { },
  "descriptions": { },
  "search": { },
  "quality": { },
  "provenance": { }
}
```

Each section has a specific purpose.

## 1. `documentId`

Stable unique identifier for the canonical record.

### Requirements

- deterministic where possible
- unique across reprocessing
- suitable as Elasticsearch `_id`

## 2. `source`

Contains preserved raw material and extraction summaries.

### Suggested shape

```json
{
  "source": {
    "rawFields": {
      "countryCode": "GB",
      "statusCode": "A1"
    },
    "archive": {
      "fileName": "payload.zip",
      "entryCount": 4
    },
    "files": [
      {
        "path": "docs/report.pdf",
        "mediaType": "application/pdf",
        "sha256": "..."
      }
    ],
    "xmlExtracted": {
      "...": "..."
    }
  }
}
```

If `rawFields` are highly variable and sparse, Elasticsearch `flattened` is a good fit because it stores the whole object as one field and indexes its leaf values as keywords, avoiding uncontrolled mapping growth. citeturn1search2turn1search11

## 4. `normalized`

The typed system-of-record view used for exact matching, filters, aggregations, and ranking features.

### Suggested subsections

- `ids`
- `codes`
- `labels`
- `dates`
- `numbers`
- `entities`
- `geo`
- `classifications`

### Example

```json
{
  "normalized": {
    "ids": {
      "submissionId": "SUB-2026-000123"
    },
    "codes": {
      "countryCode": "GB",
      "statusCode": "A1"
    },
    "labels": {
      "countryName": "United Kingdom",
      "statusDescription": "Active record"
    },
    "dates": {
      "eventAt": "2026-03-08T10:00:00Z"
    },
    "geo": {
      "location": {
        "lat": 51.5074,
        "lon": -0.1278
      },
      "cityName": "London",
      "regionName": "England"
    }
  }
}
```

## 5. `descriptions`

Human-readable explanatory content that is not merely raw document text.

### Suggested content

- code explanations
- XML-derived summaries
- business-friendly labels
- reference data descriptions
- extracted file summaries
- warnings that matter for interpretation

### Example

```json
{
  "descriptions": {
    "fieldValueExplanations": [
      "Country code GB means United Kingdom.",
      "Status code A1 means Active record."
    ],
    "xmlSummary": [
      "The XML records an arrival event in London.",
      "The XML identifies the consignee as Acme Imports."
    ],
    "fileSummaries": [
      "A PDF report discusses quarterly shipping activity and customs processing."
    ]
  }
}
```

## 6. `search`

The retrieval-focused surface.

### Suggested fields

- `title`
- `all_text`
- `semantic_text`
- optional `keywords`
- optional `document_text_excerpt`

### Example

```json
{
  "search": {
    "title": "Active shipment arrival in London, United Kingdom",
    "all_text": "GB United Kingdom Active record shipment arrival London consignee Acme Imports quarterly shipping customs declaration",
    "semantic_text": "This record describes an active shipment arrival event in London, United Kingdom. The consignee is Acme Imports. A PDF report discusses quarterly shipping activity and customs processing."
  }
}
```

## 7. `facets`

A curated set of fields intended specifically for filtering and faceting in the UI.

### Why separate from `normalized`

You may choose to expose only a subset of normalized fields in the UI. `facets` makes that explicit.

### Example

```json
{
  "facets": {
    "documentType": "shipment-event",
    "countryCode": "GB",
    "statusCode": "A1",
    "cityName": "London"
  }
}
```

## 8. `quality`

Operational and extraction-quality indicators.

### Example fields

- `hasTextExtractionWarnings`
- `isScannedPdf`
- `ocrApplied`
- `semanticProjectionVersion`
- `extractionConfidence`

### Purpose

- troubleshooting
- analytics
- routing low-quality content for later repair

## 9. `provenance`

Tracks where the canonical record came from.

### Example fields

- source system name
- source message ID
- archive checksum
- processing timestamp
- pipeline version
- extraction library version

This section is invaluable when you evolve the pipeline and need to compare behavior across versions.

---

# Recommended Elasticsearch Mapping Strategy

## Guiding Rules

- use explicit mappings for canonical fields
- use `keyword` for exact identifiers, codes, and facet values
- use `text` for prose and free-text search
- use multi-fields where the same logical field needs both exact and analyzed behavior
- use `flattened` for highly variable raw source bags
- use `geo_point` for latitude/longitude
- use `semantic_text` for the narrative semantic field

Elastic documents that strings can be indexed as `text` or `keyword`, and multi-fields allow a field to be indexed in multiple ways. `flattened` is specifically intended for unknown or variable subfields. `semantic_text` simplifies semantic search ingestion and querying. citeturn0search0turn1search22turn1search2turn1search1

## Example Mapping

```json
{
  "mappings": {
    "properties": {
      "documentId": { "type": "keyword" },
      "documentType": { "type": "keyword" },

      "source": {
        "properties": {
          "rawFields": { "type": "flattened" },
          "xmlExtracted": { "type": "flattened" },
          "archive": {
            "properties": {
              "fileName": { "type": "keyword" },
              "entryCount": { "type": "integer" }
            }
          },
          "files": {
            "properties": {
              "path": { "type": "keyword" },
              "mediaType": { "type": "keyword" },
              "sha256": { "type": "keyword" }
            }
          }
        }
      },

      "normalized": {
        "properties": {
          "ids": {
            "properties": {
              "submissionId": { "type": "keyword" }
            }
          },
          "codes": {
            "properties": {
              "countryCode": { "type": "keyword" },
              "statusCode": { "type": "keyword" }
            }
          },
          "labels": {
            "properties": {
              "countryName": {
                "type": "text",
                "fields": {
                  "raw": { "type": "keyword" }
                }
              },
              "statusDescription": {
                "type": "text",
                "fields": {
                  "raw": { "type": "keyword" }
                }
              }
            }
          },
          "dates": {
            "properties": {
              "eventAt": { "type": "date" }
            }
          },
          "geo": {
            "properties": {
              "location": { "type": "geo_point" },
              "cityName": {
                "type": "text",
                "fields": {
                  "raw": { "type": "keyword" }
                }
              },
              "regionName": {
                "type": "text",
                "fields": {
                  "raw": { "type": "keyword" }
                }
              }
            }
          }
        }
      },

      "descriptions": {
        "properties": {
          "fieldValueExplanations": { "type": "text" },
          "xmlSummary": { "type": "text" },
          "fileSummaries": { "type": "text" }
        }
      },

      "search": {
        "properties": {
          "title": {
            "type": "text",
            "fields": {
              "raw": { "type": "keyword" }
            }
          },
          "all_text": { "type": "text" },
          "semantic_text": { "type": "semantic_text" }
        }
      },

      "facets": {
        "properties": {
          "documentType": { "type": "keyword" },
          "countryCode": { "type": "keyword" },
          "statusCode": { "type": "keyword" },
          "cityName": { "type": "keyword" }
        }
      },

      "quality": {
        "properties": {
          "hasTextExtractionWarnings": { "type": "boolean" },
          "isScannedPdf": { "type": "boolean" },
          "ocrApplied": { "type": "boolean" },
          "semanticProjectionVersion": { "type": "keyword" },
          "extractionConfidence": { "type": "float" }
        }
      },

      "provenance": {
        "properties": {
          "sourceSystem": { "type": "keyword" },
          "sourceMessageId": { "type": "keyword" },
          "archiveSha256": { "type": "keyword" },
          "processedAt": { "type": "date" },
          "pipelineVersion": { "type": "keyword" },
          "extractorVersion": { "type": "keyword" }
        }
      }
    }
  }
}
```

---

# Why `flattened` is Useful Here

Your raw metadata can be sparse, variable, and source-dependent. Mapping every possible raw key as a first-class field can cause uncontrolled mapping growth and operational pain. The `flattened` field type addresses this by storing an entire object as one field and indexing leaf values as keywords. That makes it suitable for:

- unpredictable source field names
- raw source preservation
- occasional exact lookup on raw values
- avoiding mapping explosion

It is **not** the right surface for primary free-text search because its leaf values are treated like keywords rather than analyzed prose. citeturn1search2

---

# Why `semantic_text` is Preferred for the First Version

Elastic recommends the `semantic_text` workflow as the simplest semantic-search path because it automates much of the vector plumbing, ingestion-time inference, and default chunking behavior that would otherwise need to be configured manually. citeturn1search1turn1search13turn1search20

For this ingestion service, `semantic_text` is a good fit because:

- your pipeline is already generating a semantic narrative field
- you want one common indexing pattern across all source types
- you want hybrid search without first building a bespoke embedding infrastructure

If later you need custom embeddings, external model control, or specialized chunking/ranking, you can revisit a lower-level `dense_vector` design.

---

# Query Model for the Single Search Box

The single search box should target the **search projections**, not the raw source bag.

## Lexical query path

Primary targets:

- `search.title`
- `search.all_text`
- `descriptions.fieldValueExplanations`
- `descriptions.xmlSummary`
- `descriptions.fileSummaries`

Use either:

- a single grouped field like `search.all_text`, optionally built with `copy_to`, or
- several text fields queried with `combined_fields`

Elastic documents both patterns. `copy_to` reduces the number of fields searched, while `combined_fields` searches multiple text fields as if they were a single combined field. citeturn1search0turn1search3

## Semantic query path

Primary target:

- `search.semantic_text`

## Hybrid ranking

Run lexical and semantic retrieval together and fuse them. Elastic documents hybrid search using `semantic_text`. citeturn1search16

### Conceptual example

```json
{
  "retriever": {
    "rrf": {
      "retrievers": [
        {
          "standard": {
            "query": {
              "combined_fields": {
                "query": "uk active london shipment",
                "fields": [
                  "search.title",
                  "search.all_text",
                  "descriptions.fieldValueExplanations",
                  "descriptions.xmlSummary",
                  "descriptions.fileSummaries"
                ]
              }
            }
          }
        },
        {
          "standard": {
            "query": {
              "semantic": {
                "field": "search.semantic_text",
                "query": "uk active london shipment"
              }
            }
          }
        }
      ]
    }
  }
}
```

This is the retrieval model that makes the Google-like single-box experience possible.

---

# Recommended .NET Service Architecture

## Suggested pipeline interfaces

```csharp
public interface IArchiveUnpacker
{
    Task<IReadOnlyList<ArchiveEntryDescriptor>> UnpackAsync(Stream archive, CancellationToken cancellationToken);
}

public interface IStructuredExtractor
{
    Task<StructuredExtractionResult> ExtractAsync(IReadOnlyList<ArchiveEntryDescriptor> files, CancellationToken cancellationToken);
}

public interface IDocumentTextExtractor
{
    Task<IReadOnlyList<ExtractedDocument>> ExtractAsync(IReadOnlyList<ArchiveEntryDescriptor> files, CancellationToken cancellationToken);
}

public interface ICanonicalizer
{
    Task<CanonicalWorkItem> CanonicalizeAsync(RawIngestionModel raw, CancellationToken cancellationToken);
}

public interface IReferenceEnricher
{
    Task<CanonicalWorkItem> EnrichAsync(CanonicalWorkItem item, CancellationToken cancellationToken);
}

public interface ILexicalProjectionBuilder
{
    string BuildAllText(CanonicalWorkItem item);
    string BuildTitle(CanonicalWorkItem item);
}

public interface ISemanticProjectionBuilder
{
    Task<string> BuildSemanticTextAsync(CanonicalWorkItem item, CancellationToken cancellationToken);
}

public interface ICanonicalDocumentIndexer
{
    Task IndexAsync(CanonicalIndexDocument document, CancellationToken cancellationToken);
}
```

## Suggested orchestration sequence

```csharp
IngestionEnvelope
  -> archive unpacker
  -> structured extractor
  -> document text extractor (Kreuzberg binding)
  -> canonicalizer
  -> reference/geo enrichers
  -> lexical projection builder
  -> semantic projection builder
  -> validator
  -> Elasticsearch indexer
```

This design keeps the pipeline composable and testable.

---

# Recommended Canonical DTO Shape in .NET

```csharp
public sealed record CanonicalIndexDocument(
    string DocumentId,
    string DocumentType,
    SourceSection Source,
    NormalizedSection Normalized,
    DescriptionSection Descriptions,
    SearchSection Search,
    FacetSection Facets,
    QualitySection Quality,
    ProvenanceSection Provenance);
```

With nested records for each section. The shape of the DTO should mirror the final Elasticsearch `_source` exactly. That makes serialization deterministic and avoids drift between code and mapping.

---

# Treatment of Large Extracted Text

Large PDFs or Office documents can produce very large extracted bodies. Do not automatically dump all extracted text directly into every search field.

## Recommended strategy

Store:

- a curated `fileSummaries` value for high-signal text
- an optional `document_text_excerpt` if useful
- a semantic summary derived from the extracted text

Consider omitting or trimming the full extracted body unless there is a strong product need to preserve it in Elasticsearch. Since `_source` stores the original indexed JSON body, retaining very large text fields directly in the indexed document can increase storage costs and affect operational behavior. citeturn0search6

If you need full-body retention, consider keeping the full extracted text in external storage and indexing only curated or chunked representations in Elasticsearch.

---

# Versioning and Evolution

This pipeline will evolve. Plan for that from day one.

## Fields to version explicitly

- pipeline version
- canonical schema version
- semantic projection version
- extractor version
- static reference dataset version

## Why this matters

When search relevance changes, you need to know whether it changed because:

- mappings changed
- extraction behavior changed
- semantic narrative templates changed
- reference data changed
- ZIP/XML parsing changed

Without version markers, debugging relevance regressions becomes difficult.

---

# Recommended First Production Scope

A sensible first release would include:

1. ZIP unpacking
2. XML fact extraction
3. document text extraction through Kreuzberg C# binding
4. canonical normalization to stable typed fields
5. static code/value enrichment
6. geo enrichment where applicable
7. deterministic `search.title`
8. deterministic `search.all_text`
9. deterministic `search.semantic_text`
10. explicit Elasticsearch mappings using `keyword`, `text`, `flattened`, `geo_point`, and `semantic_text`
11. hybrid search over lexical plus semantic projections

This gives you a strong search foundation without overcommitting to advanced LLM-driven summarization too early.

---

# Final Architectural Summary

The ingestion service should be designed around a **canonical search document** that is common across all source types.

ZIP archives, XML files, PDFs, Word documents, and static reference data are all simply **inputs** to that canonical projection.

The canonical document should contain:

- preserved raw source material for traceability
- normalized typed fields for exact retrieval and filtering
- descriptive explanatory text for human meaning
- a lexical search field for ordinary term-based search
- a semantic narrative field for meaning-based retrieval
- provenance and quality metadata for operations and evolution

That model is what turns heterogeneous ingested files into a search experience that behaves like a single text box while still preserving the power of structured Elasticsearch queries.

---

# References

- Elastic field data types and typing guidance. citeturn0search0
- Elastic `copy_to` reference. citeturn1search0
- Elastic `semantic_text` reference and semantic search guidance. citeturn1search1turn1search13turn1search20
- Elastic `flattened` field type reference. citeturn1search2
- Elastic `combined_fields` query reference. citeturn1search3
- Elastic `_source` field behavior. citeturn0search6
- Elastic multi-fields reference. citeturn1search22
- Elastic hybrid search with `semantic_text`. citeturn1search16
- Apache Tika overview. citeturn0search4
- Kreuzberg package information. citeturn0search2
