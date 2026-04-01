# Ingestion troubleshooting

Use this page after reading [Ingestion pipeline](Ingestion-Pipeline) and [Ingestion walkthrough](Ingestion-Walkthrough).

## Quick symptom guide

| Symptom | Likely cause | Next page |
|---|---|---|
| No documents are appearing in Elasticsearch | The message failed before indexing, the provider is not enabled, or rules/enrichment produced no title | [Ingestion walkthrough](Ingestion-Walkthrough) |
| Startup fails with a rules validation error | Invalid JSON, missing `title`, unsupported path syntax, or unknown provider metadata | [Ingestion rules](Ingestion-Rules#fail-fast-validation-vs-runtime-tolerance) |
| A rule works in JSON review but not in runtime evaluation | The active payload path does not resolve, or the rule depends on the wrong provider scope | [Appendix: rule syntax quick reference](Appendix-Rule-Syntax-Quick-Reference) |
| RulesWorkbench shows changes but the host still behaves as if the old rules are loaded | The services-mode stack was not restarted after repository rule edits | [Project setup](Project-Setup#configuration-behavior-in-local-aspire) |
| The batch dead-letters with `CANONICAL_TITLE_REQUIRED` | Enrichers and rules never produced a retained title | [CanonicalDocument and discovery taxonomy](CanonicalDocument-and-Discovery-Taxonomy#title) |
| One key appears blocked while other work keeps flowing | A single partition lane is hot or downstream indexing for that lane is retrying | [Ingestion pipeline](Ingestion-Pipeline#channels-backpressure-and-lane-health) |

## Start with the boundary that owns the symptom

A fast way to avoid chasing the wrong layer is to ask which boundary owns the failure first.

| Area | Owner | Typical files or pages |
|---|---|---|
| Queue polling, visibility, acknowledgements | Infrastructure | [Ingestion pipeline](Ingestion-Pipeline), `src/UKHO.Search.Infrastructure.Ingestion` |
| Provider registration and startup validation | Provider model + host composition | [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism), [Provider metadata and split registration](Provider-Metadata-and-Split-Registration) |
| ZIP parsing, file-content extraction, geo enrichment | File Share provider | [File Share provider](FileShare-Provider) |
| Rule matching and canonical field mutation | Shared rules engine | [Ingestion rules](Ingestion-Rules), [Appendix: rule syntax quick reference](Appendix-Rule-Syntax-Quick-Reference) |
| Search visibility after successful indexing | Elasticsearch / query path | [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard) |

## Common issues

### Startup fails because a provider is unknown

The current rules-loading path canonicalizes provider names through `IProviderCatalog`.

If a rule entry targets a provider name that is not registered in provider metadata:

- the host fails during rules loading
- the error names the unknown provider
- fixing the provider metadata or the rule scope is required before startup can continue

Check both the provider registration and the rule key or folder name before editing runtime code.

### Startup fails because a rule is missing `title`

`title` is part of the current contract.

A rule that only adds keywords, taxonomy, or content but has no title still fails validation at load time. Add a non-empty display-oriented title and reload the stack.

### A rule using `exists: false` does not seem to match

`exists: false` only matches when the resolved values are absent or all empty/whitespace after trimming.

Check the payload shape in `RulesWorkbench` carefully:

- if the property exists with a non-empty value, `exists: false` will not match
- if the property name casing or spacing is wrong, the path resolves to nothing
- `properties["name"]` is safer than dot notation when the source property name contains spaces

### The rule matches in theory but nothing changes on the document

Start by checking whether the action is one of the fields the current action applier actively materializes.

Today the applier writes:

- `title`
- `keywords`
- `searchText`
- `content`
- taxonomy sets such as `category`, `series`, and `instance`

The DTO shape still contains `facets` and `documentType`, but those sections are not currently applied to `CanonicalDocument` by the live action applier. If your JSON only changes those sections, the rule can match without producing visible canonical output.

### The batch goes to dead letter even though some rules matched

A matched rule is not the same as a successful document.

The most important post-rules failure to remember is missing title validation. If the final canonical document has no retained title after enrichment and rules complete, the pipeline routes the operation to index dead letter instead of indexing it.

Use dead-letter payloads together with `RulesWorkbench` to confirm whether:

- the expected title template expanded to nothing
- the rule never matched
- another stage failed before indexing

## Moderately advanced issues

### Rule changes are not visible after editing files under `rules/`

In local services mode the repository files are loaded into the configuration emulator under the `rules` prefix at AppHost startup.

That means a repository rule edit is not live until the services-mode stack is restarted. If RulesWorkbench and IngestionServiceHost appear stale, restart AppHost before assuming the rule engine is caching incorrectly.

### Ordering-sensitive rule behavior looks inconsistent

The current engine applies rules in the order provided by the loaded provider ruleset.

For file-based loading the repository preserves deterministic path ordering through the file loader. For configuration-backed local services mode, the safest practice is still to keep rules independent and monotonic rather than depending on subtle inter-rule sequencing.

If a rule appears to depend on a previous rule's side effect, refactor the logic so each rule can stand on its own inputs.

### Lane backlog is growing for only one subset of documents

That usually points to a hot key or a downstream retry path in one partition lane rather than to a total host failure.

Inspect:

- queue-depth and per-node metrics in Aspire
- dead-letter and diagnostics output for the affected key
- bulk-index or enrichment retries for that lane

This is one of the places where the lane-based design is working as intended: one problematic key does not have to collapse the whole provider graph.

### ZIP-dependent enrichment works differently from RulesWorkbench

That difference is expected.

`RulesWorkbench` shares the rules engine, but it does not execute the full ZIP-dependent File Share enrichment chain. If a batch depends on Kreuzberg extraction, S-57 parsing, or S-101 parsing before the desired canonical field exists, test through the full pipeline as well as through RulesWorkbench.

## Practical diagnostic commands

Start the local services stack:

```powershell
dotnet run --project src/Hosts/AppHost/AppHost.csproj
```

Run the ingestion-rules tests:

```powershell
dotnet test test/UKHO.Search.Infrastructure.Ingestion.Tests/UKHO.Search.Infrastructure.Ingestion.Tests.csproj
```

Run the RulesWorkbench tests:

```powershell
dotnet test test/RulesWorkbench.Tests/RulesWorkbench.Tests.csproj
```

## What to confirm before escalating

1. The correct provider is registered and enabled.
2. The rule id, title, and provider scope are correct.
3. Predicate paths resolve against the active payload you are actually sending.
4. The action writes to a canonical field the current action applier materializes.
5. The final canonical document retains at least one title.
6. The dead-letter or diagnostics output matches the stage you think is failing.

## Related pages

- [Ingestion pipeline](Ingestion-Pipeline)
- [Ingestion walkthrough](Ingestion-Walkthrough)
- [Ingestion rules](Ingestion-Rules)
- [Appendix: rule syntax quick reference](Appendix-Rule-Syntax-Quick-Reference)
- [Ingestion service provider mechanism](Ingestion-Service-Provider-Mechanism)
- [File Share provider](FileShare-Provider)
- [Tools: `RulesWorkbench`](Tools-RulesWorkbench)
- [Metrics in the Aspire dashboard](Metrics-in-the-Aspire-Dashboard)
