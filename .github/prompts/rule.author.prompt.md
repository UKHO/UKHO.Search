# Ingestion rule author prompt (mapping document to per-rule JSON files)

Target output path: `./rules/file-share/`

Copy/paste the prompt below into ChatGPT/Copilot Chat when you want the assistant to read an existing **rule mapping markdown document** and create the corresponding **per-rule JSON files**.

---

## Reference prompt

You are authoring **ingestion rule JSON files** from an existing **markdown rule mapping document**.

Your job is to read the selected mapping document, identify each proposed rule, ensure `./rules/file-share/` exists, and create the corresponding rule JSON files in that directory.

Use `./docs/ingestion-rules.md` as the implementation guide whenever rule-schema or rule-authoring detail is needed.

## Mandatory operating rules

1. **Your first response in every run must ask me which markdown rule mapping document to use, unless I already provided it in the chat.**
   - Example: `./docs/044-rule-discovery/spec-domain-rule-discovery_v0.03.md`
2. If I already provided the mapping document path/version in the chat, use it and do not ask again.
3. Treat the selected markdown mapping document as the authoritative source for:
   - business unit name
   - class number
   - discriminator logic
   - canonical mapping decisions
   - domain confirmations and resolved open questions
4. For **each proposed rule** in the document, create **one JSON file** in `./rules/file-share/`.
5. If `./rules/file-share/` does not exist, create it before writing any rule files.
6. Each JSON file must follow the guidance in `./docs/ingestion-rules.md`.
7. Use `if` rather than `match` when authoring predicates.
8. Do not create a new markdown spec/version while authoring rules.
9. If a required rule detail is missing or ambiguous, ask **one concise clarification question at a time**.
10. Do not guess where the mapping document is explicit.
11. File creation is required; do not stop at analysis only.
12. Format every authored JSON file with **strictly four spaces per indentation level**.

## Required input

Prompt me for the markdown document containing the rule mapping unless it is already specified.

Example input document:

- `./docs/044-rule-discovery/spec-domain-rule-discovery_v0.03.md`

## Output location

Create all generated rule files under:

- `./rules/file-share/`

If `./rules/file-share/` does not exist, create it before writing rule files.

## File naming convention

Each rule file must be named:

- `bu-{businessunit name}-{class}-{description}.json`

Naming rules:

- use lowercase only
- replace spaces and separator punctuation with hyphens
- make `{businessunit name}` come directly from the document in normalized filename form
- make `{class}` come directly from the document class number
- make `{description}` short, specific, and meaningful to a human reader
- avoid unnecessary words such as `rule` or `json` in the description segment

Example filenames:

- `bu-adds-1-s63-cell-agency-traceid.json`
- `bu-adds-s100-2-product-type-product-identifier.json`
- `bu-adsd-viewerupdates-1-publishdatetime.json`

## Rule authoring requirements

For each proposed rule found in the mapping document:

1. Create a valid per-rule JSON file with shape:

```json
{
    "schemaVersion": "1.0",
    "rule": {
        "id": "...",
        "description": "...",
        "enabled": true,
        "if": { },
        "then": { }
    }
}
```

2. Build the `if` predicate from the document's proposed discriminator.
3. Build the `then` actions from the document's proposed canonical mapping.
4. Follow `./docs/ingestion-rules.md` for:
   - schema validity
   - path syntax
   - predicate/operator structure
   - action structure
   - scalar vs multi-valued fields
   - numeric conversion using `toInt(...)`
   - lowercase normalization expectations
5. Preserve the intent of the mapping document exactly.
6. Where the mapping document says all batch attribute values must be copied into `keywords`, author only the explicitly mapped fixed keywords from the spec in rule JSON. Assume the ingestion service copies the remaining batch attribute values into `keywords` at runtime.
7. Where the mapping document specifies aliases such as both `s-100` and `s100`, include them in the authored JSON exactly as required by the mapping document.
8. Use business-unit-specific, unique rule ids.
9. When creating or updating rule files, write the JSON using **strictly four spaces per indentation level**.

## Working method

### Step 1 - Ask for the mapping document

If no mapping document path/version is already present in the chat, ask for it first.

Example:

> Please provide the markdown rule mapping document to use, for example `./docs/044-rule-discovery/spec-domain-rule-discovery_v0.03.md`.

### Step 2 - Read the mapping document

Extract, for each business unit/class:

- business unit name
- class number
- representative metadata only if needed for description or clarification
- proposed discriminator
- proposed canonical mapping
- any domain confirmations that affect rule contents

### Step 3 - Author rule JSON files

For each proposed rule/class:

- create one JSON file in `./rules/`
- use the required filename convention
- make the rule valid according to `./docs/ingestion-rules.md`

### Step 4 - Validate your own output

Before finishing:

- check that every proposed rule/class from the mapping document has a corresponding JSON file
- check that filenames are lowercase and human-readable
- check that each file uses `schemaVersion: "1.0"`
- check that each rule has exactly one predicate block
- check that operators and paths conform to `./docs/ingestion-rules.md`
- check that every rule JSON file is formatted with strictly four spaces per indentation level

## Clarification rules

Ask a question only when necessary, for example:

- the mapping document contains a class but no usable discriminator
- a required action cannot be represented safely from the documented guidance
- the mapping document is explicit about intent but incomplete about the exact output structure

When asking, ask only one question at a time.

## Deliverable

Create the rule JSON files in `./rules/file-share/`.

Do not create a new spec document.
Do not rewrite the mapping document unless I explicitly ask you to.
