# `UKHO.ADDS.Search.FileShareEmulator`

## What this project is

This project hosts the File Share Emulator UI/service. In local development it is orchestrated by the Aspire AppHost and runs as a container built from the project `Dockerfile`.

The emulator expects its file content to be available under:

- `/data`

That folder is a Docker named volume mounted by the Aspire AppHost.

## Seed data and AppHost configuration

Seed data is provided by a Docker **data image** built by the `FileShareImageBuilder` tool.

The Aspire AppHost (`src\UKHO.ADDS.Search.AppHost\appsettings.json`) controls which data image will be used to seed the `/data` volume.

### Keep these values in sync

The following settings must correspond, otherwise the emulator can seed from the wrong image (or not find the expected content):

- AppHost `Parameters:environment` MUST match `FileShareImageBuilder` `configuration.override.json` `environment`.

The `environment` value is provided to the emulator as an environment variable named `environment` by the Aspire AppHost.

It corresponds to the `environment` value used by `FileShareImageBuilder` when the seed image was created (for example `vnext-e2e`).

## Readiness

The emulator exposes a readiness endpoint:

- `GET /health/ready`

Readiness is based on a sentinel file written by the seeding workflow:

- `/data/.seed.complete`

This prevents the emulator from reporting Ready until the data copy has completed.
