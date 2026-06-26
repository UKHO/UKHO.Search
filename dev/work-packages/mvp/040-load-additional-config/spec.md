# Specification: Extend `UKHO.Aspire.Configuration.Seeder` to load additional config (rules) into Azure App Configuration

**Target output path:** `./dev/work-packages/mvp/040-load-additional-config/spec.md`

## 1. Document control

| Field | Value |
|---|---|
| Work Package | `040-load-additional-config` |
| Artefact | Single spec document |
| Status | Draft |
| Audience | Developers / maintainers of local Aspire configuration tooling |

## 2. Overview (high-level only)

### 2.1 Purpose

`UKHO.Aspire.Configuration.Seeder` is an executable run as part of local Aspire orchestration to seed values into Azure App Configuration.

This change extends the Seeder and the local Aspire hosting integration so that additional configuration (often used for ingestion rules) can be loaded from a local directory and stored in Azure App Configuration.

Although examples in this document may reference “rules”, the mechanism is **generic** and the uplifted code is used in other projects. Implementation changes must therefore avoid rule-specific naming/terminology (for example, avoid “rules” in environment variable names, option names, method names, and log messages).

### 2.2 Goals

- Enable Aspire orchestration (`UKHO.Aspire.Configuration.Hosting`) to optionally pass an “additional configuration” directory path and prefix to the Seeder.
- Enable the Seeder to ingest a deep directory listing of the additional configuration directory and write each file into Azure App Configuration as a key/value.
- Ensure existing seeding behaviour is not altered.
- Support running the Seeder outside Aspire by adding optional command line parameters for the same additional configuration inputs.

### 2.3 Non-goals

- No changes to `DistributedApplicationBuilderExtensions.cs` `AddConfiguration()` method.
- No changes to any existing seed sources or key naming behaviour beyond the new opt-in “additional config” feature.
- No requirement to parse/validate specific “rule” formats; files are stored as raw string content.
- No introduction of rule-specific terminology into reusable components; treat this as generic “additional configuration” ingestion.

### 2.4 Assumptions / constraints

- The workspace includes Aspire-local orchestration and an Azure App Configuration emulator/service.
- The additional config inputs are optional and default to `string.Empty`.
- Additional configuration content is represented as files on disk and should be stored verbatim in App Configuration.

## 3. Current state (as understood)

- `UKHO.Aspire.Configuration.Hosting` provides extension methods for wiring a configuration emulator/service into local Aspire orchestration.
- `AddConfigurationEmulator()` currently provisions and/or wires the seeder into the distributed application.
- `UKHO.Aspire.Configuration.Seeder` seeds baseline configuration values.
- The Seeder can run either under Aspire (driven by environment variables) and/or standalone (driven by CLI args), following an established argument/env-var pattern.

## 4. Proposed changes

### 4.1 Hosting change: extend `AddConfigurationEmulator(...)` signature

**Component:** `UKHO.Aspire.Configuration.Hosting`

**Change:** Extend `DistributedApplicationBuilderExtensions.cs` `AddConfigurationEmulator()` method to accept:

- `additionalConfigurationPath` (string)
- `additionalConfigurationPrefix` (string)

**Defaults:**

- Both parameters default to `string.Empty`.

**Behaviour:**

- These values are passed to the Seeder as environment variables.
- This requires extending `WellKnownConfigurationName` to include names for these two environment variables.

**Explicitly out of scope:**

- Do not extend/modify `AddConfiguration()`.

### 4.2 Seeder change: conditional ingestion of “additional configuration” directory

**Component:** `UKHO.Aspire.Configuration.Seeder`

**Inputs:**

- Two new optional inputs corresponding to Aspire environment variables:
  - Additional configuration root directory path.
  - Additional configuration prefix.

**Invocation sources:**

- **Under Aspire:** passed as environment variables.
- **Standalone:** passed as two optional command line parameters, following existing CLI pattern.

**Opt-in:**

- If either value is null/empty, no additional ingestion occurs.
- If **both** values are **not** null/empty, the Seeder performs additional ingestion.

**Additional ingestion algorithm:**

1. Obtain a deep directory listing of the additional configuration root path.
2. For each file under the root:
   - Compute the file’s relative path segments beneath the root (0..n directories).
   - Compute `filename` as the file name **without** extension.
   - Write a key/value to Azure App Configuration:
     - **Key format:** `{prefix}:{path0}:...:{pathn}:{filename}`
       - `{prefix}` is the provided additional configuration prefix.
       - `{path0}..{pathn}` are subdirectories beneath the root path.
       - `{filename}` is file name without extension.
     - **Value:** the full **contents** of the file read as a string.

**Generic mechanism note:**

- The ingestion mechanism is generic and must not reference “rules” in code. “Rules” are just one example consumer of the additional configuration channel.

**Preservation of existing behaviour:**

- Existing seeding flows must remain unchanged.
- The additional ingestion must be additive and gated behind the opt-in inputs.

### 4.3 Command line parameters for standalone execution

**Requirement:**

- When the Seeder runs outside Aspire, add two optional command line parameters to provide the same:
  - `additionalConfigurationPath`
  - `additionalConfigurationPrefix`

**Constraints:**

- Follow the established pattern in the Seeder codebase for defining and reading optional parameters.
- CLI parameters must not be required; default behaviour remains identical to today.

## 5. Functional requirements

### FR-1: Extend `AddConfigurationEmulator()` to accept additional config parameters

- The method accepts two additional optional string parameters (defaults `string.Empty`).
- The method passes these values into the seeder’s environment using well-known variable names.

### FR-2: Add well-known env var names

- Extend `WellKnownConfigurationName` (or equivalent constants) to define environment variable names for:
  - AdditionalConfigPath
  - AdditionalConfigPrefix

### FR-3: Seeder ingests additional directory when both values are provided

- When both additional values are non-null/non-empty:
  - Enumerate all files recursively under the root.
  - For each file, write `{prefix}:{pathSegments}:{filenameWithoutExtension}` with raw file content.

### FR-4: Seeder no-ops additional ingestion when values not provided

- When either value is null/empty:
  - Skip additional ingestion.
  - Continue with existing seeding behaviour unchanged.

### FR-5: Standalone Seeder supports CLI args for additional config

- Add two optional CLI args to supply the additional prefix/path.
- Environment variables remain supported (for Aspire), and CLI should align with current precedence rules/pattern (whatever existing pattern dictates).

## 6. Technical requirements

### TR-1: Key normalization and delimiters

- Key delimiter is `:` exactly as specified.
- Do not include file extension in the final key segment.
- Relative directory segments use directory names as they appear on disk.

### TR-2: File reading

- Read file content as a string.
- Encoding approach should match existing repo patterns; if not specified, use .NET defaults consistent with current code.

### TR-3: Directory traversal

- Use a recursive file enumeration to obtain a deep listing.
- Ignore directories; operate only on files.

### TR-4: Error handling

- Preserve existing error handling semantics.
- Additional ingestion should not alter existing functional flow; failures should be handled in a way consistent with existing seeding steps.

### TR-5: No changes to existing functionality

- Existing seed sources and their output keys/values must remain unchanged.

## 7. Data mapping

### 7.1 Example mapping

Given:

- `additionalConfigurationPrefix = "ingestion-rules"` *(example only; code must remain generic)*
- `additionalConfigurationPath = C:\config\additional`

Files:

- `C:\config\additional\rules\charts\avcs.json`
- `C:\config\additional\rules\catalogue\default.yaml`

Keys and values:

- Key: `ingestion-rules:rules:charts:avcs`
  - Value: contents of `avcs.json`
- Key: `ingestion-rules:rules:catalogue:default`
  - Value: contents of `default.yaml`

## 8. Interfaces

### 8.1 Hosting API surface (conceptual)

- `AddConfigurationEmulator(..., string additionalConfigurationPath = "", string additionalConfigurationPrefix = "")`

#### Usage example (Aspire host)

```csharp
builder.AddConfigurationEmulator(
    serviceName: "EFS",
    configurationAwareProjects: configurationAwareProjects,
    externalServiceMocks: externalServiceMocks,
    configJsonPath: "./config/appsettings.local.json",
    externalServicesPath: "./config/external-services.local.json",
    additionalConfigurationPath: "./config/additional",
    additionalConfigurationPrefix: "additional");
```

### 8.2 Environment variables

- `WellKnownConfigurationName.AdditionalConfigurationPath`
- `WellKnownConfigurationName.AdditionalConfigurationPrefix`

### 8.3 CLI arguments

- Two optional command line parameters matching the above (names follow existing pattern in Seeder).

#### Usage example (standalone)

Command-line argument positions follow the existing Seeder pattern (values `0..4` are required; `5..6` are optional):

```powershell
dotnet run --project .\configuration\UKHO.Aspire.Configuration.Seeder\UKHO.Aspire.Configuration.Seeder.csproj -- \
  EFS dev .\config\appsettings.local.json .\config\external-services.local.json http://localhost:5200 \
  .\config\additional additional
```

## 9. Testing requirements

### 9.1 Unit / component test expectations

- Verify key generation from nested paths, including:
  - root file (no subdirectories)
  - multiple subdirectories
  - different extensions

### 9.2 Integration / local Aspire validation

- Run local Aspire orchestration with additional env vars set and validate:
  - keys appear in Azure App Configuration
  - baseline seeded keys remain the same

### 9.3 Standalone execution validation

- Run Seeder executable with new CLI args and validate the same result as Aspire env-var driven run.

## 10. Operational considerations

- This feature is designed primarily for local dev; ensure it can be disabled by default (empty values).
- Consider size/quantity of files; use streaming/efficient enumeration patterns consistent with current implementation.

## 11. Open questions (to be confirmed in implementation)

1. Should CLI args override environment variables, or vice versa (existing pattern will be followed)?
2. Should binary files be ignored, or is it acceptable that reading as string may fail (current spec assumes text files only)?
3. Should the seeder skip hidden/system files (not specified; default is include all files)?
