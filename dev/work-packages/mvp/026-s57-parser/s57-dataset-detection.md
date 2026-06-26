# S-57 Dataset Detection (FileShare)

> Target: `dev/work-packages/mvp/026-s57-parser/s57-dataset-detection.md`

## 1. Overview

### 1.1 Purpose

Define how the FileShare ingestion provider should identify S-57 datasets in a batch, specifically by detecting and grouping `*.000`/`*.001`… files within `S57BatchContentHandler`.

### 1.2 Scope

- Extend `S100BatchContentHandler` to:
  - look for `*.000` files
  - treat files with the same base name and numeric extension (`.000`..`.999`) as belonging to the same S-57 dataset

In this repository, this behavior should be implemented by extending `S57BatchContentHandler` (not `S100BatchContentHandler`).

## 2. System context

### 2.1 Current state

`S57BatchContentHandler` is invoked for S-57 batches but currently only logs that it was called.

### 2.2 Proposed state

- Identify candidate S-57 datasets as any file ending in `.000`.
- For each candidate base name, include any sibling files with extensions:
  - `.001`, `.002`, … up to `.nnn` (3 digits)

## 3. Component / service design (high level)

### 3.1 Grouping rules

- A dataset is defined by:
  - `baseName` (filename without extension)
  - a set of member files matching `baseName + .[0-9][0-9][0-9]`
- The `.000` file is mandatory for a dataset group.

### 3.2 Ordering / determinism

- Member files should be ordered lexicographically (or numerically by extension) to ensure deterministic processing.

### 3.3 Failure handling

- If `.000` is present but other members are missing, parsing should still proceed using `.000`.
- If multiple `.000` files share the same base name (case-insensitive collision), define precedence rules (open question).

## 4. Functional requirements

- Detect S-57 datasets by `.000` presence.
- Enumerate sister files `.001`…`.nnn` for the same base name.
- Ensure S-57 datasets do not conflict with existing content detection rules.

## 5. Non-functional requirements

- No significant performance degradation scanning large batches.
- Deterministic grouping.

## 12. Open questions

1. Are dataset base names case-sensitive in the file share, or should grouping be case-insensitive?
2. Should `.000` only be considered if it matches a specific naming convention (e.g., ENC cell naming), or any `.000`?
