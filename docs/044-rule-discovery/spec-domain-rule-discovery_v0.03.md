# Ingestion rule mapping proposal

Version: `v0.03`
Work Package: `044-rule-discovery`
Supersedes: `spec-domain-rule-discovery_v0.02.md`
Source discovery document: `docs/044-rule-discovery/spec-domain-rule-discovery_v0.01.md`

## Change Log

- `v0.03`
  - records domain confirmation that `printtoorder` rules should be included
  - records domain confirmation that support/readme/startup-pack classes should be indexed
  - records domain confirmation that fixed `s-xxx` values should be indexed in both hyphenated and non-hyphenated forms
  - records domain confirmation that `adsd-viewerupdates` should be searchable for api consumers
  - records domain confirmation that `aenp` monitoring items should be searchable
  - removes the resolved `printtoorder`, support/readme/startup-pack, fixed `s-xxx`, and `adsd-viewerupdates` questions from the open questions list
- `v0.02`
  - adds per-business-unit and per-class mapping proposals
  - adds draft discriminator logic for later rule authoring
  - adds canonical field mapping guidance, search text proposals, and open questions
- `v0.01`
  - discovery-only analysis of batch attribute signatures

## 1. Overview

This document extends the discovery evidence from `docs/044-rule-discovery/spec-domain-rule-discovery_v0.01.md` with draft mapping proposals for later ingestion rule authoring.

This is still a specification and analysis document. It does not define final rule json, does not modify source code, and does not implement rules.

Selected discovery document version/path: `docs/044-rule-discovery/spec-domain-rule-discovery_v0.01.md`

Scope:

- propose how each discovered batch class can be recognised
- propose how each class can map into the canonical search document shape
- identify classes that likely should not produce searchable rules yet
- capture follow-up questions and risks for later rule authoring

Non-goals:

- no final rule json output
- no code changes
- no schema changes
- no upstream data normalization changes

## 2. Global assumptions and normalization rules

- discriminator paths, fixed outputs, keywords, category values, series values, and search text templates are written in lowercase
- sample tables preserve the original discovery values verbatim
- `keywords` must include a copy of every batch attribute value
- where the source value is string-based but conceptually numeric, later rule authoring should use `toInt(...)`
- where dotted versions such as `1.2` or mixed text versions such as `4.2 Trials` appear, no integer mapping is proposed unless a safe normalization step is agreed later
- where only weak support metadata exists, the recommendation may be `no searchable rule proposed` or `defer pending domain confirmation`
- later rule authoring can convert these proposals into rule json once discriminator safety is agreed

## 3. Business unit mapping proposals

### Business Unit: `ADDS` (`Id: 1`)

- Business unit description: private enc s63 data to make s63 exchange set
- Discovery summary: `108,743` batches, `7` classes, coverage `108,743 / 108,743`
- Domain confirmation: support/readme/startup-pack classes in this business unit should be indexed where rule metadata is available

#### Class `1`

- Representative batch id: `38813F7B-07D1-4317-AEBE-000053A25DF0`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `ProductCode`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | FR |
| CellName | FR471460 |
| EditionNumber | 2 |
| ProductCode | AVCS |
| TraceId | 1d0e6e3b-e5e7-4b95-b7e6-d4ef1c5a245a |
| UpdateNumber | 7 |

##### Proposed discriminator

Rationale: this is the main `adds` s63 cell shape with `agency` and `traceid`, but without `source`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"agency\"]", "exists": true },
    { "path": "properties[\"traceid\"]", "exists": true },
    { "path": "properties[\"source\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `s63`, `enc`, `cell` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | business unit intent |
| series | `s63` | adds domain intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s63 enc cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | concise semantic summary |

##### Proposed search text

`s63 enc cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- `productcode` is consistently `avcs`; it is useful as a keyword but not the primary series discriminator

#### Class `2`

- Representative batch id: `F7FE179D-6EAA-443C-80AD-0016CE394FAD`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `ProductCode`, `Source`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | BR |
| CellName | BR41406A |
| EditionNumber | 12 |
| ProductCode | AVCS |
| Source | Penrose-E2E |
| TraceId | 7cae5280-1672-44e9-c033-08dd8cad9fdc |
| UpdateNumber | 0 |

##### Proposed discriminator

Rationale: same core s63 cell shape as class `1`, but `source` exists and safely distinguishes it.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"agency\"]", "exists": true },
    { "path": "properties[\"source\"]", "exists": true },
    { "path": "properties[\"traceid\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `s63`, `enc`, `cell` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | business unit intent |
| series | `s63` | adds domain intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s63 enc cell $path:properties["cellname"] from $path:properties["agency"] source $path:properties["source"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | adds source context |

##### Proposed search text

`s63 enc cell $path:properties["cellname"] from $path:properties["agency"] source $path:properties["source"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- later rule authors may choose not to surface `source` in search text if it creates noise

#### Class `3`

- Representative batch id: `4A64E8FB-C46B-441E-91F7-0031ABC9FB62`
- Sorted attribute keys: `CellName`, `EditionNumber`, `ProductCode`, `Source`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| CellName | NO4E2729 |
| EditionNumber | 5 |
| ProductCode | AVCS |
| Source | ADDS-load-test |
| TraceId | 2c65a075-b54a-4a96-a7d9-8de3ede4be80 |
| UpdateNumber | 4 |

##### Proposed discriminator

Rationale: this class lacks `agency`, but keeps `source` and `traceid`; within `adds` that makes it distinct.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"agency\"]", "exists": false },
    { "path": "properties[\"source\"]", "exists": true },
    { "path": "properties[\"traceid\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `s63`, `enc`, `cell` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no safe region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | business unit intent |
| series | `s63` | adds domain intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s63 enc cell $path:properties["cellname"] source $path:properties["source"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | no authority available |

##### Proposed search text

`s63 enc cell $path:properties["cellname"] source $path:properties["source"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- defaulting `authority` to `ukho` is a fallback only

#### Class `4`

- Representative batch id: `F1BE8ABC-D56E-46AF-90FE-0004F5D27D20`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `ProductCode`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | FR |
| CellName | FR57376B |
| EditionNumber | 2 |
| ProductCode | AVCS |
| UpdateNumber | 1 |

##### Proposed discriminator

Rationale: this is the lean s63 cell shape with `agency` but without `source` and without `traceid`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"agency\"]", "exists": true },
    { "path": "properties[\"source\"]", "exists": false },
    { "path": "properties[\"traceid\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `s63`, `enc`, `cell` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | business unit intent |
| series | `s63` | adds domain intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s63 enc cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | concise semantic summary |

##### Proposed search text

`s63 enc cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- low metadata completeness compared with classes `1` and `2`

#### Class `5`

- Representative batch id: `32A42D7D-2C83-4CF0-AC38-3F76F6A386D6`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | DVD INFO |
| Product Type | AVCS |

##### Proposed discriminator

Rationale: support-style metadata with `content` and `product type` only.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"content\"]", "exists": true },
    { "path": "properties[\"product type\"]", "eq": "avcs" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `dvd`, `info` | limited recall support |
| authority | `ukho` | no authority field |
| region | not mapped | no region evidence |
| format | `dvd` | explicit media clue in content |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | appears to be support metadata rather than a data cell |
| series | `avcs` | explicit product family |
| instance | `$path:properties["content"]` | only identity-bearing field |
| searchText | `adds avcs dvd info support item` | limited semantic value |

##### Proposed search text

`adds avcs dvd info support item`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed

#### Class `6`

- Representative batch id: `FBC3B496-30FB-45DA-8BD4-6D5C1C5B719B`
- Sorted attribute keys: `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | AVCS |

##### Proposed discriminator

Rationale: only `product type` exists; this is distinguishable but not richly searchable.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"product type\"]", "eq": "avcs" },
    { "path": "properties[\"content\"]", "exists": false },
    { "path": "properties[\"cellname\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values | minimum possible mapping |
| authority | not mapped | no safe authority |
| region | not mapped | no evidence |
| format | not mapped | no evidence |
| majorVersion | not mapped | no evidence |
| minorVersion | not mapped | no evidence |
| category | not mapped | too weak to classify safely |
| series | `avcs` | only explicit family signal |
| instance | not mapped | no identity-bearing field |
| searchText | not mapped | not enough safe semantic material |

##### Proposed search text

`not mapped`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed
- this class still requires a minimal rule because metadata is extremely sparse

#### Class `7`

- Representative batch id: `04B78CE7-C288-400C-BA80-73B765EADF8F`
- Sorted attribute keys: `Catalogue Type`, `Content`, `Frequency`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Catalogue Type | ADC |
| Content | Catalogue |
| Frequency | Weekly |
| Product Type | AVCS |
| Week Number | 38 |
| Year | 2025 |
| Year / Week | 2025 / 38 |

##### Proposed discriminator

Rationale: weekly catalogue package shape with `catalogue type` and `year / week`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds" },
    { "path": "properties[\"catalogue type\"]", "exists": true },
    { "path": "properties[\"year / week\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds`, `avcs`, `catalogue`, `weekly` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `catalogue` | content is catalogue-like |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `catalogue` | explicit content |
| series | `avcs` | product family |
| instance | `$path:properties["year / week"]` | identity-bearing schedule |
| searchText | `weekly avcs catalogue for $path:properties["year / week"]` | semantic summary |

##### Proposed search text

`weekly avcs catalogue for $path:properties["year / week"]`

##### Notes / risks / questions

- `catalogue type` value `adc` is useful as a keyword rather than a canonical field

### Business Unit: `ADDS-S57` (`Id: 2`)

- Business unit description: private enc s57 data to make unencrypted exchange sets used for bess
- Discovery summary: `84,731` batches, `6` classes, coverage `84,731 / 84,731`
- Domain confirmation: support/readme/startup-pack classes in this business unit should be indexed where rule metadata is available

#### Class `1`

- Representative batch id: `FD8197D1-A269-42F0-8A46-000000444A9E`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `ProductCode`, `Source`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | GB |
| CellName | GB302508 |
| EditionNumber | 8 |
| ProductCode | AVCS |
| Source | Penrose-BackFill |
| TraceId | 264f9e75-12c8-43c3-beac-08dd767f1884 |
| UpdateNumber | 0 |

##### Proposed discriminator

Rationale: core `adds-s57` cell shape with `agency`, `source`, and `traceid`, but without `original source`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"agency\"]", "exists": true },
    { "path": "properties[\"source\"]", "exists": true },
    { "path": "properties[\"original source\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds-s57`, `avcs`, `s57`, `cell` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | still chart-cell style content |
| series | `s57` | business unit intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s57 cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | semantic summary |

##### Proposed search text

`s57 cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- although the business unit produces unencrypted exchange sets, the individual batch metadata looks like cell-level content

#### Class `2`

- Representative batch id: `A1347B4D-5E2B-441A-A2D0-000EB9C5C2CB`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `Original Source`, `ProductCode`, `Source`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | DK |
| CellName | DK40933E |
| EditionNumber | 2 |
| Original Source | Penrose-E2E |
| ProductCode | AVCS |
| Source | Replace S-57 CRC Backfill |
| TraceId | 5c324412-a353-45ae-c140-08dd8cad9fdc |
| UpdateNumber | 1 |

##### Proposed discriminator

Rationale: `original source` safely separates this replacement/backfill shape from class `1`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"original source\"]", "exists": true },
    { "path": "properties[\"s57-crc\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds-s57`, `avcs`, `s57`, `replacement`, `backfill` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | still chart-cell style content |
| series | `s57` | business unit intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s57 replacement cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | semantic summary |

##### Proposed search text

`s57 replacement cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- `original source` should remain a keyword even if not surfaced as a canonical field

#### Class `3`

- Representative batch id: `36409275-7175-418A-BC00-016951CF32F6`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `Original Source`, `ProductCode`, `s57-CRC`, `Source`, `TraceId`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | CA |
| CellName | CA473414 |
| EditionNumber | 0 |
| Original Source | Penrose-E2E |
| ProductCode | AVCS |
| s57-CRC | F0000B4D |
| Source | Replace S-57 CRC Backfill |
| TraceId | 7b838097-2824-4a76-5a20-08ddc8454483 |
| UpdateNumber | 1 |

##### Proposed discriminator

Rationale: `s57-crc` exists only in this class, so it is a safe discriminator.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"s57-crc\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds-s57`, `avcs`, `s57`, `crc`, `replacement` | improves recall |
| authority | `$path:properties["agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media signal |
| majorVersion | `toInt($path:properties["editionnumber"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["updatenumber"])` | update number is version-like |
| category | `enc` | still chart-cell style content |
| series | `s57` | business unit intent |
| instance | `$path:properties["cellname"]` | cell identity |
| searchText | `s57 crc replacement cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]` | semantic summary |

##### Proposed search text

`s57 crc replacement cell $path:properties["cellname"] from $path:properties["agency"] edition $path:properties["editionnumber"] update $path:properties["updatenumber"]`

##### Notes / risks / questions

- `s57-crc` should likely remain keyword-only rather than canonicalized further

#### Class `4`

- Representative batch id: `87CE7C1C-39CD-4B03-A623-F148746D9CF5`
- Sorted attribute keys: `Catalogue Type`, `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Catalogue Type | ADC |
| Content | Catalogue |
| Product Type | AVCS |

##### Proposed discriminator

Rationale: `catalogue type` makes this a clear catalogue support shape.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"catalogue type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adds-s57`, `avcs`, `catalogue` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `catalogue` | content is catalogue-like |
| majorVersion | not mapped | no year or edition field |
| minorVersion | not mapped | no week or update field |
| category | `catalogue` | explicit content |
| series | `avcs` | product family |
| instance | `$path:properties["catalogue type"]` | only useful identity-bearing field |
| searchText | `avcs catalogue support item` | limited but safe |

##### Proposed search text

`avcs catalogue support item`

##### Notes / risks / questions

- no date/version field means this remains weakly searchable

#### Class `5`

- Representative batch id: `67B69A84-9089-4546-9CA6-9A7268AED419`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | DVD INFO |
| Product Type | AVCS |

##### Proposed discriminator

Rationale: support-style metadata with `content` only.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"content\"]", "eq": "dvd info" },
    { "path": "properties[\"dvd info\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `dvd`, `info`, `avcs` | limited recall support |
| authority | `ukho` | no authority field |
| region | not mapped | no evidence |
| format | `dvd` | explicit media clue in content |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | appears to be support metadata |
| series | `avcs` | explicit product family |
| instance | `$path:properties["content"]` | only identity-bearing field |
| searchText | `adds-s57 avcs dvd info support item` | limited semantic value |

##### Proposed search text

`adds-s57 avcs dvd info support item`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed

#### Class `6`

- Representative batch id: `CA6D5B9F-B509-4DD5-A6A6-4CAFD70750DF`
- Sorted attribute keys: `DVD INFO`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| DVD INFO | AVCS |
| Product Type | AVCS |

##### Proposed discriminator

Rationale: unusual key name `dvd info` safely distinguishes this class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s57" },
    { "path": "properties[\"dvd info\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `dvd`, `info`, `avcs` | limited recall support |
| authority | not mapped | no authority field |
| region | not mapped | no evidence |
| format | `dvd` | key name implies media format |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | appears to be support metadata |
| series | `avcs` | explicit product family |
| instance | `$path:properties["dvd info"]` | only identity-bearing value |
| searchText | `adds-s57 avcs dvd info support item` | limited semantic value |

##### Proposed search text

`adds-s57 avcs dvd info support item`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed
- this class should still use a minimal rule because metadata is sparse

### Business Unit: `AVCSCustomExchangeSets` (`Id: 3`)

- Business unit description: exchange sets made by customers using the ess api and ess ui (s57, s63 and s100). customers only have access to their own exchange sets, retrieved using batch id or url
- Discovery summary: `2` batches, `1` class, coverage `2 / 2`

#### Class `1`

- Representative batch id: `CD1E5514-688F-4054-9644-76E8A94C7276`
- Sorted attribute keys: `Exchange Set Type`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Update |
| Media Type | Zip |
| Product Type | AVCS |

##### Proposed discriminator

Rationale: only class in this business unit; `exchange set type` confirms exchange-set metadata.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcscustomexchangesets" },
    { "path": "properties[\"exchange set type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `custom`, `exchange set`, `customer`, `avcs` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `exchange set` | explicit domain intent |
| series | `avcs` | product family |
| instance | `$path:properties["exchange set type"]` | only identity-bearing field |
| searchText | `custom avcs exchange set $path:properties["exchange set type"] in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`custom avcs exchange set $path:properties["exchange set type"] in $path:properties["media type"] format`

##### Notes / risks / questions

- customer ownership is important operationally but not represented in the sampled metadata

### Business Unit: `AVCSData` (`Id: 4`)

- Business unit description: weekly data sets uploaded by tpms and collected by distributers
- Discovery summary: `14` batches, `5` classes, coverage `14 / 14`

#### Class `1`

- Representative batch id: `656D2B8C-A7FC-4230-A46E-002E346E34C4`
- Sorted attribute keys: `Exchange Set Type`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | AIO |
| Product Type | AIO |
| Week Number | 10 |
| Year | 2026 |
| Year / Week | 2026 / 10 |

##### Proposed discriminator

Rationale: `product type = aio` and `exchange set type` present make this the aio weekly set.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcsdata" },
    { "path": "properties[\"product type\"]", "eq": "aio" },
    { "path": "properties[\"exchange set type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aio`, `weekly`, `dataset` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | not mapped | no media field |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `exchange set` | weekly delivered set |
| series | `avcs` | avcs data family |
| instance | `$path:properties["year / week"]` | schedule identity |
| searchText | `weekly aio data set for $path:properties["year / week"]` | semantic summary |

##### Proposed search text

`weekly aio data set for $path:properties["year / week"]`

##### Notes / risks / questions

- `aio` should remain prominent in keywords even though series stays at family level `avcs`

#### Class `2`

- Representative batch id: `1AEF90F8-3433-4605-ADF9-408F9608144F`
- Sorted attribute keys: `Catalogue Type`, `Content`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Catalogue Type | Text |
| Content | Catalogue |
| Product Type | AVCS |
| Week Number | 06 |
| Year | 2026 |
| Year / Week | 2026 / 06 |

##### Proposed discriminator

Rationale: `catalogue type` distinguishes the weekly catalogue class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcsdata" },
    { "path": "properties[\"catalogue type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `avcs`, `catalogue`, `weekly` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `catalogue` | explicit content |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `catalogue` | explicit content |
| series | `avcs` | product family |
| instance | `$path:properties["year / week"]` | schedule identity |
| searchText | `weekly avcs catalogue for $path:properties["year / week"]` | semantic summary |

##### Proposed search text

`weekly avcs catalogue for $path:properties["year / week"]`

##### Notes / risks / questions

- `catalogue type` value `text` is useful as a keyword only

#### Class `3`

- Representative batch id: `37B289EB-43ED-4A9F-B5FF-0A26FB4F77BD`
- Sorted attribute keys: `Exchange Set Type`, `Media Type`, `Product Type`, `S63 Version`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Update |
| Media Type | Zip |
| Product Type | AVCS |
| S63 Version | 1.2 |
| Week Number | 06 |
| Year | 2026 |
| Year / Week | 2026 / 06 |

##### Proposed discriminator

Rationale: `s63 version` safely distinguishes this packaged avcs update set from class `1`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcsdata" },
    { "path": "properties[\"s63 version\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `avcs`, `s63`, `update`, `weekly` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `exchange set` | delivered package |
| series | `s63` | explicit s63 signal |
| instance | `$path:properties["year / week"]` | schedule identity |
| searchText | `weekly s63 avcs update exchange set for $path:properties["year / week"] in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`weekly s63 avcs update exchange set for $path:properties["year / week"] in $path:properties["media type"] format`

##### Notes / risks / questions

- `s63 version` is best retained as keywords until dotted-version handling is standardized

#### Class `4`

- Representative batch id: `47D46FC7-F795-4230-8698-52043124302C`
- Sorted attribute keys: `Content`, `Product Type`, `Service`

| AttributeKey | AttributeValue |
|---|---|
| Content | Certificate |
| Product Type | AVCS |
| Service | AVCS OUS |

##### Proposed discriminator

Rationale: `service` plus `content = certificate` makes this a service certificate class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcsdata" },
    { "path": "properties[\"service\"]", "exists": true },
    { "path": "properties[\"content\"]", "eq": "certificate" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `certificate`, `service`, `avcs` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | not mapped | no media format field |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `certificate` | explicit content |
| series | `avcs` | product family |
| instance | `$path:properties["service"]` | service identity |
| searchText | `avcs certificate for $path:properties["service"]` | semantic summary |

##### Proposed search text

`avcs certificate for $path:properties["service"]`

##### Notes / risks / questions

- this may be operationally useful even though versioning is absent

#### Class `5`

- Representative batch id: `254E56B0-4246-48EF-9D6C-270AB5A9153D`
- Sorted attribute keys: `Content`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Content | Catalogue |
| Product Type | AVCS |
| Year | 2023 |

##### Proposed discriminator

Rationale: catalogue content without week data forms a distinct annual/simple catalogue class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcsdata" },
    { "path": "properties[\"content\"]", "eq": "catalogue" },
    { "path": "properties[\"week number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `avcs`, `catalogue` | improves recall |
| authority | `ukho` | no agency field |
| region | not mapped | no region evidence |
| format | `catalogue` | explicit content |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | not mapped | no week field |
| category | `catalogue` | explicit content |
| series | `avcs` | product family |
| instance | `$path:properties["year"]` | only identity-bearing period |
| searchText | `avcs catalogue for $path:properties["year"]` | semantic summary |

##### Proposed search text

`avcs catalogue for $path:properties["year"]`

##### Notes / risks / questions

- if this represents an archival catalogue, later rule authors may want a stronger archival keyword set

### Business Unit: `MaritimeSafetyInformation` (`Id: 5`)

- Business unit description: notice to mariners displayed on msi website and also available via fss ui/api
- Discovery summary: `216` batches, `3` classes, coverage `216 / 216`

#### Class `1`

- Representative batch id: `BCEE859A-84E7-4D72-85EF-02119167B285`
- Sorted attribute keys: `Content`, `Data Date`, `Frequency`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Content | tracings |
| Data Date | 2023-06-08 |
| Frequency | Weekly |
| Product Type | Notices to Mariners |
| Week Number | 23 |
| Year | 2023 |
| Year / Week | 2023 / 23 |

##### Proposed discriminator

Rationale: `content` exists and identifies a weekly tracings sub-class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "maritimesafetyinformation" },
    { "path": "properties[\"content\"]", "exists": true },
    { "path": "properties[\"frequency\"]", "eq": "weekly" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `msi`, `notice to mariners`, `weekly`, `tracings` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `$path:properties["content"]` | content behaves like format subtype |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `notice to mariners` | explicit product type |
| series | `msi` | stable family label |
| instance | `$path:properties["year / week"]` | schedule identity |
| searchText | `weekly notice to mariners tracings for $path:properties["year / week"] dated $path:properties["data date"]` | semantic summary |

##### Proposed search text

`weekly notice to mariners tracings for $path:properties["year / week"] dated $path:properties["data date"]`

##### Notes / risks / questions

- `content` could also be left keyword-only if a stricter interpretation of `format` is preferred

#### Class `2`

- Representative batch id: `44DA7BAA-1665-4F2B-9B6A-00A9F8E40B98`
- Sorted attribute keys: `Data Date`, `Frequency`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Data Date | 2025-01-16 |
| Frequency | Weekly |
| Product Type | Notices to Mariners |
| Week Number | 3 |
| Year | 2025 |
| Year / Week | 2025 / 3 |

##### Proposed discriminator

Rationale: weekly notice-to-mariners shape without `content`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "maritimesafetyinformation" },
    { "path": "properties[\"frequency\"]", "eq": "weekly" },
    { "path": "properties[\"content\"]", "exists": false },
    { "path": "properties[\"week number\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `msi`, `notice to mariners`, `weekly` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no explicit format field |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `notice to mariners` | explicit product type |
| series | `msi` | stable family label |
| instance | `$path:properties["year / week"]` | schedule identity |
| searchText | `weekly notice to mariners for $path:properties["year / week"] dated $path:properties["data date"]` | semantic summary |

##### Proposed search text

`weekly notice to mariners for $path:properties["year / week"] dated $path:properties["data date"]`

##### Notes / risks / questions

- this appears to be the main searchable ntm weekly class

#### Class `3`

- Representative batch id: `C1727824-8D5F-4420-9D74-0B3FC3946AC0`
- Sorted attribute keys: `Data Date`, `Frequency`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Data Date | 2024-02-29 |
| Frequency | Cumulative |
| Product Type | Notices to Mariners |
| Year | 2024 |

##### Proposed discriminator

Rationale: `frequency = cumulative` safely distinguishes the cumulative annual class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "maritimesafetyinformation" },
    { "path": "properties[\"frequency\"]", "eq": "cumulative" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `msi`, `notice to mariners`, `cumulative` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no explicit format field |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | not mapped | no week field |
| category | `notice to mariners` | explicit product type |
| series | `msi` | stable family label |
| instance | `$path:properties["year"]` | annual identity |
| searchText | `cumulative notice to mariners for $path:properties["year"] dated $path:properties["data date"]` | semantic summary |

##### Proposed search text

`cumulative notice to mariners for $path:properties["year"] dated $path:properties["data date"]`

##### Notes / risks / questions

- annual cumulative outputs may merit distinct ui treatment later

### Business Unit: `BritishLegalDepositLibraryPublications` (`Id: 6`)

- Business unit description: nothing searchable
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `no searchable rule proposed`

### Business Unit: `PrintToOrder` (`Id: 7`)

- Business unit description: nothing searchable, bu is for print to order team
- Discovery summary: `4,369` batches, `11` classes, coverage `4,369 / 4,369`
- Domain confirmation: include `printtoorder` rules

#### Class `1`

- Representative batch id: `41698497-5397-4391-AC8B-003EFDB75A9A`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `Nm Number`, `Paper Depth`, `Paper Width`, `Plan Type`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | 2896 |
| Chart Title | Mina' Salalah and Approaches |
| Chart Version Id | 125372 |
| Edition Date | 2022-04-07 |
| Edition Number | 7 |
| Nm Number | 3333/23 |
| Paper Depth | 1068 |
| Paper Width | 715 |
| Plan Type | NM |
| Product Type | Paper Chart |
| Publication Date | 1983-04-01 |

##### Proposed discriminator

Rationale: `nm number`, `plan type`, and `chart version id` exist, with no `int number`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"nm number\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": true },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `print`, `nm` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | `nm number` is not a safe integer |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- business unit description says not searchable; if that remains true, later rule authoring should suppress this entire business unit despite viable metadata

#### Class `2`

- Representative batch id: `721F2AC6-ACF4-46C7-B03E-00D8A87732C9`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `INT Number`, `Nm Number`, `Paper Depth`, `Paper Width`, `Plan Type`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | 0116 |
| Chart Title | Approaches to Westerschelde |
| Chart Version Id | 149410 |
| Edition Date | 2022-08-04 |
| Edition Number | 3 |
| INT Number | 1477 |
| Nm Number | 1137/25 |
| Paper Depth | 1189 |
| Paper Width | 841 |
| Plan Type | NM |
| Product Type | Paper Chart |
| Publication Date | 2017-01-19 |

##### Proposed discriminator

Rationale: this is the class `1` shape plus `int number`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"nm number\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": true },
    { "path": "properties[\"int number\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `int`, `nm` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | `nm number` is not a safe integer |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- as above, business-unit-level domain confirmation is still needed before indexing

#### Class `3`

- Representative batch id: `D74D3EEC-4EF1-4C86-9A4B-0067B061D627`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `Paper Depth`, `Paper Width`, `Plan Type`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | NOV5126 |
| Chart Title | Routeing Chart Indian Ocean November |
| Chart Version Id | 118368 |
| Edition Date | 2022-03-31 |
| Edition Number | 5 |
| Paper Depth | 1068 |
| Paper Width | 715 |
| Plan Type | NE |
| Product Type | Paper Chart |
| Publication Date | 2004-07-08 |

##### Proposed discriminator

Rationale: `chart version id` and `plan type` exist, but both `nm number` and `int number` are absent.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": true },
    { "path": "properties[\"nm number\"]", "exists": false },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `routeing` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe integer minor version |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"] plan $path:properties["plan type"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"] plan $path:properties["plan type"]`

##### Notes / risks / questions

- `plan type` is useful as a keyword and optional search-text token

#### Class `4`

- Representative batch id: `6A4BACB9-FA06-4B51-A84B-01F400444D14`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Edition Date`, `Edition Number`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | F2649 |
| Chart Title | British Isles; Western Approaches to the English Channel |
| Edition Date | 2020-02-27 |
| Edition Number | 5 |
| Product Type | Paper Chart |
| Publication Date | 1999-08-12 |

##### Proposed discriminator

Rationale: lean paper-chart shape with no `chart version id` and no paper dimensions.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": false },
    { "path": "properties[\"edition date\"]", "exists": true },
    { "path": "properties[\"paper depth\"]", "exists": false },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- same business-unit-level searchability caveat applies

#### Class `5`

- Representative batch id: `D59C5655-ECBF-4103-844C-009DF56929FA`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `INT Number`, `Paper Depth`, `Paper Width`, `Plan Type`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | 4906 |
| Chart Title | Weddell Sea |
| Chart Version Id | 98958 |
| Edition Date | 2020-07-16 |
| Edition Number | 3 |
| INT Number | 906 |
| Paper Depth | 1189 |
| Paper Width | 841 |
| Plan Type | NE |
| Product Type | Paper Chart |
| Publication Date | 2005-10-27 |

##### Proposed discriminator

Rationale: `chart version id`, `int number`, and `plan type` exist, but `nm number` does not.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"int number\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": true },
    { "path": "properties[\"nm number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `int` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe integer minor version |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- no additional notes

#### Class `6`

- Representative batch id: `CC4CCF76-CD1B-4344-BDA5-040C2013BDE9`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `Paper Depth`, `Paper Width`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | FCS2267B |
| Chart Title | Op Mayflower Scenario and South Coast Exercise Areas B |
| Chart Version Id | 142444 |
| Edition Date | 2024-07-18 |
| Edition Number | 1 |
| Paper Depth | 680 |
| Paper Width | 1050 |
| Product Type | Paper Chart |
| Publication Date | 2024-07-18 |

##### Proposed discriminator

Rationale: `chart version id` exists, but `plan type`, `nm number`, and `int number` are absent.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": false },
    { "path": "properties[\"nm number\"]", "exists": false },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- no additional notes

#### Class `7`

- Representative batch id: `5152F58A-CA67-40F7-8E7A-022912DEA4E2`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Edition Number`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | FCS2266A |
| Chart Title | Joint Warrior 24-1 / Nordic Response 24 - Northern Overview |
| Edition Number | 1 |
| Product Type | Paper Chart |
| Publication Date | 2024-02-07 |

##### Proposed discriminator

Rationale: minimal paper-chart shape with no `chart version id`, no `edition date`, and no dimensions.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": false },
    { "path": "properties[\"edition date\"]", "exists": false },
    { "path": "properties[\"paper depth\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- no additional notes

#### Class `8`

- Representative batch id: `ABBE5329-D7EB-49BF-9780-03DC2633DB9E`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Edition Date`, `Edition Number`, `Paper Depth`, `Paper Width`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | F2858 |
| Chart Title | Gulf Operations Chart |
| Edition Date | 2024-09-12 |
| Edition Number | 13 |
| Paper Depth | 720 |
| Paper Width | 1030 |
| Product Type | Paper Chart |
| Publication Date | 1990-01-19 |

##### Proposed discriminator

Rationale: no `chart version id`, but `edition date` and paper dimensions exist.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": false },
    { "path": "properties[\"edition date\"]", "exists": true },
    { "path": "properties[\"paper depth\"]", "exists": true },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- no additional notes

#### Class `9`

- Representative batch id: `CF03BD30-B1B0-4ED0-AA03-1304416FEEC2`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `EditionNumber`, `Paper Depth`, `Paper Width`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | FCS2004 |
| Chart Title | Towed Array Patrol Ship Planning Chart |
| Edition Number | 1 |
| Paper Depth | 1189 |
| Paper Width | 841 |
| Product Type | Paper Chart |
| Publication Date | 30/11/2010 |

##### Proposed discriminator

Rationale: no `chart version id`, no `edition date`, but paper dimensions exist.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": false },
    { "path": "properties[\"edition date\"]", "exists": false },
    { "path": "properties[\"paper depth\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- key naming variation `editionnumber` vs `edition number` will need normalization during rule authoring

#### Class `10`

- Representative batch id: `90BFDDA6-A75E-4D25-A959-10CDE9064801`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Edition Date`, `Edition Number`, `INT Number`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | IN472 |
| Chart Title | Nicobar Islands |
| Edition Date | 2019-07-31 |
| Edition Number | 2 |
| INT Number | 7032 |
| Product Type | Paper Chart |
| Publication Date | 2020-10-08 |

##### Proposed discriminator

Rationale: no `chart version id`, `edition date` exists, `int number` exists, and no dimensions exist.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": false },
    { "path": "properties[\"edition date\"]", "exists": true },
    { "path": "properties[\"int number\"]", "exists": true },
    { "path": "properties[\"paper depth\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `int` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | no safe minor version field |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] int $path:properties["int number"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- no additional notes

#### Class `11`

- Representative batch id: `2E6746BA-B5C1-43FA-8F86-A0F9514FDB5F`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Chart Version Id`, `Edition Date`, `Edition Number`, `Nm Number`, `Paper Depth`, `Paper Width`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | TO0502 |
| Chart Title | Approaches to Nuku alofa Harbour |
| Chart Version Id | 134259 |
| Edition Date | 2022-03-31 |
| Edition Number | 1 |
| Nm Number | 3134/23 |
| Paper Depth | 1068 |
| Paper Width | 715 |
| Product Type | Paper Chart |
| Publication Date | 2022-03-31 |

##### Proposed discriminator

Rationale: `chart version id` and `nm number` exist, but `plan type` and `int number` do not.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "printtoorder" },
    { "path": "properties[\"chart version id\"]", "exists": true },
    { "path": "properties[\"nm number\"]", "exists": true },
    { "path": "properties[\"plan type\"]", "exists": false },
    { "path": "properties[\"int number\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `paper`, `chart`, `nm` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no safe region field |
| format | `paper` | explicit product type |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | not mapped | `nm number` is not a safe integer |
| category | `paper chart` | explicit product type |
| series | not mapped | no stable series field |
| instance | `$path:properties["chart number"]` | chart identity |
| searchText | `paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]` | semantic summary |

##### Proposed search text

`paper chart $path:properties["chart number"] $path:properties["chart title"] edition $path:properties["edition number"]`

##### Notes / risks / questions

- same business-unit-level searchability caveat applies

### Business Unit: `ADDSSupport` (`Id: 8`)

- Business unit description: random test stuff
- Discovery summary: `1` batches, `1` classes, coverage `1 / 1`

#### Class `1`

- Representative batch id: `A92ECF31-9DCE-4F47-A191-D4E7901C6213`
- Sorted attribute keys: `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | SAR TEST |
| Week Number | 44 |
| Year | 2024 |
| Year / Week | 2024 / 44 |

##### Proposed discriminator

Rationale: only class in a test-only business unit.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "addssupport" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values | minimal mapping only |
| authority | not mapped | no safe authority |
| region | not mapped | no evidence |
| format | not mapped | no evidence |
| majorVersion | `toInt($path:properties["year"])` | available but not enough to justify indexing |
| minorVersion | `toInt($path:properties["week number"])` | available but not enough to justify indexing |
| category | not mapped | test-only business unit |
| series | not mapped | test-only business unit |
| instance | `$path:properties["year / week"]` | only identity-bearing field |
| searchText | not mapped | test-only business unit |

##### Proposed search text

`not mapped`

##### Notes / risks / questions

- no searchable rule proposed because the business unit is explicitly test-oriented

### Business Unit: `ADP` (`Id: 9`)

- Business unit description: weekly data sets uploaded by tpms and collected by distributers
- Discovery summary: `5` batches, `2` classes, coverage `5 / 5`

#### Class `1`

- Representative batch id: `06BDBA9A-454E-4792-A22E-5488267F4CC2`
- Sorted attribute keys: `ADP Version`, `Content`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| ADP Version | 23 |
| Content | ADP Software |
| Media Type | DVD |
| Product Type | ADP |

##### Proposed discriminator

Rationale: `adp version` exists, making this the main adp software class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adp" },
    { "path": "properties[\"adp version\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adp`, `software`, `dvd` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | `toInt($path:properties["adp version"])` | version-like integer |
| minorVersion | not mapped | no minor version field |
| category | `software` | explicit content |
| series | `adp` | product family |
| instance | `$path:properties["product type"]` | only stable identity-bearing field |
| searchText | `adp software version $path:properties["adp version"] on $path:properties["media type"]` | semantic summary |

##### Proposed search text

`adp software version $path:properties["adp version"] on $path:properties["media type"]`

##### Notes / risks / questions

- no additional notes

#### Class `2`

- Representative batch id: `0DCA217D-4453-42B3-AFC6-9AED6C9EB421`
- Sorted attribute keys: `Content`, `NavPac Version`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Software |
| NavPac Version | 4.2 Trials |
| Product Type | HMNAO |

##### Proposed discriminator

Rationale: `navpac version` exists only in this class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adp" },
    { "path": "properties[\"navpac version\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `software`, `navpac`, `trial` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | not mapped | mixed text version is not safely integer-convertible |
| minorVersion | not mapped | mixed text version is not safely integer-convertible |
| category | `software` | explicit content |
| series | `navpac` | strongest family signal |
| instance | `$path:properties["product type"]` | best available identity |
| searchText | `navpac software $path:properties["navpac version"] for $path:properties["product type"]` | semantic summary |

##### Proposed search text

`navpac software $path:properties["navpac version"] for $path:properties["product type"]`

##### Notes / risks / questions

- later rule authors may want dotted version splitting if needed elsewhere

### Business Unit: `AENP` (`Id: 10`)

- Business unit description: weekly data sets uploaded by tpms and collected by distributers
- Discovery summary: `100` batches, `4` classes, coverage `100 / 100`

#### Class `1`

- Representative batch id: `04D0217D-CD10-4C0D-B8F6-0229408F069F`
- Sorted attribute keys: `Edition`, `Product ID`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Edition | 2024 |
| Product ID | e-NP234B |
| Product Type | AENP |
| Year | 2024 |

##### Proposed discriminator

Rationale: `product id` with `edition` and `year` identifies the main publication class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "aenp" },
    { "path": "properties[\"product id\"]", "exists": true },
    { "path": "properties[\"edition\"]", "exists": true },
    { "path": "properties[\"year\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aenp`, `publication` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | `toInt($path:properties["edition"])` | edition behaves as year-like version |
| minorVersion | not mapped | no minor version field |
| category | `publication` | product id suggests publication-like item |
| series | `aenp` | product family |
| instance | `$path:properties["product id"]` | publication identity |
| searchText | `aenp publication $path:properties["product id"] edition $path:properties["edition"] year $path:properties["year"]` | semantic summary |

##### Proposed search text

`aenp publication $path:properties["product id"] edition $path:properties["edition"] year $path:properties["year"]`

##### Notes / risks / questions

- using `edition` rather than `year` for `majorVersion` best fits publication semantics

#### Class `2`

- Representative batch id: `D7C49021-BAD8-42E8-A7E8-A9BA78FFB11B`
- Sorted attribute keys: `AENP Version`, `Content`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| AENP Version | 1.3 |
| Content | AENP Software |
| Media Type | Zip |
| Product Type | AENP |

##### Proposed discriminator

Rationale: `aenp version` plus `media type` identifies the packaged software class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "aenp" },
    { "path": "properties[\"aenp version\"]", "exists": true },
    { "path": "properties[\"media type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aenp`, `software` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | not mapped | dotted version is not safely integer-convertible |
| minorVersion | not mapped | dotted version is not safely integer-convertible |
| category | `software` | explicit content |
| series | `aenp` | product family |
| instance | `$path:properties["product type"]` | best stable identity |
| searchText | `aenp software $path:properties["aenp version"] in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`aenp software $path:properties["aenp version"] in $path:properties["media type"] format`

##### Notes / risks / questions

- dotted version handling could be added later if needed across product families

#### Class `3`

- Representative batch id: `059A8A5A-0E81-4426-8FF3-58C6B7E98383`
- Sorted attribute keys: `AENP Version`, `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| AENP Version | SDK |
| Content | Software |
| Product Type | AENP |

##### Proposed discriminator

Rationale: `aenp version` exists, but `media type` does not; the sample value indicates sdk software.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "aenp" },
    { "path": "properties[\"aenp version\"]", "exists": true },
    { "path": "properties[\"media type\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aenp`, `software`, `sdk` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | not mapped | `sdk` is not numeric |
| minorVersion | not mapped | `sdk` is not numeric |
| category | `software` | explicit content |
| series | `aenp` | product family |
| instance | `$path:properties["aenp version"]` | only identity-bearing field |
| searchText | `aenp sdk software` | semantic summary |

##### Proposed search text

`aenp sdk software`

##### Notes / risks / questions

- later rule authors may choose to normalize `sdk` into keywords only and use `product type` as instance instead

#### Class `4`

- Representative batch id: `3EF9D155-7280-4B03-8503-78B36C802C55`
- Sorted attribute keys: `Product ID`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Product ID | AENP Monitoring |
| Product Type | AENP |

##### Proposed discriminator

Rationale: only `product id` and `product type` exist, making this a lightweight monitoring/support class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "aenp" },
    { "path": "properties[\"product id\"]", "exists": true },
    { "path": "properties[\"edition\"]", "exists": false },
    { "path": "properties[\"aenp version\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aenp`, `monitoring` | limited recall support |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | looks operational rather than publication or software |
| series | `aenp` | product family |
| instance | `$path:properties["product id"]` | only identity-bearing field |
| searchText | `aenp monitoring item` | limited semantic value |

##### Proposed search text

`aenp monitoring item`

##### Notes / risks / questions

- domain confirmation says monitoring items should be searchable

### Business Unit: `ARCS` (`Id: 11`)

- Business unit description: weekly data sets uploaded by tpms and collected by distributers
- Discovery summary: `22` batches, `1` classes, coverage `22 / 22`

#### Class `1`

- Representative batch id: `B8FC73E7-A733-4501-B170-0A723CCE3555`
- Sorted attribute keys: `Disc`, `Media Type`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Disc | RC5 |
| Media Type | CD |
| Product Type | ARCS |
| Week Number | 19 |
| Year | 2024 |
| Year / Week | 2024 / 19 |

##### Proposed discriminator

Rationale: only class in this business unit; `disc` and period fields clearly identify the weekly media set.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "arcs" },
    { "path": "properties[\"disc\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `arcs`, `weekly`, `disc` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `exchange set` | periodic delivered package |
| series | `arcs` | product family |
| instance | `$path:properties["disc"]` | disc identity |
| searchText | `weekly arcs disc $path:properties["disc"] for $path:properties["year / week"]` | semantic summary |

##### Proposed search text

`weekly arcs disc $path:properties["disc"] for $path:properties["year / week"]`

##### Notes / risks / questions

- no additional notes

### Business Unit: `PaperProducts` (`Id: 12`)

- Business unit description: pod files not sure whether these are downloaded from ui think fss might just be for storage and an application might be getting them
- Discovery summary: `2` batches, `2` classes, coverage `2 / 2`

#### Class `1`

- Representative batch id: `035DE0A1-BD0F-4119-9C9D-C6116A42500D`
- Sorted attribute keys: `Content`, `POD Version`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Software |
| POD Version | SPC |
| Product Type | POD |

##### Proposed discriminator

Rationale: `pod version` exists only in this class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "paperproducts" },
    { "path": "properties[\"pod version\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `pod`, `software` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no explicit media field |
| majorVersion | not mapped | `spc` is not numeric |
| minorVersion | not mapped | `spc` is not numeric |
| category | `software` | explicit content |
| series | `pod` | product family |
| instance | `$path:properties["pod version"]` | only identity-bearing field |
| searchText | `pod software $path:properties["pod version"]` | semantic summary |

##### Proposed search text

`pod software $path:properties["pod version"]`

##### Notes / risks / questions

- defer pending domain confirmation if this storage-only business unit should remain non-searchable

#### Class `2`

- Representative batch id: `3EA9C3DB-1FDB-4E19-B9BB-C3C3FC0FE8F2`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Images |
| Product Type | Publications |

##### Proposed discriminator

Rationale: only class in this business unit without `pod version`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "paperproducts" },
    { "path": "properties[\"pod version\"]", "exists": false },
    { "path": "properties[\"content\"]", "eq": "images" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `images`, `publications` | limited recall support |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `images` | strongest media-like clue |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `publication` | closest user-facing type |
| series | not mapped | no stable series field |
| instance | `$path:properties["product type"]` | only identity-bearing field |
| searchText | `publication images item` | weak semantic value |

##### Proposed search text

`publication images item`

##### Notes / risks / questions

- no searchable rule proposed unless domain owners confirm these assets should appear in search

### Business Unit: `Chersoft` (`Id: 13`)

- Business unit description: business continuity
- Discovery summary: `92` batches, `1` classes, coverage `92 / 92`

#### Class `1`

- Representative batch id: `E0709FA6-ACA0-4561-8D2C-02C4592829C2`
- Sorted attribute keys: `Content`, `Edition`, `Product ID`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Content | Annual |
| Edition | 24 |
| Product ID | e-NP314-24 |
| Product Type | AENP |
| Year | 2024 |

##### Proposed discriminator

Rationale: only class in this continuity business unit; `product id` and `content = annual` describe an annual aenp publication.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "chersoft" },
    { "path": "properties[\"product id\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `aenp`, `annual`, `publication` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | `toInt($path:properties["year"])` | full year is safest version field |
| minorVersion | not mapped | no minor version field |
| category | `publication` | publication-like content |
| series | `aenp` | product family |
| instance | `$path:properties["product id"]` | publication identity |
| searchText | `annual aenp publication $path:properties["product id"] for $path:properties["year"]` | semantic summary |

##### Proposed search text

`annual aenp publication $path:properties["product id"] for $path:properties["year"]`

##### Notes / risks / questions

- continuity/business-continuity purpose may justify excluding this business unit from general search later

### Business Unit: `VAR` (`Id: 14`)

- Business unit description: specific bu for file sharing with vars but not really anything
- Discovery summary: `2` batches, `1` classes, coverage `2 / 2`
- Domain confirmation: support/readme/startup-pack classes in this business unit should be indexed

#### Class `1`

- Representative batch id: `CFE2E3D1-877E-432B-91C3-03A850904D67`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | VAR Start-up pack zip |
| Product Type | VAR |

##### Proposed discriminator

Rationale: only class in a low-value sharing business unit.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "var" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `var`, `startup`, `zip` | limited recall support |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | `zip` | explicit content clue |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | startup pack support asset |
| series | `var` | product family |
| instance | `$path:properties["content"]` | only identity-bearing field |
| searchText | `var startup pack zip` | limited semantic value |

##### Proposed search text

`var startup pack zip`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed

### Business Unit: `ADDS-S100` (`Id: 15`)

- Business unit description: private s100 file ingestion to make s100 exchange sets
- Discovery summary: `2,631` batches, `4` classes, coverage `2,631 / 2,631`

#### Class `1`

- Representative batch id: `BF0734FD-EE32-4578-96F8-00044623DEB6`
- Sorted attribute keys: `Edition Number`, `Producing Agency`, `Product Code`, `Product Identifier`, `Product Name`, `Source`, `Trace ID`, `Update Number`

| AttributeKey | AttributeValue |
|---|---|
| Edition Number | 1 |
| Producing Agency | GB00 |
| Product Code | s101 |
| Product Identifier | s101 |
| Product Name | 101GB00PTCF5C1 |
| Source | ADDS-load-test |
| Trace ID | d6cd06f3-19c7-4bfc-97f5-7135a36f7ae5 |
| Update Number | 0 |

##### Proposed discriminator

Rationale: `product code` exists, so this is the primary s100 product-update class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"product code\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `s100`, `product`, `cell` | improves recall |
| authority | `$path:properties["producing agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media field |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["update number"])` | update number is version-like |
| category | `data product` | individual s100 product batch |
| series | `$path:properties["product code"]` | explicit product code such as `s101` |
| instance | `$path:properties["product name"]` | strongest identity-bearing field |
| searchText | `s100 product $path:properties["product name"] from $path:properties["producing agency"] edition $path:properties["edition number"] update $path:properties["update number"]` | semantic summary |

##### Proposed search text

`s100 product $path:properties["product name"] from $path:properties["producing agency"] edition $path:properties["edition number"] update $path:properties["update number"]`

##### Notes / risks / questions

- `product identifier` duplicates the series signal; keep both as keywords

#### Class `2`

- Representative batch id: `6C9C6917-55C0-4A88-8C5F-0238439A1568`
- Sorted attribute keys: `Edition Number`, `Producing Agency`, `Product Identifier`, `Product Name`, `Product Type`, `Source`, `Trace ID`, `Update Number`

| AttributeKey | AttributeValue |
|---|---|
| Edition Number | 2 |
| Producing Agency | GB00 |
| Product Identifier | S-101 |
| Product Name | 101GB006TST10 |
| Product Type | S-100 |
| Source | PenroseS100-E2E |
| Trace ID | 25eaee3f-a9ac-4d49-8d36-08de3e07b2b8 |
| Update Number | 0 |

##### Proposed discriminator

Rationale: `product type` exists while `product code` does not, distinguishing this variant.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"product type\"]", "exists": true },
    { "path": "properties[\"product code\"]", "exists": false },
    { "path": "properties[\"product name\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `s100`, `product` | improves recall |
| authority | `$path:properties["producing agency"]` | explicit producer authority |
| region | not mapped | no separate region field |
| format | not mapped | no media field |
| majorVersion | `toInt($path:properties["edition number"])` | edition number is version-like |
| minorVersion | `toInt($path:properties["update number"])` | update number is version-like |
| category | `data product` | individual s100 product batch |
| series | add both `s-100` and `s100`; also add normalized aliases from `$path:properties["product identifier"]`, giving `s-101` and `s101` for the sampled value | keep both family-level and product-level series aliases |
| instance | `$path:properties["product name"]` | strongest identity-bearing field |
| searchText | `s100 product $path:properties["product name"] from $path:properties["producing agency"] edition $path:properties["edition number"] update $path:properties["update number"]` | semantic summary |

##### Proposed search text

`s100 product $path:properties["product name"] from $path:properties["producing agency"] edition $path:properties["edition number"] update $path:properties["update number"]`

##### Notes / risks / questions

- domain confirmation says fixed `s-xxx` values should be indexed in both hyphenated and non-hyphenated forms
- for sampled data, `series` should include both family aliases (`s-100`, `s100`) and product aliases (`s-101`, `s101`)

#### Class `3`

- Representative batch id: `C3D41A15-023B-4D9E-B973-732A42A04B04`
- Sorted attribute keys: `Exchange Set Type`, `Frequency`, `Media Type`, `Product Code`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Base |
| Frequency | DAILY |
| Media Type | Zip |
| Product Code | S-100 |

##### Proposed discriminator

Rationale: exchange-set package class using `product code` rather than `product type`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"exchange set type\"]", "exists": true },
    { "path": "properties[\"product code\"]", "exists": true },
    { "path": "properties[\"product type\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `s100`, `base`, `exchange set`, `daily` | improves recall |
| authority | `ukho` | no producing agency field |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | not mapped | no numeric version field |
| minorVersion | not mapped | no numeric version field |
| category | `exchange set` | explicit package intent |
| series | add both `s-100` and `s100` | retain both fixed-value aliases in the index |
| instance | `$path:properties["exchange set type"]` | only identity-bearing field |
| searchText | `daily s100 base exchange set in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`daily s100 base exchange set in $path:properties["media type"] format`

##### Notes / risks / questions

- frequency is informative for search text but not mapped separately

#### Class `4`

- Representative batch id: `70837D39-37D8-43C8-8783-93055969BA3D`
- Sorted attribute keys: `Exchange Set Type`, `Frequency`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Base |
| Frequency | DAILY |
| Media Type | Zip |
| Product Type | S-100 |

##### Proposed discriminator

Rationale: exchange-set package class using `product type` rather than `product code`.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adds-s100" },
    { "path": "properties[\"exchange set type\"]", "exists": true },
    { "path": "properties[\"product type\"]", "exists": true },
    { "path": "properties[\"product code\"]", "exists": false }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `s100`, `base`, `exchange set`, `daily` | improves recall |
| authority | `ukho` | no producing agency field |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | not mapped | no numeric version field |
| minorVersion | not mapped | no numeric version field |
| category | `exchange set` | explicit package intent |
| series | add both `s-100` and `s100` | retain both fixed-value aliases in the index |
| instance | `$path:properties["exchange set type"]` | only identity-bearing field |
| searchText | `daily s100 base exchange set in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`daily s100 base exchange set in $path:properties["media type"] format`

##### Notes / risks / questions

- classes `3` and `4` likely belong to one rule family with alternative field aliases

### Business Unit: ` LooseLeaf` (`Id: 16`)

- Business unit description: private bu for file sharing with this supplier
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `defer pending domain confirmation`

### Business Unit: `S100-TidalService` (`Id: 17`)

- Business unit description: s111 and s104 data for tidal trial
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `defer pending domain confirmation`

### Business Unit: `AVCS-BespokeExchangeSets` (`Id: 18`)

- Business unit description: bess outputs, these need to be collected from ui
- Discovery summary: `16` batches, `2` classes, coverage `16 / 16`
- Domain confirmation: support/readme/startup-pack classes in this business unit should be indexed where rule metadata is available

#### Class `1`

- Representative batch id: `E0979297-3A92-41A8-82A4-14B7C3E54856`
- Sorted attribute keys: `Audience`, `Content`

| AttributeKey | AttributeValue |
|---|---|
| Audience | Forth Ports |
| Content | Bespoke README |

##### Proposed discriminator

Rationale: `content = bespoke readme` identifies a support/readme class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcs-bespokeexchangesets" },
    { "path": "properties[\"content\"]", "eq": "bespoke readme" }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `bespoke`, `readme`, `avcs` | limited recall support |
| authority | `ukho` | no authority field |
| region | not mapped | no region evidence |
| format | `text` | readme-like support asset |
| majorVersion | not mapped | no version field |
| minorVersion | not mapped | no version field |
| category | `support` | support/readme asset |
| series | `avcs` | family association |
| instance | `$path:properties["audience"]` | audience is the useful identity |
| searchText | `bespoke avcs readme for $path:properties["audience"]` | semantic summary |

##### Proposed search text

`bespoke avcs readme for $path:properties["audience"]`

##### Notes / risks / questions

- domain confirmation says support/readme/startup-pack classes should be indexed

#### Class `2`

- Representative batch id: `8EA0DD33-0357-4FB1-A9BD-031813DFF456`
- Sorted attribute keys: `Audience`, `Exchange Set Type`, `Frequency`, `Media Type`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Audience | MartinRE-AllGB |
| Exchange Set Type | Base |
| Frequency | Hourly |
| Media Type | Zip |
| Product Type | AVCS |
| Week Number | 41 |
| Year | 2025 |
| Year / Week | 2025 / 41 |

##### Proposed discriminator

Rationale: `exchange set type` and period fields make this the actual bespoke exchange-set class.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "avcs-bespokeexchangesets" },
    { "path": "properties[\"exchange set type\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `bespoke`, `exchange set`, `avcs`, `hourly` | improves recall |
| authority | `ukho` | no authority field |
| region | not mapped | no region field |
| format | `$path:properties["media type"]` | explicit media format |
| majorVersion | `toInt($path:properties["year"])` | year is major period |
| minorVersion | `toInt($path:properties["week number"])` | week is minor period |
| category | `exchange set` | explicit package intent |
| series | `avcs` | product family |
| instance | `$path:properties["audience"]` | bespoke audience identity |
| searchText | `bespoke avcs $path:properties["exchange set type"] exchange set for $path:properties["audience"] in $path:properties["media type"] format` | semantic summary |

##### Proposed search text

`bespoke avcs $path:properties["exchange set type"] exchange set for $path:properties["audience"] in $path:properties["media type"] format`

##### Notes / risks / questions

- audience is likely important for access control and should remain searchable if these outputs are indexed

### Business Unit: `SENC` (`Id: 19`)

- Business unit description: specific bu for file sharing with sencs but not really anything
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `defer pending domain confirmation`

### Business Unit: `TestPenrose-S57` (`Id: 20`)

- Business unit description: old test bu
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `no searchable rule proposed`

### Business Unit: `TestPenrose-S63` (`Id: 21`)

- Business unit description: old test bu
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `no searchable rule proposed`

### Business Unit: `PrintedMedia` (`Id: 22`)

- Business unit description: for sending files to packaged sounds printer
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `defer pending domain confirmation`

### Business Unit: `DefenceProducts` (`Id: 23`)

- Business unit description: defence file sharing collected from ui - defence data in here so do not use for testing
- Discovery summary: `0` batches, `0` classes, coverage `0 / 0`
- No classes were discovered because this business unit currently has no batches.
- Recommendation: `defer pending domain confirmation`

### Business Unit: `ADSD-ViewerUpdates` (`Id: 24`)

- Business unit description: sailing directions data being added here in case adds distributers ever want to get it via api (no files displayed on fss ui)
- Discovery summary: `5` batches, `1` classes, coverage `5 / 5`
- Domain confirmation: make `adsd-viewerupdates` searchable so api consumers benefit from indexing

#### Class `1`

- Representative batch id: `EF818CB7-DED6-4A8D-A4D8-3C94A315CEAC`
- Sorted attribute keys: `Product Type`, `PublishDateTime`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | ADV |
| PublishDateTime | 11/07/2025 09:10:15 +00:00 |

##### Proposed discriminator

Rationale: only class in this business unit; `publishdatetime` is the key metadata signal.

```json
{
  "all": [
    { "path": "properties[\"businessunitname\"]", "eq": "adsd-viewerupdates" },
    { "path": "properties[\"publishdatetime\"]", "exists": true }
  ]
}
```

##### Proposed canonical mapping

| Canonical Field | Source / Expression | Mapping Rationale |
|---|---|---|
| keywords | copy all batch attribute values; add `adsd`, `viewer`, `update`, `adv` | improves recall |
| authority | `ukho` | domain fit |
| region | not mapped | no region field |
| format | not mapped | no media field |
| majorVersion | not mapped | would require date-part extraction not yet specified |
| minorVersion | not mapped | would require date-part extraction not yet specified |
| category | `viewer update` | business-unit intent |
| series | `adsd` | stable family label |
| instance | `$path:properties["publishdatetime"]` | only identity-bearing field |
| searchText | `adsd viewer update published $path:properties["publishdatetime"]` | semantic summary |

##### Proposed search text

`adsd viewer update published $path:properties["publishdatetime"]`

##### Notes / risks / questions

- domain confirmation says `adsd-viewerupdates` should be searchable even though files are not shown on the ui

## 4. Cross-business-unit observations

- `adds` and `adds-s57` form closely related cell-update rule families and should likely share authoring patterns
- `adds-s100` forms a distinct s100 product family with strong potential for reusable product and exchange-set rule templates
- for `adds-s100`, fixed `s-xxx` values are now confirmed to require both hyphenated and non-hyphenated aliases in index mapping, and mixed family/product aliases can coexist in `series`
- catalogue/media-package patterns recur across `adds`, `adds-s57`, `avcsdata`, `adp`, `aenp`, `arcs`, and `avcs-bespokeexchangesets`
- `printtoorder` has rich metadata and is now confirmed for rule inclusion despite the original description saying it was not searchable
- `adsd-viewerupdates` is now confirmed searchable for api consumers even though files are not shown on the ui
- support/readme/startup-pack classes in `adds`, `adds-s57`, `var`, and `avcs-bespokeexchangesets` are now confirmed for inclusion, although some still only support minimal mappings because metadata is sparse
- several other low-value support/business-continuity/file-sharing units (`addssupport`, `paperproducts`, `chersoft`) still have technically mappable metadata but weaker user-facing search value
- zero-batch business units should remain outside rule authoring until real evidence exists

## 5. Open questions requiring domain confirmation

No open questions remain in this version.
