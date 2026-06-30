# UKHO.Search.Ingestion.Contracts

`UKHO.Search.Ingestion.Contracts` is the canonical .NET contract package for authoring Search ingestion queue messages.

The package is intentionally narrow. It gives producers the DTOs, enums, collection wrappers, helper APIs, validator surface, and `System.Text.Json` configuration needed to create valid queue-message payloads for the Search ingestion runtime. It does not try to be a transport client, a queue-submission SDK, or a runtime integration package.

This README is the canonical producer guide for the package. Read it when you need to understand what the package is for, which entry points it exposes, which responsibilities remain outside it, and how to think about compatibility when the package evolves.

## Who this package is for

This package is for code that already knows which queue to talk to and how to authenticate against that queue, but needs a supported way to construct the message body itself.

Typical consumers include:
- remote .NET producers that create `IndexItem`, `DeleteItem`, or `UpdateAcl` messages
- internal runtime projects that need the same wire contract without depending on broader runtime-local DTO ownership
- tests and tooling that need to validate or inspect the published JSON contract

In other words, this package owns message authoring, not message delivery.

That distinction matters. If your application needs to choose a queue, authenticate to Azure, decide which provider queue to target, or manage retries and submission failures, that part of the solution belongs outside this package.

## Supported contract surface

The package currently publishes the three ingestion envelope operations used by the runtime:
- `IndexItem`: create or update a document, including metadata properties, security tokens, timestamp, and file entries
- `DeleteItem`: remove a previously indexed document by identifier
- `UpdateAcl`: replace the security-token set for an existing document

All three operations are represented through the `IngestionRequest` envelope. The envelope requires:
- a `RequestType` discriminator
- exactly one matching payload property: `IndexItem`, `DeleteItem`, or `UpdateAcl`

Legacy payload property names such as `AddItem` and `UpdateItem` are intentionally rejected. The contract now uses the current payload names only.

From a producer perspective, the package surface has four layers:
- raw DTOs for callers that want direct control over the contract model
- helper APIs and builders for common authoring paths
- serializer entry points for canonical JSON emission and parsing
- validator entry points for non-throwing contract checks

## Producer-safe authoring entry points

The raw DTOs remain the canonical contract surface, but the package also exposes producer-safe convenience APIs for the first common authoring paths.

Current helper surface:
- `IngestionPropertyFactory.*(...)`
- `IngestionRequestFactory.CreateDelete(...)`
- `IngestionRequestFactory.CreateAclUpdate(...)`
- `IngestionRequestFactory.CreateIndex(...)`
- `IndexRequestBuilder`
- `IngestionContractSerializer.Serialize(...)`
- `IngestionContractSerializer.DeserializeIngestionRequest(...)`
- `IngestionContractValidator.Validate(...)`
- `IngestionContractsPackage.ContractVersion`

These helpers exist to reduce repetitive setup and accidental serializer misconfiguration. They do not change the underlying wire contract, and they do not take ownership of queue submission or provider policy.

The package also exposes `IngestionContractsPackage.ContractVersion` as a simple visible compatibility marker. Producers, tests, and documentation can reference that marker when they need to state which helper-era contract surface they are targeting.

## How producers should serialize messages

Use either the package DTOs together with `IngestionJsonSerializerOptions.Create()`, or the package-owned serializer facade, so the emitted JSON matches the runtime's expected field names, null-handling rules, lower-case property-type tokens, and typed property-value behavior.

```csharp
using System.Text.Json;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Contracts.Serialization;

var envelope = new IngestionRequest
{
	RequestType = IngestionRequestType.IndexItem,
	IndexItem = new IndexRequest
	{
		Id = "ABC123",
		Timestamp = DateTimeOffset.UtcNow,
		Files = new IngestionFileList(),
		Properties =
		[
			new IngestionProperty
			{
				Name = "Title",
				Type = IngestionPropertyType.String,
				Value = "Example document"
			}
		],
		SecurityTokens = ["token-a"]
	}
};

var json = JsonSerializer.Serialize(envelope, IngestionJsonSerializerOptions.Create());
```

For simple delete and ACL-update flows, a producer can use the helper surface instead:

```csharp
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Contracts.Serialization;

var deleteEnvelope = IngestionRequestFactory.CreateDelete("ABC123");
var deleteJson = IngestionContractSerializer.Serialize(deleteEnvelope);

var aclEnvelope = IngestionRequestFactory.CreateAclUpdate("ABC123", ["token-a", "token-b"]);
var aclJson = IngestionContractSerializer.Serialize(aclEnvelope);
```

For `IndexItem` authoring, producers can either call `IngestionRequestFactory.CreateIndex(...)` directly or use `IndexRequestBuilder` when incremental composition is clearer:

```csharp
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Contracts.Serialization;

var indexRequest = new IndexRequestBuilder()
	.WithId("ABC123")
	.WithTimestamp(DateTimeOffset.UtcNow)
	.AddSecurityToken("token-a")
	.AddProperty(IngestionPropertyFactory.String("Title", "Example document"))
	.AddProperty(IngestionPropertyFactory.StringArray("Keywords", ["alpha", "beta"]))
	.AddFile("a.txt", 123, DateTimeOffset.UtcNow, "text/plain")
	.Build();

var envelope = IngestionRequestFactory.CreateIndex(indexRequest);
var json = IngestionContractSerializer.Serialize(envelope);
```

The builder exists to simplify authoring, not to weaken the contract. It still delegates final validation to the canonical queue-message DTO model before it returns a payload.

When a producer wants a non-throwing validation step before serialization or queue submission, the package also exposes a flat validator result surface:

```csharp
using UKHO.Search.Ingestion.Contracts;

var validation = IngestionContractValidator.Validate(envelope);
if (!validation.IsValid)
{
	foreach (var error in validation.Errors)
	{
		Console.WriteLine($"{error.Code} @ {error.Path}: {error.Message}");
	}
}
```

The validator reports a flat core error model with a stable `Code`, a contract `Path`, and a human-readable `Message`. That shape is intentionally simple so producer code can log it, display it, serialize it, or map it into local tooling without depending on a UI-specific grouping model.

The package deliberately keeps the serializer entry point simple. If a producer can already submit a UTF-8 or string payload to the correct queue, the serialized output from that code path is the contract this package is designed to provide.

## A practical producer workflow

For most producers, the recommended sequence is:
1. Choose whether raw DTOs or helper APIs make the calling code clearer.
2. Build the envelope and payload through the package surface.
3. Run `IngestionContractValidator.Validate(...)` if your application wants a non-throwing pre-submit validation step.
4. Serialize through `IngestionContractSerializer` or `IngestionJsonSerializerOptions.Create()`.
5. Submit the resulting JSON through your own queue or transport mechanism.

That final submission step is intentionally outside the package boundary. The contracts package helps you produce the payload correctly; it does not own how your environment delivers that payload.

## Which entry point to use

Use the package surface according to the authoring scenario:
- use raw DTOs when your code already manages contract details directly and you want the thinnest possible abstraction
- use `IngestionRequestFactory` for straightforward envelope creation
- use `IndexRequestBuilder` when `IndexItem` authoring is incremental or easier to read as a fluent sequence
- use `IngestionPropertyFactory` when you want typed property creation without manually pairing `Type` and `Value`
- use `IngestionContractSerializer` when you want canonical serialization without wiring `JsonSerializerOptions` yourself
- use `IngestionContractValidator` when you want a non-throwing validation pass before submission or serialization boundaries

## What the helper surface still does not do

The new helper APIs make authoring easier, but they still do not:
- derive File Share or provider-specific security tokens
- inject `BusinessUnitName` or other provider-policy fields automatically
- submit messages to a queue or transport
- choose a provider queue or routing topology
- generate journal identity, `ShadowId`, dead-letter metadata, or replay metadata

Producers must still supply their own queue destination, authentication, retry policy, and any upstream security-token derivation policy that belongs to their deployment or provider context.

The same is true for provider queue selection and deployment topology. The contracts package does not decide which queue name to use or which environment-specific route should carry a message. Those decisions belong to the producer host or a separate transport-oriented package.

## Contract rules producers need to know

The DTOs enforce the same current-state validation rules that the runtime expects from the message contract. Important examples include:
- `IndexRequest.Id`, `DeleteItemRequest.Id`, and `UpdateAclRequest.Id` must be present and non-blank
- `IndexRequest.SecurityTokens` and `UpdateAclRequest.SecurityTokens` must be non-empty and must not contain blank entries
- `IndexRequest.Files` and `IndexRequest.Properties` must be present, even when empty
- `IndexRequest.Properties` must not include an `Id` property because the document identifier is a first-class field
- `IngestionProperty.Type` values serialize using lower-case JSON tokens such as `string`

These rules matter because the package is not only a set of shapes. It is also the place where producers and runtime consumers share the same interpretation of what a valid queue message means.

## Compatibility rules

The package is a versioned contract surface, not just a convenience library.

For practical purposes, the following elements should be treated as compatibility-sensitive:
- queue-message JSON field names
- request discriminator values and payload-property names
- property-type token serialization
- helper semantics that claim to create canonical contract instances
- serializer behavior that emits or parses canonical queue-message JSON
- validator interpretations of what counts as a valid contract instance

That does not mean every future implementation change is a breaking change. Internal refactoring, documentation improvements, or clearer helper implementation details can evolve freely when they do not alter the observable contract story. What does count as a deliberate contract change is any change that would make an existing producer emit materially different canonical JSON, require a different set of authoring assumptions, or reinterpret previously valid or invalid payloads.

The version marker is therefore not a replacement for semantic versioning. It is a visible signal that the producer-facing contract should be discussed and evolved intentionally rather than incidentally.

## Responsibilities that stay outside the package

The contracts package stops at authoring and validation. It does not own:
- queue submission
- queue naming
- authentication or credential management
- provider discovery or routing
- deployment topology
- File Share or provider-specific security-token derivation
- journal identity, `ShadowId`, dead-letter metadata, supersession, or replay metadata

Those exclusions are not omissions in the documentation. They are part of the package design. A producer supplies queue-message data only; the wider Search runtime and the producer’s host environment supply the rest.

## Human-readable examples

The checked-in fixture files under `test/UKHO.Search.Ingestion.Contracts.Tests/Fixtures/` are the canonical human-readable examples of the supported envelope JSON shapes:
- `index-item-envelope.json`
- `delete-item-envelope.json`
- `update-acl-envelope.json`

Those fixtures are exercised by tests so they remain executable examples rather than drifting into stale documentation.

For a repository-level framing guide that explains how this package fits into the wider Search architecture and where queue submission concerns begin, see the producer guide in [Remote-Ingestion-Producer-Guide](../../wiki/Remote-Ingestion-Producer-Guide.md).

## What this package does not do

This package does not:
- submit queue messages
- choose queues, providers, or routing topology
- acquire credentials or authenticate against Azure resources
- derive File Share or provider-specific security tokens
- implement ingestion pipeline behavior, journal handling, replay, dead-letter flow, or runtime orchestration
- expose Studio, Workbench, host, or infrastructure APIs

If a producer needs help with queue naming, message submission, retries, deployment topology, or provider selection, that concern belongs outside this package.

## Dependency boundary

The package remains intentionally dependency-light. It may depend on:
- the .NET base class library
- in-box `System.Text.Json` APIs that ship with the target framework

It must not reference:
- other Search solution projects
- Azure SDK packages
- queue, blob, SQL, or Elasticsearch client libraries
- logging abstractions
- Aspire, App Configuration, or host wiring libraries
- Studio, Blazor, React, RulesWorkbench, or provider implementation assemblies

Those boundary rules are enforced by `test/UKHO.Search.Ingestion.Contracts.Tests`, which fails if project or package references are introduced into the contracts project.