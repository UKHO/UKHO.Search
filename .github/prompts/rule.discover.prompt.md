# Ingestion rule discovery spec generator prompt (database-driven; produces a markdown discovery document)

Target output path: `docs/xxx-<work-item-descriptor>/spec-domain-rule-discovery_v0.01.md`

Copy/paste the prompt below into ChatGPT/Copilot Chat when you want the assistant to connect to the local SQL Server database, analyse the source data, and generate a **single markdown discovery specification** for future ingestion rule authoring.

---

## Reference prompt

You are generating a **single markdown discovery specification document** to identify the ingestion rules that will likely need to be written later.

This is a **discovery exercise only**.

Do **not** write ingestion rules, rule JSON files, or implementation code. Only produce the markdown discovery document content.

The source data is stored in a local SQL Server database using an **Entity Attribute Value** pattern:

- `BusinessUnit` -> parent grouping
- `Batch` -> individual batch within a business unit
- `BatchAttribute` -> key/value attributes for each batch

## Mandatory operating rules

1. **On every run, you MUST ask me for the connection string to the local SQL Server database before doing anything else.**
2. Do **not** run any database commands until I provide the connection string.
3. Once I provide the connection string, use **`sqlcmd`** to execute the required queries.
4. Keep me updated with progress as you work, especially if there are many business units or batches.
5. Design the queries so that **every row in `dbo.Batch` is covered** by the discovery output.
6. If a batch has no attributes, it must still be included and represented as an empty attribute-key set.
7. This workflow is for **discovery only**. Do not attempt to author final ingestion rules.

## Deliverable

Produce the full markdown content for a **single** discovery specification document in the active Work Package folder, for example:

- `docs/042-rule-discovery/spec-domain-rule-discovery_v0.01.md`

Do not modify rule JSON files. Only produce the markdown document content.

## Discovery objective

For each business unit, discover how its batches naturally split into different data shapes based on their attribute keys.

For each business unit you must:

1. Output a heading containing the **business unit name and id**.
2. Examine **every batch** in that business unit.
3. Determine the distinct set of `BatchAttribute.AttributeKey` values present for each batch.
4. Group batches by their **distinct attribute-key set**.
5. For every distinct attribute-key set within the business unit, output:
   - a stable class identifier or label
   - how many batches belong to that class
   - one representative batch id
   - the sorted list of attribute keys in that class
   - an example markdown pipe table showing the representative batch's `AttributeKey` and `AttributeValue` pairs
6. Verify that the grouped class counts add up to the total number of batches for that business unit.
7. Include an overall coverage summary proving that **all batches in the database were covered**.

## Required working method

### Step 1 - Ask for connection string

Your first response in every run must ask for the SQL Server connection string.

Example:

> Please provide the SQL Server connection string for the local database. Once you share it, I will use `sqlcmd` to analyse the BusinessUnit / Batch / BatchAttribute data and generate the discovery markdown document.

Do not ask any other question first unless the connection string is already present in the chat.

### Step 2 - Connect with `sqlcmd`

After I provide the connection string:

- derive the required `sqlcmd` arguments from it
- use `sqlcmd` for every query
- if `sqlcmd` is unavailable, stop and tell me exactly what is missing
- do not expose secrets back in the output; if you mention the connection, mask credentials

### Step 3 - Query efficiently

Prefer **set-based queries** that analyse the whole dataset or one business unit at a time.

Do **not** query one batch at a time unless it is only for a representative example batch after classification has already been computed.

Your query strategy must make it practical to cover all rows in `dbo.Batch`.

### Step 4 - Classify batches by attribute-key signature

For each batch, compute a deterministic signature based on the **sorted distinct** `BatchAttribute.AttributeKey` values for that batch.

Important:

- treat duplicate attribute keys within the same batch as one key for signature purposes
- sort keys alphabetically when building the signature
- treat batches with no attributes as an empty signature
- the signature is only for discovery/documentation; it is not itself a final rule

### Step 5 - Extract representative examples

For each discovered signature class, retrieve one representative batch and output its attributes in a markdown pipe table.

The table must contain:

- `AttributeKey`
- `AttributeValue`

If the representative batch has no attributes, explicitly state that the batch has no `BatchAttribute` rows.

### Step 6 - Validate coverage

You must prove coverage at two levels:

1. **Per business unit**: sum of discovered class counts must equal total batch count for that business unit.
2. **Overall**: sum of all discovered class counts across all business units must equal total row count from `dbo.Batch`.

If any mismatch appears, continue investigating until either:

- coverage is complete, or
- you can clearly explain the anomaly in the markdown document

### Step 7 - Keep the user updated

Because there may be many rows, provide concise progress updates while working, for example:

- how many business units have been analysed so far
- current business unit being processed
- whether coverage checks are passing

## Suggested SQL approach

Use a set-based approach similar to the following. You may adjust the exact SQL if needed for compatibility.

### Query A - business unit summary

```sql
SELECT
    bu.Id,
    bu.Name,
    COUNT(b.Id) AS BatchCount
FROM dbo.BusinessUnit bu
LEFT JOIN dbo.Batch b
    ON b.BusinessUnitId = bu.Id
GROUP BY bu.Id, bu.Name
ORDER BY bu.Id;
```

### Query B - compute batch signatures and class counts

Prefer `STRING_AGG` if supported. If not supported by the SQL Server version, use an equivalent XML/STUFF fallback.

```sql
WITH BatchDistinctKeys AS
(
    SELECT
        b.BusinessUnitId,
        b.Id AS BatchId,
        ba.AttributeKey
    FROM dbo.Batch b
    LEFT JOIN dbo.BatchAttribute ba
        ON ba.BatchId = b.Id
    GROUP BY
        b.BusinessUnitId,
        b.Id,
        ba.AttributeKey
),
BatchSignatures AS
(
    SELECT
        BusinessUnitId,
        BatchId,
        COALESCE(
            STRING_AGG(AttributeKey, '|') WITHIN GROUP (ORDER BY AttributeKey),
            ''
        ) AS AttributeKeySignature,
        COUNT(AttributeKey) AS AttributeKeyCount
    FROM BatchDistinctKeys
    GROUP BY
        BusinessUnitId,
        BatchId
),
SignatureClasses AS
(
    SELECT
        BusinessUnitId,
        AttributeKeySignature,
        AttributeKeyCount,
        COUNT(*) AS BatchCount,
        MIN(BatchId) AS ExampleBatchId
    FROM BatchSignatures
    GROUP BY
        BusinessUnitId,
        AttributeKeySignature,
        AttributeKeyCount
)
SELECT
    bu.Id AS BusinessUnitId,
    bu.Name AS BusinessUnitName,
    sc.AttributeKeySignature,
    sc.AttributeKeyCount,
    sc.BatchCount,
    sc.ExampleBatchId
FROM SignatureClasses sc
INNER JOIN dbo.BusinessUnit bu
    ON bu.Id = sc.BusinessUnitId
ORDER BY
    bu.Id,
    sc.BatchCount DESC,
    sc.AttributeKeySignature;
```

### Query C - fetch representative example attributes

```sql
WITH BatchDistinctKeys AS
(
    SELECT
        b.BusinessUnitId,
        b.Id AS BatchId,
        ba.AttributeKey
    FROM dbo.Batch b
    LEFT JOIN dbo.BatchAttribute ba
        ON ba.BatchId = b.Id
    GROUP BY
        b.BusinessUnitId,
        b.Id,
        ba.AttributeKey
),
BatchSignatures AS
(
    SELECT
        BusinessUnitId,
        BatchId,
        COALESCE(
            STRING_AGG(AttributeKey, '|') WITHIN GROUP (ORDER BY AttributeKey),
            ''
        ) AS AttributeKeySignature
    FROM BatchDistinctKeys
    GROUP BY
        BusinessUnitId,
        BatchId
),
ExampleBatches AS
(
    SELECT
        BusinessUnitId,
        AttributeKeySignature,
        MIN(BatchId) AS ExampleBatchId
    FROM BatchSignatures
    GROUP BY
        BusinessUnitId,
        AttributeKeySignature
)
SELECT
    eb.BusinessUnitId,
    eb.AttributeKeySignature,
    eb.ExampleBatchId,
    ba.AttributeKey,
    ba.AttributeValue
FROM ExampleBatches eb
LEFT JOIN dbo.BatchAttribute ba
    ON ba.BatchId = eb.ExampleBatchId
ORDER BY
    eb.BusinessUnitId,
    eb.AttributeKeySignature,
    ba.AttributeKey,
    ba.AttributeValue;
```

### Query D - coverage validation

Use one or more validation queries to prove:

- every batch contributes to exactly one signature class
- per-business-unit class totals match the business unit batch totals
- the grand total matches `SELECT COUNT(*) FROM dbo.Batch`

## Required markdown structure

Use this structure in the final output document:

# Ingestion rule discovery specification

## 1. Overview
- purpose of the discovery exercise
- source database summary
- statement that this document is for rule discovery only and does not define the final ingestion rules

## 2. Execution metadata
- execution date/time
- connection target summary with secrets masked
- total business unit count
- total batch count
- total batch attribute count

## 3. Coverage summary
A markdown table with at least:

- `BusinessUnitId`
- `BusinessUnitName`
- `BatchCount`
- `DiscoveredClassCount`
- `CoveredBatchCount`
- `CoverageStatus`

## 4. Business unit discovery
For each business unit, include:

### Business Unit: `<Name>` (`Id: <Id>`)

- total batch count
- number of discovered classes
- short note on coverage result

Then include a summary table for the discovered classes with columns such as:

- `Class`
- `BatchCount`
- `ExampleBatchId`
- `AttributeKeyCount`
- `AttributeKeySignature`

For each class, include:

#### Class `<n>`

- batch count
- representative batch id
- sorted list of attribute keys

Example attribute table:

| AttributeKey | AttributeValue |
|---|---|
| ... | ... |

If there are no attributes for that class, explicitly say so.

## 5. Cross-business-unit observations
- repeated signatures that appear in multiple business units
- notable differences between classes
- likely high-level data families to consider later when authoring rules

Do not write the rules themselves.

## 6. Coverage validation
- per-business-unit validation results
- overall validation result
- any anomalies or unresolved data issues

## 7. Appendix
- query notes
- assumptions
- table definitions used for analysis

## Output rules

- Output **one markdown document only**.
- Do not generate code files or rule JSON.
- Do not omit business units with zero batches.
- Do not omit batches with zero attributes.
- Keep attribute signatures deterministic by sorting keys.
- Prefer concise narrative and data-rich tables.
- If the dataset is large, summarise efficiently but still include every discovered class.
- If needed, continue iterating with more `sqlcmd` queries until coverage is complete.

## Table definitions

Use these definitions as the required schema reference for the queries:

```sql
CREATE TABLE [dbo].[BusinessUnit](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Name] [varchar](255) NOT NULL,
    [CostCentre] [varchar](16) NOT NULL,
    [StorageAccount] [varchar](32) NOT NULL,
    [IsActive] [bit] NOT NULL,
    [AcceleratedDownloadThresholdInBytes] [int] NOT NULL,
 CONSTRAINT [PK_BusinessUnit] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Batch](
    [Id] [uniqueidentifier] NOT NULL,
    [BusinessUnitId] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [CreatedOn] [datetime] NOT NULL,
    [CreatedBy] [varchar](64) NOT NULL,
    [CreatedByIssuer] [varchar](128) NULL,
    [CommittedOn] [datetime] NULL,
    [RolledBackOn] [datetime] NULL,
    [ExpiryDate] [datetime] NULL,
    [ZipSizeInBytes] [bigint] NULL,
    [IndexStatus] [int] NOT NULL,
 CONSTRAINT [PK_Batch] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[BatchAttribute](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [AttributeKey] [varchar](255) NOT NULL,
    [AttributeValue] [varchar](1024) NOT NULL,
    [BatchId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_BatchAttribute] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BatchAttribute]  WITH CHECK ADD  CONSTRAINT [FK_BatchAttribute_Batch_Id] FOREIGN KEY([BatchId])
REFERENCES [dbo].[Batch] ([Id])
```
