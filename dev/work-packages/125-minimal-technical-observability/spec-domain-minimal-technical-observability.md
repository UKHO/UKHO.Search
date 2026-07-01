# Specification: WP125 Minimal Technical Observability

Target output path: `dev/work-packages/125-minimal-technical-observability/spec-domain-minimal-technical-observability.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md](../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the minimum technical observability baseline for `PublicApiHost`.

The current recommendation is broader than a bare minimum health check. `PublicApiHost` must opt into the repository's Aspire/OpenTelemetry/OTLP telemetry path so that logs, traces, and metrics flow into the standard telemetry system. At the same time, WP125 remains intentionally narrow: it does not define business audit, approval flows, or operation-tracking requirements beyond the basic technical observability needed to start, diagnose, and operate the new host.

### 1.2 Scope

In scope for WP125:
- Define the minimum technical observability expected from `PublicApiHost`.
- Require opt-in to the shared Aspire/OpenTelemetry/OTLP telemetry path.
- Define health/readiness/system metadata expectations.
- Define request correlation, authorization-failure visibility, and route-level diagnostics at a high level.

Out of scope for WP125:
- Business audit requirements.
- Workflow-specific audit events for rule editing, replay, repair, or promotion.
- Fine-grained telemetry taxonomy for every future API endpoint.
- Environment-specific observability hardening.

### 1.3 Stakeholders

- Platform owners responsible for Aspire/OpenTelemetry conventions.
- Backend owners implementing `PublicApiHost`.
- Frontend authors who need reliable startup and health diagnostics.
- Later hardening work that may add richer telemetry or business audit once the platform is working.

### 1.4 Definitions

- Technical observability: Health, readiness, logging, traces, metrics, correlation, and diagnostics needed to operate and debug the host.
- OTLP: The OpenTelemetry Protocol used to export telemetry into the repository's Aspire-compatible telemetry flow.
- Shared ServiceDefaults path: The common host path used in this repository to wire default service behavior such as telemetry.
- Business audit: Deliberate business-facing audit records for operator or workflow actions. This is explicitly out of scope for WP125.

## 2. System context

### 2.1 Current state

The repository already has an Aspire/OpenTelemetry direction that new hosts should align with.

Evidence checked:
- Multiple active hosts such as [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs), [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs), and [../../../tools/FileShareEmulator/Program.cs](../../../tools/FileShareEmulator/Program.cs) use `AddServiceDefaults()`.
- Repository guidance in [../../../wiki/Metrics-in-the-Aspire-Dashboard.md](../../../wiki/Metrics-in-the-Aspire-Dashboard.md) states that hosts calling `AddServiceDefaults()` automatically configure OpenTelemetry metrics.
- The configuration emulator at [../../../configuration/UKHO.Aspire.Configuration.Emulator/Program.cs](../../../configuration/UKHO.Aspire.Configuration.Emulator/Program.cs) shows an explicit OTLP pattern for logs, metrics, and traces.
- Earlier repository work under `dev/work-packages/mvp/009-metric-integration` records the intent that ServiceDefaults should be the shared telemetry wiring path.

The current gap is that `PublicApiHost` does not exist yet, so its technical observability baseline must be defined before implementation begins.

### 2.2 Proposed state

The recommended direction is:
- `PublicApiHost` uses the repository-standard ServiceDefaults path,
- `PublicApiHost` opts into the Aspire/OpenTelemetry/OTLP telemetry system,
- `PublicApiHost` exposes health/readiness and minimal technical system metadata,
- and `PublicApiHost` provides enough correlation and route-level visibility for debugging and operational troubleshooting.

WP125 does not require business audit. It only requires a technical observability baseline that is good enough to build and operate the new platform safely.

### 2.3 Assumptions

- `PublicApiHost` should align with the existing Aspire/OpenTelemetry direction rather than inventing a parallel telemetry scheme.
- Technical observability is required before full business audit requirements are clear.
- OTLP is the right export baseline because this is an Aspire solution and the repository already uses OpenTelemetry-compatible telemetry flows.

### 2.4 Constraints

- The observability baseline must stay minimal enough not to invent speculative audit requirements.
- The host must still provide usable technical diagnostics from the start.
- `FileShareEmulator` remains outside the `PublicApiHost` capability surface, so its emulator-only diagnostics are not themselves requirements for the public host.

## 3. Component / service design (high level)

### 3.1 Components

WP125 defines four high-level deliverables:

1. Health and readiness surface
   - The host must be able to signal startup and availability state.

2. OTLP-connected telemetry path
   - Logs, traces, and metrics flow through the shared OpenTelemetry/Aspire path.

3. Request-level diagnostics
   - Correlation and authorization-failure visibility are available for troubleshooting.

4. Minimal system metadata
   - Enough version/profile-style information exists for startup and operational checks.

### 3.2 Data flows

Telemetry flow:
1. `PublicApiHost` emits technical logs, traces, and metrics.
2. The host participates in the repository-standard ServiceDefaults telemetry path.
3. Telemetry is exported through the Aspire/OpenTelemetry/OTLP system.
4. Operators and developers use that data for startup, routing, and auth troubleshooting.

Health flow:
1. The host exposes health/readiness and minimal technical metadata endpoints.
2. The React app and platform operators use those endpoints during startup and debugging.

### 3.3 Key decisions

- Recommendation: `PublicApiHost` must opt into the shared Aspire/OpenTelemetry/OTLP telemetry system.
- Recommendation: the host should use the shared ServiceDefaults path where practical rather than custom one-off telemetry wiring.
- Recommendation: the baseline includes logs, traces, and metrics rather than only health endpoints.
- Recommendation: defer detailed metric/tracing naming conventions and tag taxonomy to implementation-focused work as long as OTLP participation and the required technical categories are present.
- Recommendation: the baseline remains technical observability only and does not attempt to define business audit.
- Recommendation: request correlation and authorization-failure visibility are part of the initial baseline.

## 4. Functional requirements

FR1. `PublicApiHost` shall expose health and readiness endpoints suitable for startup and operational checks.

FR2. `PublicApiHost` shall expose minimal technical system metadata needed for UI startup and diagnostics.

FR3. `PublicApiHost` shall opt into the repository's shared Aspire/OpenTelemetry telemetry path.

FR4. `PublicApiHost` shall export technical telemetry through OTLP.

FR5. The initial technical observability baseline shall include logs, traces, and metrics rather than health endpoints alone.

FR6. `PublicApiHost` shall support request correlation across its public API routes.

FR7. `PublicApiHost` shall make authorization failures and similar request-path failures diagnosable through technical telemetry.

FR8. WP125 shall not define business audit requirements.

FR9. Business audit and operation-tracking concerns shall remain explicitly deferred until the platform is working and the business requirements are clearer.

FR10. Detailed metric/tracing naming conventions and tag taxonomy shall remain deferred to implementation-focused work.

## 5. Non-functional requirements

NFR1. The observability baseline shall align with Aspire/OpenTelemetry conventions already used in the repository.

NFR2. The observability baseline shall be broad enough to support real debugging and operations from the first implementation.

NFR3. The observability baseline shall avoid over-specifying audit or workflow-event requirements prematurely.

NFR4. OTLP export shall be treated as a required capability rather than an optional add-on.

NFR5. The baseline shall avoid premature standardization of metric/tracing names and tags before the implementation exists, as long as the required telemetry categories and OTLP participation are in place.

## 6. Data model

WP125 does not define a business event model. It defines categories of technical observability output.

Required categories:
- health/readiness state,
- technical system metadata,
- logs,
- traces,
- metrics,
- request correlation context,
- and authorization-failure diagnostics.

## 7. Interfaces & integration

### 7.1 Host integration rules

`PublicApiHost` should integrate with the repository-standard host defaults path for telemetry wherever possible.

### 7.2 Telemetry export rules

The host must not treat telemetry as local-process-only output. It must participate in the shared OTLP-capable pipeline used by the Aspire solution.

### 7.3 Deferred concerns

Workflow-specific audit events, replay audit trails, promotion audit records, and equivalent business-facing observability remain explicitly deferred.

## 8. Observability (logging/metrics/tracing)

WP125 itself is the observability spec for Arc 02's first phase.

The baseline must include:
- health/readiness visibility,
- minimal technical system metadata,
- structured or otherwise machine-usable logging,
- tracing through the OpenTelemetry path,
- metrics through the OpenTelemetry path,
- request correlation,
- and authorization-failure visibility.

The host must opt into OTLP export as part of that baseline.

## 9. Security & compliance

WP125 does not define business audit or compliance retention rules.

Its security contribution is limited to ensuring that authentication, authorization, and routing failures are diagnosable in technical telemetry.

## 10. Testing strategy

WP125 validation should focus on technical observability readiness.

Validation anchors:
- Confirm `PublicApiHost` opts into the repository-standard telemetry path.
- Confirm OTLP export is enabled through the expected observability path.
- Confirm health/readiness and minimal technical metadata endpoints exist.
- Confirm request correlation and authorization-failure visibility are observable.

## 11. Rollout / migration

Recommended migration posture:
1. Build `PublicApiHost` on the shared host-defaults path.
2. Ensure health/readiness and basic system metadata exist.
3. Ensure logs, traces, and metrics flow through OTLP into the Aspire telemetry system.
4. Add richer business audit only after the platform is working and real business requirements exist.

Wiki review result:
No wiki page update was required for this draft work-package specification. The work records the minimum technical observability baseline rather than a current-state implementation change.

## 12. Open questions

No open questions remain in WP125 at this stage. The required technical observability categories and OTLP participation are fixed here, while metric/tracing naming conventions, tag taxonomy, and any later business-audit expansion are intentionally deferred to later implementation-focused work.