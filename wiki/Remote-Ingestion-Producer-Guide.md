# Remote ingestion producer guide

This page explains how an external .NET producer should approach `UKHO.Search.Ingestion.Contracts` in the wider Search repository story.

If you already know that the package is the right fit and you want the closest usage guide, start with the canonical package README at [../src/UKHO.Search.Ingestion.Contracts/README.md](../src/UKHO.Search.Ingestion.Contracts/README.md). That README is the source of truth for the package surface itself. This page exists to answer a slightly different question: where does that package sit in the wider architecture, and what responsibilities still remain outside it?

## Start with the package boundary

`UKHO.Search.Ingestion.Contracts` is a queue-message authoring package.

That sentence is easy to skim past, but it carries most of the important design meaning. The package exists so a producer can create valid ingestion queue messages without pulling in Search runtime hosts, provider implementations, queue polling code, Elasticsearch integration, journal concepts, or Studio-facing application layers. It is intentionally small because its job is specific: describe the message contract and help producers author that contract correctly.

The package therefore owns:
- the queue-message DTOs
- helper factories and builder support for common authoring paths
- serializer entry points for canonical JSON output
- validator entry points for non-throwing contract checks
- the visible contract-version marker

The package does not own transport or runtime execution. That line is the most important boundary to preserve while reading the rest of this guide.

## What a producer is actually solving

A producer using this package is solving the “message body” part of the problem, not the whole ingestion pipeline.

In practical terms, the producer usually already knows several things that the package deliberately does not decide:
- which queue should receive the message
- how the producer authenticates to that queue or environment
- which deployment topology or routing path is in use
- how retries, error handling, and operational telemetry work in that producer’s host process

Those concerns still matter, but they are not package responsibilities. They belong either to the producer host itself or to a future optional transport-oriented package. The contracts package is deliberately narrower so it can stay reusable and stable.

## The producer workflow in sequence

Most producers follow a simple sequence:

1. Decide which supported operation they need: `IndexItem`, `DeleteItem`, or `UpdateAcl`.
2. Use the DTOs directly, or use the helper APIs and builder support if those make the calling code clearer.
3. Optionally run the validator when a non-throwing pre-submit check is useful.
4. Serialize the final envelope through the canonical serializer path.
5. Submit the resulting JSON through the producer’s own queue or transport mechanism.

The package helps with steps 1 through 4. Step 5 is intentionally outside the package boundary.

That distinction keeps the architecture honest. If the package also owned queue clients, authentication policy, or provider routing, every consumer would inherit a larger operational dependency graph than the message contract itself really requires.

## What stays outside the package

Several concerns are explicitly outside the contracts package even though they are often discussed alongside message authoring.

### Queue submission

The package does not submit messages. It does not create or manage Azure Queue clients, and it does not promise any transport abstraction. If you need a submission helper later, that belongs in a separate optional package so the core contracts package can remain dependency-light.

### Queue naming and provider routing

The package does not choose the queue name and does not decide which provider queue should receive a given message. Those decisions belong to the producer’s deployment context and routing logic.

### Authentication and secrets

The package does not acquire credentials, manage secrets, or prove that a producer is authorized to write to a queue. A syntactically valid queue message is not the same thing as an authorized queue submission.

### Security-token derivation

The package does not derive File Share or provider-specific security tokens. That remains upstream policy in the current version. If a producer needs token derivation, that logic belongs to the producer’s domain or to some other dedicated package or service, not to `UKHO.Search.Ingestion.Contracts`.

### Journal, replay, and dead-letter concerns

The package does not own `ShadowId`, journal identity, dead-letter records, replay metadata, or outcome storage. Producers provide queue-message data only. The wider ingestion runtime owns what happens after that message enters the Search system.

## How to think about compatibility

The package is not just a convenience library. It is a versioned contract surface.

That means compatibility should be discussed in terms of observable producer-facing behavior, not only implementation detail. The following are compatibility-sensitive parts of the package story:
- queue-message JSON field names
- request discriminator values and payload-property names
- property-type token serialization
- helper semantics that claim to create canonical contract instances
- serializer behavior that emits or parses canonical queue-message JSON
- validator interpretations of valid and invalid contract instances

By contrast, internal refactoring, clearer documentation, or small implementation reshaping that does not alter the observable contract story is not automatically a breaking change.

This is why the canonical README and the contracts-only validation path matter together. The documentation explains what producers are supposed to rely on, and the tests prove that the package still behaves that way.

## When to choose which entry point

The package offers more than one way to author a message because different producer codebases value different trade-offs.

Use raw DTOs when you want the thinnest possible abstraction and your code already manages contract details directly.

Use the helper factories when you want a straightforward entry point for `DeleteItem`, `UpdateAcl`, or `IndexItem` envelope creation without repetitive boilerplate.

Use `IndexRequestBuilder` when `IndexItem` authoring is incremental and a fluent sequence is easier to read than manual DTO assembly.

Use the serializer facade when you want canonical JSON output without wiring `JsonSerializerOptions` explicitly.

Use the validator when your producer wants a non-throwing validation pass before a submission boundary.

The canonical README contains the concrete examples for those entry points. This page exists to explain why those entry points are the right level of abstraction for the package boundary.

## Where to go next

For the package-adjacent source-of-truth guide, read [../src/UKHO.Search.Ingestion.Contracts/README.md](../src/UKHO.Search.Ingestion.Contracts/README.md).

For the wider repository architecture story, read [Solution architecture](Solution-Architecture) and then [Architecture walkthrough](Architecture-Walkthrough).

For a walkthrough of how queue messages later move through ingestion runtime processing, read [Ingestion walkthrough](Ingestion-Walkthrough). That page describes runtime execution, which is a separate concern from producer message authoring.