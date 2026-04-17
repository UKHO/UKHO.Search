# Specification: Query Signal Extraction Rules

**Target output path:** `docs/095-signal-extraction-rules/spec-domain-query-signal-extraction-rules_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

### 1.1 Purpose

This specification defines the query-side signal extraction model for search in `UKHO.Search`.

The goal is to move from a free-form user query string to a deterministic Elasticsearch query against the canonical index by introducing a query-side normalization pipeline, a rule-driven signal extraction stage, a typed query plan contract, and a configuration-backed rule loading model.

This work covers the query-side equivalent of the existing ingestion rules approach, but for provider-independent search interpretation rather than document enrichment.

### 1.2 Scope

This specification covers:

- preprocessing of incoming user query text
- typed signal extraction from query text
- use of Microsoft Recognizers for typed entity recognition
- a query plan JSON contract
- a query rule JSON contract
- configuration-backed rule loading from `./rules/query`
- default mapping from remaining query text to canonical search fields
- separation between extracted canonical query model data and execution-time directives such as sort

This specification does not require implementation in this work item.

### 1.3 Stakeholders

- search/domain contributors defining query semantics
- infrastructure contributors implementing Elasticsearch query generation
- host contributors wiring query runtime services into `QueryServiceHost`
- rules authors maintaining repository query rules under `./rules/query`
- future workbench or diagnostics tooling contributors who need to inspect query plans and matched rules

### 1.4 Definitions

- **CanonicalDocument**: the provider-independent indexed document shape produced by ingestion and projected into Elasticsearch
- **Canonical query model**: a query-side model that mirrors the discovery-oriented shape of `CanonicalDocument` without reusing the ingestion CLR type directly
- **Query plan**: the runtime object and JSON representation produced after normalization, typed extraction, rules evaluation, and default mapping, which is then translated into Elasticsearch query DSL
- **Typed signal extraction**: extraction of structured values such as years, dates, numbers, and other recognizer-driven entities from free-form query text
- **Residual text/tokens**: the remaining user query content after rule-matched phrases or tokens have been consumed so they are not also applied by default matching rules
- **Signal extraction rule**: a JSON-authored query rule that inspects query input or extracted signals and emits query-side meaning such as concept expansions, sort hints, consumed phrases, or canonical query model mutations

## 2. System context

### 2.1 Current state

The current repository contains most of the ingestion-side canonical model and rule infrastructure, but the query-side runtime is largely unimplemented.

Observed repository evidence includes:

- `CanonicalDocument` exists in `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
- canonical Elasticsearch projection exists in `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDocument.cs`
- canonical index mapping exists in `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`
- current query infrastructure registration in `src/UKHO.Search.Infrastructure.Query/Injection/InjectionExtensions.cs` is effectively empty
- `QueryServiceHost` currently uses a stub query UI client rather than a real query planning and Elasticsearch execution path
- ingestion rules are already configuration-backed and loaded from namespaced configuration keys, with refresh support and startup logging

The current canonical Elasticsearch index exposes the following relevant fields:

- exact-match `keyword` fields: `keywords`, `authority`, `region`, `format`, `majorVersion`, `minorVersion`, `category`, `series`, `instance`, `securityTokens`, `provider`, `title`
- analyzed `text` fields: `searchText`, `content`

This matters because query-side defaults and rule outputs must align with the actual indexed field behavior.

### 2.2 Proposed state

The proposed query-side search pipeline is:

1. lowercase the incoming query
2. clean the query text, including removal of repeated spaces
3. tokenize and build phrase windows over the cleaned text
4. run typed entity extraction using Microsoft Recognizers behind an abstraction
5. run signal extraction rules stored under `./rules/query`
6. build a query plan containing:
   - input snapshot
   - typed extracted signals
   - canonical query model mutations
   - default matching contributions
   - execution directives such as sort
   - diagnostics such as matched rule identifiers
7. translate that query plan into Elasticsearch query DSL targeting the canonical index

The design must remain provider-independent. Query rules operate on the shared canonical index contract rather than on provider-specific payloads.

### 2.3 Assumptions

- the query side targets the same canonical index contract already defined by ingestion
- query semantics should remain provider-independent
- rules are authored in repository JSON and, in local Aspire mode, are seeded into configuration using hierarchical keys derived from the repository folder structure
- Microsoft Recognizers is included now, but isolated behind an abstraction so the query plan contract does not depend on recognizer-specific object models
- the query runtime may later add richer Elasticsearch scoring and targeting, but this specification defines a stable initial contract now

### 2.4 Constraints

- rules must be stored in `./rules/query/*.json`
- configuration keys must therefore live under the `rules:query:*` namespace
- query rules must be loaded using a similar configuration-backed mechanism to the ingestion rules host startup path, including logging of loaded rules and refresh/reload behavior
- the query plan must preserve a clean separation between canonical query intent and execution-time directives such as sorting
- the query plan must be serializable to JSON without persisting Microsoft Recognizers runtime types directly
- the query-side canonical model must conform in shape and semantics to the discovery half of `CanonicalDocument`, but should not directly reuse the ingestion `CanonicalDocument` CLR type

## 3. Component / service design (high level)

### 3.1 Components

The proposed high-level components are:

1. **Query text normalizer**
   - lowercases incoming text
   - trims and collapses repeated whitespace
   - produces cleaned text and token stream

2. **Typed query signal extractor**
   - exposed through `ITypedQuerySignalExtractor`
   - runs Microsoft Recognizers over cleaned query text
   - emits normalized typed results such as years, dates, and numbers into the query plan

3. **Query signal extraction rules engine**
   - loads rules from configuration keys under `rules:query:*`
   - evaluates predicates against query input state and extracted typed signals
   - emits canonical query model mutations, concept expansions, sort hints, and consumption directives

4. **Canonical query model builder**
   - builds the discovery-shaped query-side model using canonical field names and semantics aligned with `CanonicalDocument`

5. **Default query mapper**
   - applies default behavior to remaining residual tokens and text after rules have consumed matched signals

6. **Elasticsearch query mapper**
   - converts the query plan into Elasticsearch query DSL against the canonical index

### 3.2 Data flows

The end-to-end flow is:

- raw user query text enters the system
- normalization yields `rawText`, `normalizedText`, `cleanedText`, token list, and phrase windows
- Microsoft Recognizers yields normalized typed signals through `ITypedQuerySignalExtractor`
- signal extraction rules inspect the query input and extracted signals
- matching rules may:
  - add canonical keywords or other canonical query model values
  - emit concept records
  - emit sort hints
  - mark phrases or tokens as consumed
- defaults then apply only to remaining residual content
- the complete query plan is logged/inspectable and mapped into Elasticsearch DSL

### 3.3 Key decisions

The following decisions are captured:

- query rules will be stored under `./rules/query` and loaded from configuration keys under `rules:query:*`
- Microsoft Recognizers is included now rather than deferred
- Microsoft Recognizers must be hidden behind `ITypedQuerySignalExtractor`
- the query plan will include typed extraction outputs now so future scenarios such as `latest notice from 2024` fit the same contract
- recognized years such as `2024` map into `MajorVersion` in the query plan
- initial default analyzed matching targets both `searchText` and `content`, with `searchText` boosted above `content`
- query rules are global across search and are authored in a flat structure under `./rules/query`
- the existing loader approach is sufficient and does not require modification for nested query-rule structures
- future typed filters and boosts are introduced in the rule DSL from the first implementation using explicit `filters` and `boosts` sections
- the query-side canonical model must mirror the discovery-oriented shape of `CanonicalDocument`, but not directly use the ingestion CLR type
- execution directives such as sorting must not be forced into the canonical query model; they live alongside it in the query plan
- rule outputs should support consumption of matched phrases/tokens so rule-recognized terms are not also blindly applied through defaults
- default behavior belongs in runtime query mapping logic, not in rule files

## 4. Functional requirements

### 4.1 Query preprocessing

The query-side runtime must:

- lowercase incoming user query text
- trim leading and trailing whitespace
- collapse repeated spaces to a single space
- produce a stable cleaned form used by rules and typed extraction
- generate a token list from the cleaned query text
- support phrase recognition over multi-token spans such as `most recent`

The preprocessing stage should expose at least:

- `rawText`
- `normalizedText`
- `cleanedText`
- `tokens[*]`
- `residualTokens[*]`
- `residualText`

The runtime may also expose phrase windows or n-grams internally, but these do not need to be first-class persisted plan elements if they are only transient evaluation aids.

### 4.2 Typed signal extraction

The query-side runtime must include Microsoft Recognizers now.

This functionality must be hidden behind an abstraction named `ITypedQuerySignalExtractor`.

The recognizer stage must:

- operate on normalized/cleaned query text
- emit stable, normalized data into the query plan rather than recognizer-specific runtime objects
- support at least future representation of:
  - years
  - dates
  - numbers

The query plan contract must therefore already contain typed extraction sections so the model extends naturally to later scenarios such as `latest notice from 2024`.

Recognized years must also be projected into the canonical query model by mapping them to `MajorVersion` in the query plan.

### 4.3 Canonical query model

The query model must conform in shape to the discovery half of `CanonicalDocument`.

The canonical query model should therefore support at least:

- `keywords`
- `authority`
- `region`
- `format`
- `majorVersion`
- `minorVersion`
- `category`
- `series`
- `instance`
- `searchText`
- `content`
- optionally `title` where later search behavior requires it

The query-side runtime must not directly reuse `CanonicalDocument` as the query plan model because:

- `CanonicalDocument` includes ingestion-specific fields such as `Id`, `Provider`, `Source`, and `Timestamp`
- execution concepts such as sort are not document fields and do not belong in a canonical document-shaped record
- the query side needs transient plan metadata such as matched rules, residual text, and recognizer outputs

### 4.4 Query plan structure

The query-side runtime must produce a JSON-serializable query plan with clear separation between:

- input snapshot
- extracted typed signals
- canonical query model
- default mapping contributions
- execution directives
- diagnostics

A recommended top-level plan structure is:

- `input`
- `extracted`
- `model`
- `defaults`
- `execution`
- `diagnostics`

### 4.5 Default matching behavior

The runtime must own the default mapping behavior.

Rules must not be required to restate default search behavior.

Default behavior must initially be:

- each residual token maps to exact-match style matching against `CanonicalDocument.Keywords`
- the residual query text maps to analyzed matching against both `CanonicalDocument.SearchText` and `CanonicalDocument.Content`
- `CanonicalDocument.SearchText` must be boosted above `CanonicalDocument.Content` in the initial implementation

The initial default behavior deliberately does not require all future search surfaces to be defined now. The repository may later add targeting of `title` or other fields.

### 4.6 Rule-driven signal extraction

Rules must be able to inspect:

- the cleaned input string
- token lists
- residual token lists
- typed extraction output

Rules must be able to emit:

- canonical query model mutations, especially keyword additions
- concept records
- sort hints
- consume directives for tokens and phrases
- future typed filters or boosts without reshaping the whole rule contract

Rules must support multi-token phrase recognition such as:

- `latest`
- `most recent`

Rules must support domain concept recognition such as:

- `solas`

Rules must operate on the canonical index contract, not provider-specific source data.

### 4.7 Consumption semantics

Rules must support consumption of matched phrases and/or tokens.

This is required so that words such as `latest` can be interpreted as sort intent without also being blindly forwarded into default keyword and content matching.

Similarly, concept recognition rules may choose to consume the triggering token so the residual token set reflects only content still intended for default matching.

### 4.8 SOLAS example behavior

For the user query `latest SOLAS`, the runtime should be able to:

- recognize `latest` as sort intent
- recognize `solas` as a domain concept
- add query model keywords such as `solas`, `maritime`, `safety`, and `msi`
- sort by `majorVersion` descending then `minorVersion` descending
- prevent recognized intent terms from also being blindly applied through defaults if those phrases/tokens have been consumed

### 4.9 Future year example behavior

The design must extend naturally to queries such as `latest notice from 2024`.

At minimum, the plan contract must preserve the fact that year `2024` was recognized as a typed temporal signal and must map that recognized year into `MajorVersion` in the canonical query model.

## 5. Non-functional requirements

- query plan contracts must be deterministic and easy to inspect in tests and diagnostics
- typed extraction results must be normalized into repository-owned data structures
- rules must remain provider-independent and repository-authorable in JSON
- the design must support later extension without reshaping the top-level query plan contract
- the runtime should prefer explicit, deterministic rule behavior over opaque NLP-style inference
- the solution must not require heavy full-NLP infrastructure for the initial query signal extraction use cases

## 6. Data model

### 6.1 Query plan JSON contract

A representative query plan for `latest SOLAS` is:

```json
{
  "input": {
    "rawText": "latest SOLAS",
    "normalizedText": "latest solas",
    "cleanedText": "latest solas",
    "tokens": ["latest", "solas"],
    "residualTokens": [],
    "residualText": ""
  },
  "extracted": {
    "temporal": {
      "years": [],
      "dates": []
    },
    "numbers": [],
    "concepts": [
      {
        "id": "solas",
        "matchedText": "solas",
        "keywordExpansions": ["solas", "maritime", "safety", "msi"]
      }
    ],
    "sortHints": [
      {
        "id": "latest",
        "matchedText": "latest",
        "fields": ["majorVersion", "minorVersion"],
        "order": "desc"
      }
    ]
  },
  "model": {
    "keywords": ["solas", "maritime", "safety", "msi"],
    "authority": [],
    "region": [],
    "format": [],
    "majorVersion": [],
    "minorVersion": [],
    "category": [],
    "series": [],
    "instance": [],
    "searchText": "",
    "content": ""
  },
  "defaults": {
    "keywordTokens": [],
    "contentText": ""
  },
  "execution": {
    "sort": [
      { "field": "majorVersion", "order": "desc" },
      { "field": "minorVersion", "order": "desc" }
    ]
  },
  "diagnostics": {
    "matchedRules": ["sort-latest", "concept-solas"]
  }
}
```

### 6.2 Temporal extension example

The same plan shape must extend to a future query such as `latest notice from 2024` using typed extraction outputs such as:

```json
{
  "extracted": {
    "temporal": {
      "years": [2024],
      "dates": []
    }
  },
  "model": {
    "majorVersion": [2024]
  }
}
```

### 6.3 Signal extraction rule JSON contract

A representative rule for latest-style sort intent is:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "sort-latest",
    "title": "Recognize latest intent",
    "enabled": true,
    "if": {
      "any": [
        { "path": "input.cleanedText", "containsPhrase": "latest" },
        { "path": "input.cleanedText", "containsPhrase": "most recent" }
      ]
    },
    "then": {
      "sortHints": [
        {
          "id": "latest",
          "fields": ["majorVersion", "minorVersion"],
          "order": "desc"
        }
      ],
      "consume": {
        "phrases": ["latest", "most recent"]
      }
    }
  }
}
```

A representative rule for the SOLAS concept is:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "concept-solas",
    "title": "Recognize SOLAS concept",
    "enabled": true,
    "if": {
      "path": "input.tokens[*]",
      "eq": "solas"
    },
    "then": {
      "concepts": [
        {
          "id": "solas",
          "matchedText": "$val",
          "keywordExpansions": ["solas", "maritime", "safety", "msi"]
        }
      ],
      "model": {
        "keywords": {
          "add": ["solas", "maritime", "safety", "msi"]
        }
      },
      "consume": {
        "tokens": ["solas"]
      }
    }
  }
}
```

### 6.4 Rule data model notes

The rule model should remain similar in spirit to the ingestion rules DSL, but query-specific in predicate paths and action types.

Recommended query rule inputs include:

- `input.rawText`
- `input.normalizedText`
- `input.cleanedText`
- `input.tokens[*]`
- `input.residualTokens[*]`
- `extracted.temporal.years[*]`
- `extracted.temporal.dates[*]`
- other future typed extraction outputs

Recommended operators include:

- `eq`
- `in`
- `exists`
- `contains`
- `startsWith`
- `endsWith`
- `containsPhrase`

Recommended action groups include:

- `model`
- `concepts`
- `sortHints`
- `consume`
- `filters`
- `boosts`

Typed filters and boosts are part of the rule DSL from the first implementation rather than being deferred solely to the query plan-to-Elasticsearch mapper.

## 7. Interfaces & integration

### 7.1 Rule storage and configuration integration

Signal extraction rules must be stored physically under:

- `./rules/query/*.json`

These must map into configuration keys such as:

- `rules:query:<rule-id>`

Query rules are global across search and therefore use a flat authoring structure in this folder. The resulting configuration namespace is rooted at the `rules:query` key prefix.

The query rule loading mechanism must mirror the ingestion approach in broad terms:

- enumerate configuration under the `rules:query` namespace using hierarchical configuration sections
- log loaded rule counts and effective namespace
- support refresh/reload through configuration change token or App Configuration refresh behavior
- require no special nested-folder handling for query rules in the first implementation because the flat structure is sufficient

### 7.2 Service abstraction

The typed extraction stage must be hidden behind:

- `ITypedQuerySignalExtractor`

This abstraction should return repository-owned models rather than Microsoft Recognizers object graphs.

### 7.3 Elasticsearch integration

The Elasticsearch query mapper must interpret the query plan against the canonical index mapping.

Initial mapping expectations are:

- residual/default keyword matching targets `keywords`
- residual/default full-text matching targets both `searchText` and `content`
- `searchText` is boosted above `content`
- sort hints may target `majorVersion` and `minorVersion`

Later Elasticsearch targeting enhancements, such as targeting `title`, must fit the same query plan contract without requiring contract redesign.

## 8. Observability (logging/metrics/tracing)

The query runtime should log, at appropriate diagnostic levels:

- normalized query text
- matched rule identifiers
- counts of loaded rules from `rules:query:*`
- whether Microsoft Recognizers produced typed signals
- the execution directives derived from the plan, especially sort behavior

The runtime should avoid logging sensitive authorization/filter data if security tokens are later injected from identity context.

The query plan should be suitable for structured diagnostics and deterministic test assertions.

## 9. Security & compliance

- query rules operate on index semantics, not provider-specific source payloads
- security trimming remains a separate concern from user-entered free-form query text
- `SecurityTokens` should be injected from authenticated caller context during query execution rather than parsed from the query text
- the query plan should not rely on untrusted library object serialization; normalized repository-owned contracts must be used instead

## 10. Testing strategy

The implementation derived from this specification should be covered with tests for:

- lowercasing and whitespace cleanup
- tokenization and phrase recognition
- typed extraction normalization through `ITypedQuerySignalExtractor`
- rule matching for phrase-based and token-based rules
- consumption semantics preventing duplicate default matching
- deterministic query plan generation for `latest SOLAS`
- typed temporal capture for a future-style example such as `latest notice from 2024`
- configuration-backed loading from the `rules:query:*` namespace
- refresh/reload behavior parity with the ingestion rule loading model
- Elasticsearch mapping from query plan to expected DSL fragments

## 11. Rollout / migration

The initial rollout should proceed in the following order:

1. define query plan contracts and repository-owned typed extraction result models
2. introduce `ITypedQuerySignalExtractor` backed by Microsoft Recognizers
3. add query rule JSON contract and configuration-backed query rule loader for `rules:query:*`
4. implement default mapping for residual keywords and residual content text
5. implement Elasticsearch query translation
6. replace the stub query client/runtime path in `QueryServiceHost`

This work does not require schema migration of the canonical index itself because it targets existing canonical fields and execution semantics.

## 12. Open questions

The following open questions remain outside the decisions already captured:

None.

## 13. Change log

- v0.01: Initial specification for query-side signal extraction rules, typed query extraction via Microsoft Recognizers behind `ITypedQuerySignalExtractor`, canonical query model alignment with `CanonicalDocument`, rules stored under `./rules/query`, and representative query plan and rule JSON contracts for the `latest SOLAS` scenario.
- v0.01 (clarification): Recognized years such as `2024` map to `MajorVersion` in the query plan.
- v0.01 (clarification): Initial default analyzed matching targets both `searchText` and `content`, with `searchText` boosted above `content`.
- v0.01 (clarification): Query rules are global across search, authored flat under `./rules/query`, and loaded under the `rules:query` configuration prefix without loader modification.
- v0.01 (clarification): Typed `filters` and `boosts` are part of the rule DSL from the first implementation.
