# Ingestion rules spec generator prompt (non-collaborative; produces a markdown spec)

Target output path: `docs/xxx-<work-item-descriptor>/rule-authoring.md`

Copy/paste the prompt below into ChatGPT/Copilot Chat when you want the assistant to generate a **rules specification** document (markdown) from rule details you provide in the chat.

---

## Reference prompt

You are given all of the information needed for one or more ingestion rules in the chat.

Your job is to generate a **single markdown specification document** for those rules.

The deliverable is a markdown document inside the **current work package folder** under `./docs/xxx-<work-item-descriptor>/` (use the active work item folder you are working in), for example:

- `docs/034-ingestion-rule-parsing-operators/rule-authoring.md`

The markdown document must:
- captures the intent, assumptions, and acceptance criteria of the rule(s)
- records required inputs (paths/properties) and expected outputs
- includes the final per-rule JSON payload(s) to be placed under `src/Hosts/IngestionServiceHost/Rules/<provider>/...`

Do NOT modify any files. Only produce the markdown spec content.

Assume any rule JSON we describe will be written as a single JSON file under the `file-share` provider directory, for example:

```json
{
  "schemaVersion": "1.0",
  "rule": {
    /* INSERT RULE HERE */
  }
}
```

### Context / constraints

- Rules are stored on disk as:
  - `Rules/<provider>/**/*.json` (recursive)
  - each file contains exactly one rule document

- A rule file JSON document has shape:
  - top-level `schemaVersion` (must be `"1.0"`)
  - top-level `rule` object (the rule)

- A rule object uses:
  - `id` (required, unique, kebab-case)
  - `description` (concise)
  - `if` predicate (preferred over `match`)
  - `then` actions

- **Do not use** `then.documentType` or `then.facets` (these are removed).

- Treat unsupported/unknown fields the same as any other incorrect field (no special handling).

- Only use supported actions in `then`:
  - `keywords.add`, `searchText.add`, `content.add`
  - Additional top-level `*.add` fields for canonical fields such as:
    - `authority.add`, `region.add`, `format.add`, `category.add`, `series.add`, `instance.add`
    - `majorVersion.add`, `minorVersion.add` (numbers)

- For runtime data that is missing, rules should simply not match / produce no outputs.

- For `majorVersion.add` and `minorVersion.add`:
  - values may be JSON numbers **or** string templates wrapped in `toInt(...)`
  - use `toInt(<expr>)` to explicitly parse strings to integers (parse failure => no output)
  - variables are resolved first (e.g. `$val`, `$path:...`), then trimmed + parsed using invariant culture

### Process (spec generation)

- Produce the full markdown document content for `docs/xxx-<work-item-descriptor>/rule-authoring.md` in one output.

- Only ask a clarification question if a required piece of information is missing and cannot be inferred (for example: whether a `majorVersion`/`minorVersion` source is numeric or needs `toInt(...)`).

- If multiple rules are provided, generate a single combined spec document with a clearly separated section per rule.

- Ensure any JSON you produce is valid and properly escaped (e.g. `properties[\"week\"]`).

### How to interpret my instruction

When I describe a condition like:

- “when `properties[\"week\"]` exists”

Use an `if` block like:

```json
{ "all": [ { "path": "properties[\"week\"]", "exists": true } ] }
```

When I say:

- “put the value of `properties[\"X\"]` into `minorVersion` (and multiple others)”

Use:

- For string fields supporting templates:

```json
"fieldName": { "add": ["$path:properties[\"X\"]"] }
```

- For numeric fields (`majorVersion`, `minorVersion`):
  - if `properties["X"]` is already a JSON number, emit a numeric literal in the `add` array
  - if it might be a string, use `toInt($path:properties["X"])` (or ask me if parsing rules need changing)

### Before writing the spec

Assume the user message contains the full rule details. Do not ask iterative questions. Ask at most one minimal clarification question only if required.

### Required markdown structure (final output)

Use this structure (repeat sections 2–6 per rule if multiple rules are provided):

- Title: `Ingestion rule specification` (include date/time or a short identifier)
- `1. Overview`
  - goal / description
  - provider (e.g. `file-share`)
  - rule id(s)
- `2. Inputs`
  - list of required payload paths (e.g. `properties["week"]`) and whether they are required/optional
  - any normalization/parsing rules (including use of `toInt(...)`)
- `3. Matching logic`
  - describe the `if` predicate in plain English
  - include the draft `if` JSON block
- `4. Outputs / actions`
  - describe each `then` action and where values come from
  - include notes on missing data behavior (no outputs)
- `5. Examples`
  - example input payload fragment(s) (minimal)
  - expected outputs (canonical fields affected)
- `6. Final rule JSON`
  - include one or more final per-rule JSON documents (valid JSON)
  - include the recommended file path(s), e.g. `src/Hosts/IngestionServiceHost/Rules/file-share/<rule-id>.json`

In all JSON examples, ensure paths are properly escaped (e.g. `properties[\"week\"]`).
