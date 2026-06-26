# Work Package Overview — Emulator Download

**Target output path:** `dev/work-packages/mvp/018-emulator-download/overview.spec.md`

## 1. Overview

This work package extends the `FileShareEmulator` (Blazor) tool with a new `Downloads` feature, enabling a user to download a batch ZIP from the emulator’s configured Azure Blob Storage to a configured local folder.

## 2. System context

The `FileShareEmulator` is a UI-driven emulator used to support development/testing workflows. It already integrates with Azure Storage (Blob/Queue) and exposes some batch-related capabilities.

The new `Downloads` UI capability will integrate with the existing blob storage layout used for batch ZIPs and will use emulator configuration for the target local download folder.

## 3. Components

- **Downloads Page (Blazor UI)**
  - New page allowing a user to enter a `BatchId` and download the corresponding batch ZIP.
  - Spec: `dev/work-packages/mvp/018-emulator-download/emulator-download.spec.md`

