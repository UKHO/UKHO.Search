# Ingestion rule mapping prompt (discovery-to-canonical mapping; produces a markdown document)

Target output path: `dev/work-packages/xxx-<work-item-descriptor>/rule-mapping.md`

Copy/paste the prompt below into ChatGPT/Copilot Chat when you want the assistant to read an existing rule-discovery document and generate a **single markdown mapping document** that proposes per-class discriminator logic and canonical field mappings for later rule authoring.

---

## Reference prompt

You are generating a **single markdown mapping document** from an existing ingestion rule discovery document.

This is a **mapping and analysis exercise only**.

Do **not** write rule JSON files, update source code, or implement any rules. Only produce the markdown document content.

## Mandatory operating rules

1. **Your first response in every run must ask me which rule discovery document to use.**
  - Example: `./dev/work-packages/044-rule-discovery/spec-domain-rule-discovery.md`
2. If I already provided the discovery document path in the chat, use it and do not ask again.
3. Read the selected discovery document and use it as the authoritative source for business units, classes, signatures, counts, and representative `AttributeKey` / `AttributeValue` samples.
4. For each business unit, for each discovered class, recreate the representative sample `AttributeKey` / `AttributeValue` markdown table from the discovery document.
5. Directly beneath each recreated sample table, add a section that proposes:
   - a discriminator using `if` logic
   - the mapping from source attributes to the canonical document fields
6. **Always use lowercase** in proposed discriminator logic, property paths, literal outputs, keyword suggestions, category/series values, and search text templates.
7. Where a business unit has more than one class, the discriminator for each class must be based on:
   - the business unit name, and
   - the existence of a field that uniquely identifies that class within that business unit, or
   - if a single positive field existence check is not enough, the business unit name plus:
     - the existence of a relevant field, and
     - the equality of one or more source field values
8. If a single positive field existence check is not enough to choose a safe discriminator, do not guess. Present me with the relevant example data gathered for the competing classes and ask me which **verbatim** field value or values should be used in the discriminator.
9. If you cannot confidently choose a discriminator or mapping, ask me a concise clarification question instead of guessing.
10. When clarification is needed, ask **one question at a time only**. Do not batch multiple business units or multiple unresolved discriminator choices into a single message.
11. Do not emit final rule JSON in this step. This document must contain enough detail to enable later JSON rule creation, but it must stop short of authoring the JSON.

## Deliverable

Produce the full markdown content for a **single** mapping document in the active Work Package folder, for example:

- `dev/work-packages/044-rule-discovery/rule-mapping.md`

Do not modify any files. Only produce the markdown document content.

## Objective

Starting from the selected discovery document, propose how each discovered batch class should map into the canonical search document shape.

For each business unit and class, your output must help a domain expert answer:

- how to recognise this class reliably
- which canonical fields should be populated
- which source attributes should feed those fields
- what fixed values should be added
- where conversion such as `toInt(...)` is needed later
- what search text would be useful semantically

## Canonical document format

Use this canonical shape as the target of the mapping analysis:

- `keywords` (`keyword`, `string`)
  - **must contain a copy of each batch attribute value**
- `authority` (`keyword`, `string`)
  - could be `ukho`, or the `agency` field if it exists
- `region` (`keyword`, `string`)
- `format` (`keyword`, `string`)
  - media format or similar
- `majorVersion` (`keyword`, `int`)
  - often `edition number` or `year`; note where `toInt(...)` would be required
- `minorVersion` (`keyword`, `int`)
  - often `update number` or `week number`; note where `toInt(...)` would be required
- `category` (`keyword`, `string`)
  - highest level categorisation, for example `software`, `enc`, `exchange set`
- `series` (`keyword`, `string`)
  - for example `s57`, `s100`
- `instance` (`keyword`, `string`)
  - for example `cell name` or `year / week`
- `searchText` (`text`)
  - invent useful descriptive text from the values

## Business unit descriptions

Treat these descriptions as domain intent when proposing mappings:

- `adds` – private enc s63 data to make s63 exchange set
- `adds-s57` – private enc s57 data to make unencrypted exchange sets used for bess
- `avcscustomexchangesets` – exchange sets made by customers using the ess api and ess ui (s57, s63 and s100). customers only have access to their own exchange sets, retrieved using batch id or url
- `avcsdata` – weekly data sets uploaded by tpms and collected by distributers
- `maritimesafetyinformation` – notice to mariners displayed on msi website and also available via fss ui/api
- `britishlegaldepositlibrarypublications` – nothing searchable
- `printtoorder` – nothing searchable, bu is for print to order team
- `addssupport` – random test stuff
- `adp` - weekly data sets uploaded by tpms and collected by distributers
- `aenp` - weekly data sets uploaded by tpms and collected by distributers
- `arcs` - weekly data sets uploaded by tpms and collected by distributers
- `paperproducts` – pod files not sure whether these are downloaded from ui think fss might just be for storage and an application might be getting them
- `chersoft` – business continuity
- `var` – specific bu for file sharing with vars but not really anything
- `adds-s100` – private s100 file ingestion to make s100 exchange sets
- `looseleaf` – private bu for file sharing with this supplier
- `s100-tidalservice` – s111 and s104 data for tidal trial
- `avcs-bespokeexchangesets` – bess outputs, these need to be collected from ui
- `senc` - specific bu for file sharing with sencs but not really anything
- `testpenrose-s57` – old test bu
- `testpenrose-s63` – old test bu
- `printedmedia` – for sending files to packaged sounds printer
- `defenceproducts` – defence file sharing collected from ui - defence data in here so do not use for testing
- `adsd-viewerupdates` – sailing directions data being added here in case adds distributers ever want to get it via api (no files displayed on fss ui)

## Example rule style reference

Use this only as a style guide for the proposed discriminator and mapping logic. Do **not** output final JSON in this step.

```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "avcs-aio-catalogue",
    "description": "AVCS AIO catalogue: weekly dataset uploaded by TPM for distributors.",
    "if": {
      "all": [
        {
          "path": "properties[\"businessunitname\"]",
          "eq": "avcs-bespokeexchangesets"
        },
        {
          "path": "properties[\"catalogue type\"]",
          "exists": true
        }
      ]
    },
    "then": {
      "keywords": {
        "add": [
          "avcs",
          "catalog",
          "catalogue",
          "weekly",
          "data",
          "tpm"
        ]
      },
      "authority": {
        "add": [
          "ukho"
        ]
      },
      "category": {
        "add": [
          "catalogue"
        ]
      },
      "series": {
        "add": [
          "avcs"
        ]
      },
      "instance": {
        "add": [
          "$path:properties[\"year / week\"]"
        ]
      },
      "majorVersion": {
        "add": [
          "toInt($path:properties[\"year\"])"
        ]
      },
      "minorVersion": {
        "add": [
          "toInt($path:properties[\"week number\"])"
        ]
      },
      "searchText": {
        "add": [
          "weekly data set uploaded by tpm for distributors for $path:properties[\"year / week\"]"
        ]
      }
    }
  }
}
```

## Required working method

### Step 1 - Ask for the discovery document path

Your first response in every run must ask for the specific discovery document path unless it is already present in the chat.

Example:

> Please provide the path of the rule discovery document to use, for example `./dev/work-packages/044-rule-discovery/spec-domain-rule-discovery.md`.

Do not ask any other question first unless that path/version is already available.

### Step 2 - Read the discovery document carefully

From the selected discovery document, extract:

- business unit id and name
- business unit description from the supplied domain list above
- total batch counts and coverage notes
- each discovered class within the business unit
- the representative batch id
- the sorted attribute keys
- the representative `AttributeKey` / `AttributeValue` example table

### Step 3 - Propose discriminators

For each class, propose a discriminator using draft `if` logic in a JSON-like structure.

Rules for discriminator proposals:

- always include a business-unit-name condition such as:
  - `properties["businessunitname"] eq "adds-s57"`
- when the business unit has more than one class, first try to use the existence of a field that uniquely identifies that class within that business unit
- if uniqueness cannot be achieved from a single positive field existence check alone, you must also include one or more equality checks against source field values
- when you need value-based discrimination, present the relevant competing classes and their representative example data before asking the question
- for that clarification step, show:
  - the business unit name
  - the competing class numbers
  - the representative batch id for each class
  - the sorted attribute keys for each class
  - the recreated `AttributeKey` / `AttributeValue` example table for each competing class
  - the candidate field or fields whose values appear useful for discrimination
  - the candidate **verbatim** field value or values exactly as they appear in the discovery document
- after showing that evidence, ask a concise question asking which verbatim field value or values should be used
- ask about only one unresolved discriminator decision at a time; after the user answers, continue to the next unresolved discriminator only if another clarification is still required
- choose the next question in the order the ambiguous business units appear in the discovery document unless the user directs otherwise
- when you later write the proposed discriminator logic, keep the logic itself lowercase, but preserve the evidence values verbatim when presenting the clarification options
- use lowercase property paths such as `properties["product type"]`, `properties["week number"]`, `properties["trace id"]`
- explain in plain English why the discriminator distinguishes that class

### Step 4 - Propose canonical mappings

For each class, map source attributes into the canonical fields.

Guidance:

- `keywords`
  - always state that all batch attribute values must be copied into `keywords`
  - optionally propose extra fixed keywords that improve recall
- `authority`
  - prefer `agency` when present
  - otherwise use `ukho` when that is the best domain fit
- `region`
  - infer only when there is an obvious source such as agency/cell naming/region-like attribute
  - if not clear, say `not mapped`
- `format`
  - use media-related attributes such as `media type`, `content`, or exchange-set packaging clues where sensible
- `majorVersion`
  - prefer `edition number`, `edition`, or `year`
  - explicitly note `toInt(...)` where the source looks string-based
- `minorVersion`
  - prefer `update number` or `week number`
  - explicitly note `toInt(...)` where needed
- `category`
  - choose the highest-level user-facing category, for example `enc`, `exchange set`, `catalogue`, `software`, `notice to mariners`, `paper chart`
- `series`
  - choose values such as `s57`, `s63`, `s100`, `avcs`, `aenp`, `adp`, `arcs` when justified
- `instance`
  - prefer identity-bearing values such as `cell name`, `product id`, `product identifier`, `chart number`, or `year / week`
- `searchText`
  - propose a short natural-language template that would help semantic search
  - keep it lowercase

If a field should not be mapped, say `not mapped` and explain why.

### Step 5 - Handle non-searchable or unsupported business units

If the business unit description says it is not searchable, is test-only, or there is no reliable search value:

- still include the business unit section
- if the discovery document contains classes, still recreate the sample table(s)
- clearly recommend either:
  - `no searchable rule proposed`, or
  - `defer pending domain confirmation`
- explain why

If a business unit has zero batches in the discovery document:

- include the business unit section
- state that there is currently no class evidence and no mapping proposal can be made yet

## Required markdown structure

Use this structure in the final output:

- Title: `Ingestion rule mapping proposal` (include the discovery document version/path)
- `1. Overview`
  - purpose of the document
  - selected discovery document version/path
  - scope and non-goals
- `2. Global assumptions and normalization rules`
  - lowercase conventions
  - keyword-copy requirement
  - guidance on `toInt(...)`
  - rule-authoring follow-up note
- `3. Business unit mapping proposals`
  - repeat the following for every business unit in the discovery document:
    - business unit heading with id and name
    - business unit description
    - discovery summary (batch count, class count, coverage)
    - if no classes: state that no evidence exists yet
    - for each class:
      - class heading
      - representative batch id
      - sorted attribute keys
      - recreated sample `AttributeKey` / `AttributeValue` markdown table
      - `Proposed discriminator`
        - plain English rationale
        - draft `if` block in JSON-like form
        - if clarification is required before a safe discriminator can be chosen, add a `Discriminator clarification required` subsection that presents the competing class evidence and asks which verbatim field value or values should be used
      - `Proposed canonical mapping`
        - markdown table with columns:
          - `Canonical Field`
          - `Source / Expression`
          - `Mapping Rationale`
        - include `not mapped` where appropriate
      - `Proposed search text`
      - `Notes / risks / questions`
- `4. Cross-business-unit observations`
  - repeated patterns
  - candidate shared rule families
  - notable inconsistencies or normalization opportunities
- `5. Open questions requiring domain confirmation`
  - only include unresolved items that materially affect later rule authoring

## Quality bar

Your output must be easy for a domain expert to inspect and edit.

Therefore:

- be explicit rather than terse
- keep tables tidy and consistent
- distinguish facts from proposals
- preserve the original sample values from the discovery document in the recreated sample tables
- keep all proposed discriminator paths and fixed output values in lowercase
- include enough detail that a later step could convert the document into rule JSON without re-discovering the intent

## When to ask me a question

Ask a concise clarification question if and only if one of these is true:

- there is no discovery document version/path
- a class in a multi-class business unit does not have a clear unique discriminator from a single positive field existence check and you need the user to choose verbatim field value or values for an equality check
- a mapping choice would materially change search behaviour and the source evidence is ambiguous
- the business unit is described as non-searchable but the discovery evidence strongly suggests otherwise

When asking that discriminator clarification question, do not ask it in the abstract. Present the relevant class evidence and the candidate verbatim values first.

Ask only one clarification question per response. Wait for the user's answer before asking the next one.

If none of those apply, generate the full markdown document content in one response.
