# Work Package: 004-ingestion-model-uplift — Ingestion Request Model Uplift

**Target output path:** `docs/004-ingestion-model-uplift/spec-overview-ingestion-model-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

This work package uplifts the ingestion request domain model so the ingestion pipeline can represent multiple operations (add, update, delete, ACL update) under a single polymorphic, JSON-serializable “envelope” type.

The uplift introduces explicit request types:
- `AddItemRequest`
- `UpdateItemRequest`
- `DeleteItemRequest`
- `UpdateAclRequest`

…and introduces a new `IngestionRequest` envelope which:
- includes an `IngestionRequestType` discriminator, and
- carries exactly one of the request payloads as a named property.

The JSON contract must be supported with `System.Text.Json` and preserve existing behaviour that `null` properties are omitted during serialization.

This uplift also removes the use of `IngestionPropertyType` = `id` by introducing `Id` as a first-class, mandatory C# property in the relevant request types.

## 2. Goals and Non-Goals

### Goals
- Rename the existing `IngestionRequest` domain model (and associated extension methods) to `AddItemRequest`.
- Add `SecurityTokens` to `AddItemRequest`.
- Introduce `UpdateItemRequest` with the same shape as `AddItemRequest`.
- Remove `id` from the supported `IngestionPropertyType` set, replacing it with first-class `Id` properties on the appropriate request types.
- Add `Text` as a supported `IngestionPropertyType` for human-readable text values distinct from `String`.
- Introduce `DeleteItemRequest` containing a single mandatory `Id` string.
- Introduce `UpdateAclRequest` containing mandatory `Id` string and `SecurityTokens`.
- Introduce a new `IngestionRequest` envelope that can be serialized/deserialized to/from JSON, supporting polymorphism using an `IngestionRequestType` enum discriminator.
- Ensure JSON output omits `null` properties (consistent with existing model behaviour).

### Non-Goals
- Defining ingestion API endpoint(s), queue message wiring, or transport-level concerns.
- Defining token generation, signing, or validation semantics beyond structural constraints.
- Defining indexing behaviour, field mappings, or business logic executed by ingestion.

## 3. High-level Components

This work package produces the following domain-level components (each defined in a separate specification document):

1. `AddItemRequest`
   - Spec: `docs/004-ingestion-model-uplift/spec-domain-add-item-request_v0.01.md`
2. `UpdateItemRequest`
   - Spec: `docs/004-ingestion-model-uplift/spec-domain-update-item-request_v0.01.md`
3. `DeleteItemRequest`
   - Spec: `docs/004-ingestion-model-uplift/spec-domain-delete-item-request_v0.01.md`
4. `UpdateAclRequest`
   - Spec: `docs/004-ingestion-model-uplift/spec-domain-update-acl-request_v0.01.md`
5. `IngestionRequest` envelope + `IngestionRequestType`
   - Spec: `docs/004-ingestion-model-uplift/spec-domain-ingestion-request-envelope_v0.01.md`
