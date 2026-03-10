# Architecture

Work Package: `docs/010-ingestion-uplift/`

Related spec: `docs/010-ingestion-uplift/spec-ingestion-request-file-metadata_v0.01.md`

## Overall Technical Approach
- Extend the ingestion request domain models (`src/UKHO.Search.Ingestion`) to include:
  - A batch-level `Timestamp` (`DateTimeOffset`) on `AddItemRequest` and `UpdateItemRequest`.
  - A mandatory `Files` list describing source file metadata (`IngestionFileList` of `IngestionFile`).
- Keep serialization/deserialization consistent across producers/consumers by continuing to use `System.Text.Json` with `IngestionJsonSerializerOptions.Create()`.
- Producer-side population is implemented in the FileShareEmulator (`tools/FileShareEmulator`) by reading authoritative SQL columns and failing fast on invalid data.

```mermaid
flowchart LR
  subgraph Emulator[FileShareEmulator (tools)]
    SQL[(SQL DB)] -->|read Batch.CreatedOn| IDX[IndexService]
    SQL -->|read File rows by BatchId| IDX
    IDX -->|serialize IngestionRequest JSON| Q[(Azure Storage Queue)]
  end

  subgraph Ingestion[Ingestion pipeline (hosts/services)]
    Q -->|dequeue| CON[Ingestion source node]
    CON -->|deserialize with IngestionJsonSerializerOptions| MODEL[AddItemRequest/UpdateItemRequest]
    MODEL --> DOWN[Downstream processing (out of scope)]
  end
```

## Frontend
- Not applicable for this work package.

## Backend
### Domain model (Ingestion)
- Project: `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`
- Key types (domain contract):
  - `AddItemRequest` and `UpdateItemRequest` are extended with:
    - `Timestamp: DateTimeOffset`
    - `Files: IngestionFileList`
  - `IngestionFile` provides per-file metadata (`Filename`, `Size`, `Timestamp`, `MimeType`).
  - `IngestionFileList` is a concrete collection that serializes as a JSON array.
- Validation strategy:
  - Use `[JsonRequired]` + `IJsonOnDeserialized` validation in the request and file types so invalid/missing required fields throw `JsonException` during deserialization, matching existing ingestion model behaviour.

### Producer implementation (FileShareEmulator)
- Project: `tools/FileShareEmulator/FileShareEmulator.csproj`
- Key service:
  - `tools/FileShareEmulator/Services/IndexService.cs`
- Data sourcing:
  - `AddItemRequest.Timestamp` is read from `[Batch].[CreatedOn]` for the `batchId`.
  - `AddItemRequest.Files` is populated from `[File]` rows filtered by `BatchId`.
- Failure mode:
  - If required columns are null/invalid, request construction fails and the message is not enqueued (avoid partial messages).

### Serializer alignment
- Shared options:
  - `src/UKHO.Search.Ingestion/Requests/Serialization/IngestionJsonSerializerOptions.cs`
- All ingestion request JSON should be produced/consumed using these options to keep the envelope stable and explicitly versionable.

## Produced message shape (AddItem)
The FileShareEmulator enqueues an `IngestionRequest` JSON envelope. For `AddItem`, the payload MUST include `Timestamp` and `Files`.

```json
{
  "RequestType": "AddItem",
  "AddItem": {
    "Id": "<batch-guid>",
    "Timestamp": "2026-03-05T10:15:30+00:00",
    "Files": [
      {
        "Filename": "a.txt",
        "Size": 123,
        "Timestamp": "2026-03-05T10:15:31+00:00",
        "MimeType": "text/plain"
      }
    ],
    "Properties": [],
    "SecurityTokens": [
      "token-a"
    ]
  }
}
```

Notes:
- `Files` is required but may be empty (`[]`).
- All `Timestamp` values are ISO-8601 `DateTimeOffset` strings sourced from SQL values as-is.
