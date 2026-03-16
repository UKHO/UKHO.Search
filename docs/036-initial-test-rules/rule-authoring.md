# Ingestion rule specification (036-initial-test-rules)

Generated: 2026-03-16

## 1. Overview

### Goal / description
Define an initial set of ingestion rules for AVCS-related file-share payloads that enrich canonical fields (keywords, authority, category, series, instance, versions, search text) based on `businessunitname` and the presence of specific properties.

### Provider
- `file-share`

### Rule IDs
- `avcs-aio-exchange-set`
- `avcs-aio-catalogue`
- `avcs-bespoke-bess`

---

## Rule: `avcs-aio-exchange-set`

### 2. Inputs
Required/used payload paths:
- `properties["businessunitname"]` (required for match)
- `properties["exchange set type"]` (required for match; existence)
- `properties["year / week"]` (optional for outputs; used for `instance.add`)
- `properties["year"]` (optional for outputs; used for `majorVersion.add` via `toInt(...)`)
- `properties["week number"]` (optional for outputs; used for `minorVersion.add` via `toInt(...)`)

Normalization / parsing:
- `majorVersion.add` and `minorVersion.add` use `toInt(...)` because source values may be strings.
- If any referenced runtime data is missing, the rule either will not match (for match-critical inputs) or the specific output will be skipped (for output-only inputs).

### 3. Matching logic
Plain English:
- Match when `properties["businessunitname"]` equals `avcsdata` AND `properties["exchange set type"]` exists.

Draft `if` JSON:
```json
{
  "all": [
    {
      "path": "properties[\"businessunitname\"]",
      "equals": "avcsdata"
    },
    {
      "path": "properties[\"exchange set type\"]",
      "exists": true
    }
  ]
}
```

### 4. Outputs / actions
When matched:
- `keywords.add`: adds static keywords `avcs`, `enc`, `weekly`, `data`, `tpm`, `s57`, `aio`
- `authority.add`: adds `ukho`
- `category.add`: adds `exchange set`
- `series.add`: adds `aio`
- `instance.add`: adds the value at `properties["year / week"]` (if present)
- `majorVersion.add`: adds `toInt($path:properties["year"])` (if present and parseable)
- `minorVersion.add`: adds `toInt($path:properties["week number"])` (if present and parseable)
- `searchText.add`: adds `weekly data set uploaded by tpm for distributors`

Missing data behavior:
- If `properties["year / week"]`, `properties["year"]`, or `properties["week number"]` are missing/unparseable, only the affected outputs are skipped.

### 5. Examples
Example input payload fragment:
```json
{
  "properties": {
    "businessunitname": "avcsdata",
    "exchange set type": "AIO",
    "year / week": "2026/11",
    "year": "2026",
    "week number": "11"
  }
}
```

Expected outputs (canonical fields affected):
- `keywords`: `avcs`, `enc`, `weekly`, `data`, `tpm`, `s57`, `aio`
- `authority`: `ukho`
- `category`: `exchange set`
- `series`: `aio`
- `instance`: `2026/11`
- `majorVersion`: `2026`
- `minorVersion`: `11`
- `searchText`: includes `weekly data set uploaded by tpm for distributors`

### 6. Final rule JSON
Recommended file path:
- `src/Hosts/IngestionServiceHost/Rules/file-share/avcs-aio-exchange-set.json`

Final JSON document:
```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "avcs-aio-exchange-set",
    "description": "AVCS AIO exchange set: weekly dataset uploaded by TPM for distributors.",
    "if": {
      "all": [
        {
          "path": "properties[\"businessunitname\"]",
          "equals": "avcsdata"
        },
        {
          "path": "properties[\"exchange set type\"]",
          "exists": true
        }
      ]
    },
    "then": {
      "keywords": {
        "add": [
          "avcs",
          "enc",
          "weekly",
          "data",
          "tpm",
          "s57",
          "aio"
        ]
      },
      "authority": {
        "add": [
          "ukho"
        ]
      },
      "category": {
        "add": [
          "exchange set"
        ]
      },
      "series": {
        "add": [
          "aio"
        ]
      },
      "instance": {
        "add": [
          "$path:properties[\"year / week\"]"
        ]
      },
      "majorVersion": {
        "add": [
          "toInt($path:properties[\"year\"])"
        ]
      },
      "minorVersion": {
        "add": [
          "toInt($path:properties[\"week number\"])"
        ]
      },
      "searchText": {
        "add": [
          "weekly data set uploaded by tpm for distributors"
        ]
      }
    }
  }
}
```

---

## Rule: `avcs-aio-catalogue`

### 2. Inputs
Required/used payload paths:
- `properties["businessunitname"]` (required for match)
- `properties["catalogue type"]` (required for match; existence)
- `properties["year / week"]` (optional for outputs; used for `instance.add`)
- `properties["year"]` (optional for outputs; used for `majorVersion.add` via `toInt(...)`)
- `properties["week number"]` (optional for outputs; used for `minorVersion.add` via `toInt(...)`)

Normalization / parsing:
- `majorVersion.add` and `minorVersion.add` use `toInt(...)` because source values may be strings.
- If any referenced runtime data is missing, the rule either will not match (for match-critical inputs) or the specific output will be skipped (for output-only inputs).

### 3. Matching logic
Plain English:
- Match when `properties["businessunitname"]` equals `avcs-bespokeexchangesets` AND `properties["catalogue type"]` exists.

Draft `if` JSON:
```json
{
  "all": [
    {
      "path": "properties[\"businessunitname\"]",
      "equals": "avcs-bespokeexchangesets"
    },
    {
      "path": "properties[\"catalogue type\"]",
      "exists": true
    }
  ]
}
```

### 4. Outputs / actions
When matched:
- `keywords.add`: adds static keywords `avcs`, `catalog`, `catalogue`, `weekly`, `data`, `tpm`
- `authority.add`: adds `ukho`
- `category.add`: adds `catalogue`
- `series.add`: adds `avcs`
- `instance.add`: adds the value at `properties["year / week"]` (if present)
- `majorVersion.add`: adds `toInt($path:properties["year"])` (if present and parseable)
- `minorVersion.add`: adds `toInt($path:properties["week number"])` (if present and parseable)
- `searchText.add`: adds `weekly data set uploaded by tpm for distributors`

Missing data behavior:
- If `properties["year / week"]`, `properties["year"]`, or `properties["week number"]` are missing/unparseable, only the affected outputs are skipped.

### 5. Examples
Example input payload fragment:
```json
{
  "properties": {
    "businessunitname": "avcs-bespokeexchangesets",
    "catalogue type": "AVCS Catalogue",
    "year / week": "2026/11",
    "year": "2026",
    "week number": "11"
  }
}
```

Expected outputs (canonical fields affected):
- `keywords`: `avcs`, `catalog`, `catalogue`, `weekly`, `data`, `tpm`
- `authority`: `ukho`
- `category`: `catalogue`
- `series`: `avcs`
- `instance`: `2026/11`
- `majorVersion`: `2026`
- `minorVersion`: `11`
- `searchText`: includes `weekly data set uploaded by tpm for distributors`

### 6. Final rule JSON
Recommended file path:
- `src/Hosts/IngestionServiceHost/Rules/file-share/avcs-aio-catalogue.json`

Final JSON document:
```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "avcs-aio-catalogue",
    "description": "AVCS AIO catalogue: weekly dataset uploaded by TPM for distributors.",
    "if": {
      "all": [
        {
          "path": "properties[\"businessunitname\"]",
          "equals": "avcs-bespokeexchangesets"
        },
        {
          "path": "properties[\"catalogue type\"]",
          "exists": true
        }
      ]
    },
    "then": {
      "keywords": {
        "add": [
          "avcs",
          "catalog",
          "catalogue",
          "weekly",
          "data",
          "tpm"
        ]
      },
      "authority": {
        "add": [
          "ukho"
        ]
      },
      "category": {
        "add": [
          "catalogue"
        ]
      },
      "series": {
        "add": [
          "avcs"
        ]
      },
      "instance": {
        "add": [
          "$path:properties[\"year / week\"]"
        ]
      },
      "majorVersion": {
        "add": [
          "toInt($path:properties[\"year\"])"
        ]
      },
      "minorVersion": {
        "add": [
          "toInt($path:properties[\"week number\"])"
        ]
      },
      "searchText": {
        "add": [
          "weekly data set uploaded by tpm for distributors"
        ]
      }
    }
  }
}
```

---

## Rule: `avcs-bespoke-bess`

### 2. Inputs
Required/used payload paths:
- `properties["businessunitname"]` (required for match)
- `properties["exchange set type"]` (required for match; existence)
- `properties["frequency"]` (optional for outputs; used for `keywords.add` dynamic keyword)
- `properties["media type"]` (optional for outputs; used for `format.add`)
- `properties["id"]` (optional for outputs; used for `instance.add`)
- `properties["year"]` (optional for outputs; used for `majorVersion.add` via `toInt(...)`)
- `properties["week number"]` (optional for outputs; used for `minorVersion.add` via `toInt(...)`)

Normalization / parsing:
- `majorVersion.add` and `minorVersion.add` use `toInt(...)` because source values may be strings.
- If any referenced runtime data is missing, the rule either will not match (for match-critical inputs) or the specific output will be skipped (for output-only inputs).

### 3. Matching logic
Plain English:
- Match when `properties["businessunitname"]` equals `avcs-bespokeexchangesets` AND `properties["exchange set type"]` exists.

Draft `if` JSON:
```json
{
  "all": [
    {
      "path": "properties[\"businessunitname\"]",
      "equals": "avcs-bespokeexchangesets"
    },
    {
      "path": "properties[\"exchange set type\"]",
      "exists": true
    }
  ]
}
```

### 4. Outputs / actions
When matched:
- `keywords.add`: adds static keywords `avcs`, `enc`, `data`, `tpm`, `s57`, `aio` plus the dynamic keyword from `properties["frequency"]` (if present)
- `authority.add`: adds `ukho`
- `format.add`: adds the value at `properties["media type"]` (if present)
- `category.add`: adds `exchange set`
- `series.add`: adds `avcs` and `base`
- `instance.add`: adds the value at `properties["id"]` (if present)
- `majorVersion.add`: adds `toInt($path:properties["year"])` (if present and parseable)
- `minorVersion.add`: adds `toInt($path:properties["week number"])` (if present and parseable)
- `searchText.add`: adds `bess output`

Missing data behavior:
- If `properties["frequency"]`, `properties["media type"]`, `properties["id"]`, `properties["year"]`, or `properties["week number"]` are missing/unparseable, only the affected outputs are skipped.

### 5. Examples
Example input payload fragment:
```json
{
  "properties": {
    "businessunitname": "avcs-bespokeexchangesets",
    "exchange set type": "BESS",
    "frequency": "monthly",
    "media type": "zip",
    "id": "bess-2026-11",
    "year": "2026",
    "week number": "11"
  }
}
```

Expected outputs (canonical fields affected):
- `keywords`: `avcs`, `enc`, `data`, `tpm`, `s57`, `aio`, `monthly`
- `authority`: `ukho`
- `format`: `zip`
- `category`: `exchange set`
- `series`: `avcs`, `base`
- `instance`: `bess-2026-11`
- `majorVersion`: `2026`
- `minorVersion`: `11`
- `searchText`: includes `bess output`

### 6. Final rule JSON
Recommended file path:
- `src/Hosts/IngestionServiceHost/Rules/file-share/avcs-bespoke-bess.json`

Final JSON document:
```json
{
  "schemaVersion": "1.0",
  "rule": {
    "id": "avcs-bespoke-bess",
    "description": "AVCS bespoke BESS exchange set output.",
    "if": {
      "all": [
        {
          "path": "properties[\"businessunitname\"]",
          "equals": "avcs-bespokeexchangesets"
        },
        {
          "path": "properties[\"exchange set type\"]",
          "exists": true
        }
      ]
    },
    "then": {
      "keywords": {
        "add": [
          "avcs",
          "enc",
          "data",
          "tpm",
          "s57",
          "aio",
          "$path:properties[\"frequency\"]"
        ]
      },
      "authority": {
        "add": [
          "ukho"
        ]
      },
      "format": {
        "add": [
          "$path:properties[\"media type\"]"
        ]
      },
      "category": {
        "add": [
          "exchange set"
        ]
      },
      "series": {
        "add": [
          "avcs",
          "base"
        ]
      },
      "instance": {
        "add": [
          "$path:properties[\"id\"]"
        ]
      },
      "majorVersion": {
        "add": [
          "toInt($path:properties[\"year\"])"
        ]
      },
      "minorVersion": {
        "add": [
          "toInt($path:properties[\"week number\"])"
        ]
      },
      "searchText": {
        "add": [
          "bess output"
        ]
      }
    }
  }
}
```
