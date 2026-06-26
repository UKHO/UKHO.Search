---
description: Create a production-hardening plan for UKHO.ADDS.Management demo code.
mode: agent
context:
  language: markdown
---

# Purpose
You are a senior software engineer working in the `UKHO.ADDS.Management` repository.

The existing codebase is effectively demo/reference code. Your job is to produce a **production hardening plan** that can be executed to raise the solution to production quality.

**Output only a plan** (no code changes in this run). The plan must be structured similarly to other plans in this repository (see `dev/work-packages/*/plans/` for examples). If no examples exist, create a plan that matches the repository style: clear title, baseline, delta, carry-over, numbered work items, and validation steps.

# Hardening mindset
Think in terms of incrementally upgrading a working demo into a secure, resilient, observable, maintainable, testable, and operable system.

Prefer minimal, safe, reviewable steps. Avoid big-bang rewrites.

# Required repo-aware preparation (read-only)
Before writing the plan:
1. Read the top-level `README.md`.
2. Identify solution projects and their roles (AppHost, Host, Shell, Modules, ServiceDefaults, Tests, Mocks).
3. Inspect configuration patterns: `appsettings*.json`, `launchSettings.json`, Aspire configuration, Keycloak realm import, and any secret placeholders.
4. Locate existing cross-cutting infrastructure (logging, health checks, authN/Z, HTTP clients, validation, exception handling, OpenAPI, rate limiting).
5. Locate test projects and current coverage.

# Scope for the plan
Cover the following areas, tailoring to what the repo actually contains:

## 1) Security
- Secret handling: remove hardcoded/demo credentials; use user-secrets/dev-only defaults; production secret sources.
- Authentication & authorization: Keycloak/OIDC configuration, token validation, claims/roles mapping, least privilege.
- Input validation and output encoding (Blazor + APIs).
- CSRF/CORS/cookie settings where applicable.
- Dependency and container scanning strategy.

## 2) Reliability & resilience
- Health checks that reflect real dependencies and module readiness.
- Timeouts, retries, and circuit breakers for outbound calls.
- Graceful shutdown and startup ordering (Aspire resources).
- Background tasks and hosted services safety.

## 3) Observability
- Structured logging with consistent correlation (trace/span ids).
- OpenTelemetry traces/metrics/logs configuration.
- Dashboard-friendly resource naming and health states (Aspire).
- Audit logging for admin actions.

## 4) Configuration & environments
- Environment-specific config shaping (Dev/Test/Prod).
- Feature flags for modules.
- Safer defaults: HTTPS enforcement, HSTS in prod, secure headers.

## 5) API & UI hardening
- Versioning and OpenAPI/Swagger policies.
- Error handling and standardized problem details.
- Improved UX for auth failures and partial outages.
- Accessibility and performance basics in Blazor.

## 6) Quality gates
- Unit tests for core logic and services.
- Integration tests for auth flows and key endpoints.
- Static analysis and formatting: analyzers, `dotnet format`, nullable warnings.
- CI pipeline steps to enforce gates.

## 7) Operational readiness
- Container hardening (non-root, minimal images, pinned tags, SBOM where applicable).
- Deployment config (Aspire/containers) and runbooks.
- Logging/metrics docs and incident response basics.

# Plan format requirements
Produce a plan document with:
- A clear title that includes the area and date.
- **Baseline**: current state summary grounded in what you found.
- **Goals/Non-goals**: what this hardening effort will and won�t change.
- **Delta**: enumerated changes grouped by theme (security, reliability, observability, etc.).
- **Carry-over**: work explicitly deferred.
- **Work items**: numbered list where each item is actionable, reviewable, and includes:
  - scope (projects/components)
  - concrete tasks
  - acceptance criteria
  - validation (build/tests/manual checks)
- **Risks & assumptions**.
- **Validation checklist** at the end.

Keep the plan pragmatic and specific: reference actual project names and files when you can, but do not invent files.

# Output rules
- Output must be **markdown only**.
- Do not include code blocks unless listing file paths, commands, or configuration keys.
- Do not perform edits or propose changes that require secrets.
