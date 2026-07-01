# Specification: WP104 Producer Guidance And Compatibility Rules

Target output path: `dev/work-packages/104-producer-guidance-compatibility-rules/spec-domain-producer-guidance-compatibility-rules.md`

Date: 2026-06-30

Source material:
- [../../specs/next-gen-arc01-wp.md](../../specs/next-gen-arc01-wp.md)
- [../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md](../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md)
- [../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md](../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md)
- [../102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md](../102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md)
- [../103-in-repo-consumer-contract-convergence/spec-domain-in-repo-consumer-contract-convergence.md](../103-in-repo-consumer-contract-convergence/spec-domain-in-repo-consumer-contract-convergence.md)
- [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)
- [../../src/UKHO.Search.Ingestion.Contracts/README.md](../../src/UKHO.Search.Ingestion.Contracts/README.md)

## 1. Overview

### 1.1 Purpose

This specification defines WP104, which publishes the final producer guidance and compatibility rules for `UKHO.Search.Ingestion.Contracts`.

WP100 established the package boundary. WP101 extracted the queue-message DTO and serializer contract. WP102 added producer-safe helpers, builder support, serializer facade entry points, and a non-throwing validator. WP103 converged the repository’s internal consumers on the extracted contract package. WP104 turns that completed technical surface into explicit producer guidance that explains how external .NET producers should use the package and how compatibility should be reasoned about over time.

### 1.2 Scope

In scope for WP104:
- Publish current-state guidance for remote .NET producers who need to construct and serialize queue messages without acquiring Search runtime dependencies.
- Explain the supported contract surface, helper APIs, serializer and validator entry points, and the contract-version marker in one coherent producer-facing story.
- Define the compatibility rules around queue-message shape, package responsibilities, queue submission boundaries, and what changes would count as a contract change.
- Add a minimal external-consumer-style sample validation path that references only the contracts project and proves that each supported operation can be created and serialized correctly.

Out of scope for WP104:
- Adding a transport SDK or queue-submission helper package.
- Changing queue-message JSON, helper behavior, or validation semantics already delivered by WP101 and WP102.
- Moving security-token derivation into the contracts package.
- Introducing public developer API guidance for browser-host clients, Studio, or later arc concerns.

### 1.3 Stakeholders

- Remote .NET producers who need to understand how to use `UKHO.Search.Ingestion.Contracts` correctly.
- Search platform maintainers who need explicit compatibility rules for evolving the contracts package.
- Documentation owners responsible for keeping package guidance current and coherent.
- Test owners who need a minimal external-consumer-style validation path for the final Arc 01 contract story.

### 1.4 Definitions

- Producer guidance: current-state documentation that teaches an external .NET producer how to author valid queue messages with the contracts package.
- Compatibility rule: an explicit statement about what aspects of the package and wire contract are stable, what is out of scope, and what kinds of future changes would count as a deliberate contract change.
- External-consumer-style sample: a sample or test path that references only the contracts package and proves that a producer can create and serialize the supported message operations without Search runtime dependencies.

## 2. System context

### 2.1 Current state

The technical package surface is now largely complete, but the final producer guidance and compatibility rules are still distributed across work package records and package-local documentation rather than captured as the completed Arc 01 producer story.

Evidence checked:
- [../../src/UKHO.Search.Ingestion.Contracts/README.md](../../src/UKHO.Search.Ingestion.Contracts/README.md) already describes the DTOs, serializer facade, helper APIs, builder, validator, and contract-version marker.
- [../../dev/work-packages/102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md](../../dev/work-packages/102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md) defines the helper and validator contract surface as part of the package.
- [../../dev/work-packages/103-in-repo-consumer-contract-convergence/spec-domain-in-repo-consumer-contract-convergence.md](../../dev/work-packages/103-in-repo-consumer-contract-convergence/spec-domain-in-repo-consumer-contract-convergence.md) records that the repository consumer estate now converges on the extracted package.
- The Arc 01 roadmap still calls for explicit guidance stating that queue submission helpers are separate, queue naming and authentication are external concerns, security-token derivation remains upstream, and producers do not own journal identity or replay metadata.

This means the code and package surface are ready, but the final producer-facing explanation of compatibility boundaries and usage rules still needs a dedicated work package to close Arc 01 cleanly.

### 2.2 Proposed state

After WP104:
- External producer guidance is published in a clear current-state form that explains what the contracts package is for, what it is not for, how to use it, and how to think about compatibility.
- The guidance explicitly distinguishes message authoring from queue submission, provider selection, authentication, topology, token derivation, and journal concerns.
- The repository has a minimal external-consumer-style validation path that proves each supported operation can be created and serialized using only the contracts package.
- Arc 01 ends with one coherent producer story rather than a set of partially distributed implementation notes.

### 2.3 Assumptions

- The package README is already the most natural place for producer-facing guidance, but the final published shape may still need confirmation.
- External producers need a concrete explanation of compatibility boundaries at least as much as they need API reference detail.
- The final Arc 01 guidance should describe current behavior, not future aspirations for optional queue-submission packages.

### 2.4 Constraints

- The guidance must stay aligned to the completed package surface and must not promise capabilities that are not actually implemented.
- WP104 must not blur the contracts package into a transport SDK.
- Compatibility rules must reflect current behavior and package ownership, not speculative future package shapes beyond clearly labelled optional follow-up ideas.

## 3. Component / service design (high level)

### 3.1 Components

WP104 affects three main areas:

1. Producer-facing package guidance
   - the canonical guidance page for remote .NET producers

2. Compatibility rules
   - explicit statements about stable contract elements, out-of-scope concerns, and what counts as a deliberate contract change

3. External-consumer-style validation path
   - a minimal sample or test proving the supported operations can be created and serialized through the contracts package alone

### 3.2 Data flows

Current state:
1. Producers can use the contracts package.
2. Guidance exists, but the final Arc 01 producer story is not yet explicitly closed out as a completed documentation slice.

Target state after WP104:
1. A producer reads the package guidance.
2. The producer understands which APIs to use for message authoring and which responsibilities remain external.
3. The producer can validate that understanding against a minimal external-consumer-style sample or test.
4. Maintainers can use the written compatibility rules to reason about future changes to the package.

### 3.3 Key decisions

- Guidance-placement decision: the package README remains the canonical producer guide, and WP104 also adds a repository-level producer guide page that points back to the package README and frames how external producers should approach the package.
- Current-state decision: guidance must explain only the actually delivered WP101-WP103 package surface.
- Boundary decision: queue submission, queue naming, authentication, provider queue selection, deployment topology, and security-token derivation remain outside the contracts package.
- Compatibility decision: the guidance must explain that changing queue-message JSON shape, helper semantics, serializer behavior, or validator interpretation is a deliberate contract change rather than incidental implementation detail.
- Journal-boundary decision: producers provide queue-message data only; ingestion owns later journal identity, outcome, dead-letter, supersession, and replay metadata.

## 4. Functional requirements

FR1. WP104 shall publish clear producer-facing guidance for `UKHO.Search.Ingestion.Contracts`.

FR1a. The canonical producer guidance shall remain in `src/UKHO.Search.Ingestion.Contracts/README.md`.

FR1b. WP104 shall also publish a repository-level producer guide page that points back to the package README and frames how external producers should approach the contracts package.

FR2. The guidance shall explain that queue submission helpers are a separate optional package concern and are not part of the core contracts package.

FR3. The guidance shall explain that queue naming, authentication, provider queue selection, and deployment topology are external concerns.

FR4. The guidance shall state that security-token derivation remains upstream in the current version of the contracts package.

FR5. The guidance shall explain that producers provide queue-message data only and do not own journal identity, outcome, dead-letter, supersession, or replay metadata.

FR6. The guidance shall explain the supported DTO, helper, serializer, validator, and contract-version-marker surface in current-state terms.

FR7. WP104 shall provide a minimal external-consumer-style validation path that references only the contracts project and proves that each supported operation can be created and serialized correctly.

FR8. WP104 shall document compatibility rules that explain what aspects of the package and wire contract are stable and what kinds of changes count as deliberate contract changes.

## 5. Non-functional requirements

NFR1. The guidance shall read as current-state producer documentation rather than as roadmap notes.

NFR2. The guidance shall preserve the boundary clarity achieved by WP100-WP103 and shall not imply package responsibilities that do not exist.

NFR3. The external-consumer-style validation path shall remain small, deterministic, and easy to run.

NFR4. The compatibility rules shall be explicit enough to guide maintainers and external consumers during future package evolution.

## 6. Data model

WP104 does not introduce a new contract model. It documents and validates the already delivered `UKHO.Search.Ingestion.Contracts` surface.

The documented surface includes:
- queue-message DTOs
- helper factories and builder support
- serializer facade and validator entry points
- contract-version marker

The documented exclusions include:
- queue clients and submission helpers
- provider policy such as File Share security-token derivation
- journal, replay, and dead-letter models

## 7. Interfaces & integration

### 7.1 Producer-facing integration model

External producers should be able to:
- reference only `UKHO.Search.Ingestion.Contracts`
- create `IndexItem`, `DeleteItem`, and `UpdateAcl` messages
- serialize those messages with package-owned serializer behavior
- validate those messages without needing Search runtime dependencies

### 7.2 Package boundary model

The guidance must explicitly reinforce that the contracts package stops at authoring and validation. It does not own:
- queue transport
- provider discovery
- authentication
- token derivation
- journal or replay behavior

### 7.3 Documentation publishing model

WP104 shall publish producer guidance in two coordinated layers:
- the package README as the canonical API- and usage-adjacent guide for consumers who already have the package in hand
- a repository-level producer guide page that explains where the package sits in the wider Search architecture, points readers at the canonical README, and reinforces the queue-submission and compatibility boundaries

## 8. Observability (logging/metrics/tracing)

WP104 does not require new observability primitives.

Any external-consumer-style sample should stay focused on package usage rather than on logging or runtime telemetry concerns.

## 9. Security & compliance

The guidance shall explain that producing a syntactically valid message does not imply authorization to submit it to a given queue or provider.

The guidance shall also state that secrets, credentials, and deployment-specific trust boundaries are outside the contracts package.

## 10. Testing strategy

WP104 validation should include:
- a minimal external-consumer-style sample or test path that references only the contracts project
- coverage proving that `IndexItem`, `DeleteItem`, and `UpdateAcl` can each be created and serialized through the final package surface
- verification that the documented producer entry points remain aligned with the actual package API

## 11. Rollout / migration

WP104 is primarily a documentation and package-usage closeout slice for Arc 01.

Expected rollout characteristics:
- no runtime behavior changes are required
- guidance and validation should close the Arc 01 producer story cleanly
- future optional queue-submission package work remains explicitly separate

## 12. Open questions

No open questions remain at the specification level for WP104.

The following implementation decision is now captured by the spec and should be treated as the working default unless later evidence forces a change:
- the package README remains the canonical producer guide, and WP104 also adds a repository-level producer guide page that points readers back to it and explains the wider package boundary and compatibility context