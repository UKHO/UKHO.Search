# `FileShareImageBuilder`

## What this tool is

`FileShareImageBuilder` is a small console utility that connects to an environment’s File Share Service and supporting storage, downloads/exports a set of file-share content, and packages that content into a Docker “data image”.

That data image is intended to be used by the Aspire orchestration to seed the File Share Emulator’s `/data` volume (so the emulator can read real files quickly during local development and testing).

## Where the resulting image goes (Docker Desktop)

When the export step runs it performs a `docker build` using a generated Dockerfile and tags the resulting image as:

- `fss-data-<environment>`

For example, with `"environment": "vnext-e2e"` the image name will be:

- `fss-data-vnext-e2e`

You can find it in Docker Desktop under **Images**, or via the CLI:

- `docker images | findstr fss-data-`

The tool also saves the image to a tar file in the `dataImagePath` directory:

- `<dataImagePath>\fss-data-<environment>.tar`

## Important: keep AppHost parameters in sync

The Aspire AppHost (`src\UKHO.ADDS.Search.AppHost\appsettings.json`) controls which data image will be used to seed the File Share Emulator.

The following values MUST correspond:

- `Parameters:environment` is passed to this tool as an environment variable named `environment`.
- `Parameters:emulator-data-image` MUST match the image name this tool produces (by default `fss-data-<environment>`).

If these are not kept in sync, the emulator may seed from an unexpected data image (or fail to find the expected content).

## Required local configuration (not checked in)

You must create `configuration.override.json` locally.

This file is **intentionally not committed to source control**, because it contains environment-specific settings and may contain sensitive details.

Create it at:

- `src\FileShareImageBuilder\configuration.override.json`

To get started, copy the checked-in `configuration.json` to `configuration.override.json` and then update the values for your environment:

- Copy `src\FileShareImageBuilder\configuration.json` to `src\FileShareImageBuilder\configuration.override.json`

`configuration.override.json` is the file you should treat as local-only.

## `configuration.override.json` parameters

| Setting | Purpose |
|---|---|
| `environment` | Environment name used to tag the generated image. When run from the Aspire dashboard this is provided via the `environment` environment variable (from AppHost). Only set this in `configuration.override.json` if you run the tool outside Aspire. |
| `sourceDatabase` | SQL connection string to the source metadata database used during the build process (typically an existing environment database). |
| `remoteService` | Base URL for the remote File Share Service to read content from (e.g. `https://files...`). |
| `tenantId` | Microsoft Entra tenant ID used for interactive authentication. |
| `clientId` | Client/application ID used for authentication scopes when acquiring tokens. |
| `dataImagePath` | Local working directory used to write intermediate files and outputs (including the final `*.tar`). |
| `dataImageBinSizeGB` | Target maximum size (in GB) for each exported “bin” of data. |
| `dataImageCount` | Maximum number of images/bins to generate (used to limit export volume). |

## How to run (from the Aspire dashboard)

1. Start the Aspire AppHost (`UKHO.ADDS.Search.AppHost`).
2. In the Aspire dashboard, locate the resource named `FileShareBuilder` (this is the `FileShareImageBuilder` project).
3. Select **Start** (it is configured as **Explicit Start** so it will not run automatically).
4. Monitor logs in the dashboard. On completion you should see messages indicating the Docker image has been built and saved.

After it completes, the image will be available locally in Docker Desktop as `fss-data-<environment>`.
