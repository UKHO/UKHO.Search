# Specification: WP125 Minimal Technical Observability For The Browser Hosts

Target output path: `dev/work-packages/125-minimal-technical-observability/spec-domain-minimal-technical-observability.md`

Date: 2026-07-01

Repository note:
The folder name is retained for numbering continuity. The canonical decision in this file supersedes the earlier single-public-host assumption.

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md](../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the minimum technical observability baseline for the browser-host direction.

The current recommendation is:
- `QueryServiceHost` and the new `WorkbenchHost` both opt into the repository's shared ServiceDefaults and OpenTelemetry direction,
- both hosts expose health/readiness and minimal technical metadata,
- both hosts provide request correlation and authorization-failure visibility,
- and business audit remains explicitly deferred.

### 1.2 Scope

In scope for WP125:
- define the minimum technical observability expected from `QueryServiceHost` and `WorkbenchHost`,
- require alignment with the shared ServiceDefaults telemetry path,
- define health/readiness and basic host diagnostics expectations,
- and define request-correlation and authorization-failure visibility at a high level.

Out of scope for WP125:
- business audit requirements,
- workflow-specific audit events,
- detailed telemetry taxonomy for every future endpoint,
- or environment-specific observability hardening.

### 1.3 Stakeholders

- Platform owners responsible for Aspire and OpenTelemetry conventions.
- Public-host authors.
- Internal-host authors.
- Later hardening work that may add richer telemetry or audit once the platform is working.

### 1.4 Definitions

- Technical observability: Health, readiness, logging, traces, metrics, correlation, and diagnostics needed to operate and debug a host.
- Shared ServiceDefaults path: The common repository host path used to wire defaults such as telemetry.
- Business audit: Deliberate audit records for operator or workflow actions. This remains out of scope here.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) and [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) already use `AddServiceDefaults()`.
- Repository guidance in [../../../wiki/Metrics-in-the-Aspire-Dashboard.md](../../../wiki/Metrics-in-the-Aspire-Dashboard.md) documents the shared Aspire and OpenTelemetry direction.
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes the split between the public and internal browser hosts.

### 2.2 Proposed state

The recommended direction is:
- both `QueryServiceHost` and `WorkbenchHost` use the repository-standard ServiceDefaults path,
- both hosts participate in the shared OpenTelemetry telemetry system,
- both hosts expose health/readiness and minimal technical metadata,
- and both hosts make authorization failures and route-path issues diagnosable.

### 2.3 Assumptions

- The repository already has a workable shared telemetry direction.
- Both browser hosts need a technical observability baseline from the start.
- Business audit is important later but should not be invented prematurely in this planning slice.

### 2.4 Constraints

- The observability baseline must stay minimal enough to avoid speculative audit requirements.
- The baseline must still be strong enough to debug startup, routing, and auth failures in both hosts.
- FileShareEmulator remains outside the product-host capability surface, so its emulator-only diagnostics are not themselves product-host requirements.

## 3. Key decisions

- `QueryServiceHost` and `WorkbenchHost` both opt into the shared ServiceDefaults and OpenTelemetry path.
- Both hosts expose health/readiness and minimal technical metadata.
- Both hosts provide request correlation and authorization-failure visibility.
- Logs, traces, and metrics are all part of the baseline.
- Business audit remains deferred.

## 4. Functional requirements

FR1. `QueryServiceHost` shall expose health and readiness endpoints suitable for startup and operational checks.

FR2. `WorkbenchHost` shall expose health and readiness endpoints suitable for startup and operational checks.

FR3. Both hosts shall expose minimal technical metadata needed for diagnostics and startup proof.

FR4. Both hosts shall opt into the repository's shared ServiceDefaults telemetry path.

FR5. Both hosts shall participate in the shared OpenTelemetry telemetry system.

FR6. The baseline shall include logs, traces, and metrics rather than health endpoints alone.

FR7. Both hosts shall support request correlation across their deliberate routes.

FR8. Both hosts shall make authorization failures and similar request-path failures diagnosable through technical telemetry.

FR9. WP125 shall not define business audit requirements.

## 5. Non-functional requirements

NFR1. The observability baseline shall align with repository conventions already used in active hosts.

NFR2. The baseline shall be broad enough to support real debugging from the first implementation.

NFR3. The baseline shall avoid over-specifying workflow audit or endpoint-by-endpoint telemetry taxonomy prematurely.

## 6. Data model

Required categories of technical observability output:
- health and readiness state,
- minimal technical host metadata,
- logs,
- traces,
- metrics,
- request correlation context,
- and authorization-failure diagnostics.

## 7. Interfaces and integration

### 7.1 Host integration rules

Both browser hosts should integrate with the shared host defaults path for telemetry wherever practical.

### 7.2 Deferred concerns

Workflow-specific audit events, replay audit trails, promotion audit records, and equivalent business-facing observability remain explicitly deferred.

## 8. Observability

WP125 itself is the observability spec for Arc 02's first phase. The baseline must include:
- health/readiness visibility,
- minimal technical metadata,
- logs,
- traces,
- metrics,
- request correlation,
- and authorization-failure visibility.

## 9. Security and compliance

WP125 does not define business audit or retention rules. Its security contribution is limited to ensuring that authentication, authorization, and routing failures are diagnosable in technical telemetry.

## 10. Testing strategy

Validation anchors:
- confirm both browser hosts opt into the shared telemetry path,
- confirm health/readiness and minimal metadata endpoints exist,
- confirm request correlation and auth-failure visibility are observable,
- and confirm later hardening work can extend this baseline without redesigning it.

## 11. Rollout and migration

Recommended migration posture:
1. keep the repository-standard telemetry path,
2. apply it to both browser hosts,
3. expose health/readiness and minimal metadata early,
4. defer business audit until the platform workflows are settled.

Wiki review result:
No wiki page update was required for this planning work package. The work records a target observability baseline rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP125 now fixes the minimum technical observability baseline for both browser hosts without introducing premature business-audit requirements.