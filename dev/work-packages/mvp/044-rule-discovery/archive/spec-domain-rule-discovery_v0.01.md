# Ingestion rule discovery specification

Version: `v0.01`
Work Package: `044-rule-discovery`

## 1. Overview

This document records a discovery-only analysis of the local SQL Server file-share emulator dataset in order to identify the distinct batch attribute shapes that are likely to require ingestion rules later.

The source data uses an Entity Attribute Value structure:

- `dbo.BusinessUnit`
- `dbo.Batch`
- `dbo.BatchAttribute`

This document does **not** define final ingestion rules, JSON rulesets, or implementation code. It only identifies recurring attribute-key signatures, representative examples, and coverage evidence.

## 2. Execution metadata

- Execution date/time: `2026-03-18T11:53:05.1922150+00:00`
- Connection target summary: `Server=127.0.0.1,63410; Database=fileshare-emulator-db; User ID=sa; Password=***; TrustServerCertificate=true`
- Query tool: `sqlcmd`
- Total business unit count: `24`
- Total batch count: `200,951`
- Total batch attribute count: `1,325,703`
- Total discovered signature classes: `52`

## 3. Coverage summary

| BusinessUnitId | BusinessUnitName | BatchCount | DiscoveredClassCount | CoveredBatchCount | CoverageStatus |
|---|---|---:|---:|---:|---|
| 1 | ADDS | 108743 | 7 | 108743 | OK |
| 2 | ADDS-S57 | 84731 | 6 | 84731 | OK |
| 3 | AVCSCustomExchangeSets | 2 | 1 | 2 | OK |
| 4 | AVCSData | 14 | 5 | 14 | OK |
| 5 | MaritimeSafetyInformation | 216 | 3 | 216 | OK |
| 6 | BritishLegalDepositLibraryPublications | 0 | 0 | 0 | OK |
| 7 | PrintToOrder | 4369 | 11 | 4369 | OK |
| 8 | ADDSSupport | 1 | 1 | 1 | OK |
| 9 | ADP | 5 | 2 | 5 | OK |
| 10 | AENP | 100 | 4 | 100 | OK |
| 11 | ARCS | 22 | 1 | 22 | OK |
| 12 | PaperProducts | 2 | 2 | 2 | OK |
| 13 | Chersoft | 92 | 1 | 92 | OK |
| 14 | VAR | 2 | 1 | 2 | OK |
| 15 | ADDS-S100 | 2631 | 4 | 2631 | OK |
| 16 |  LooseLeaf | 0 | 0 | 0 | OK |
| 17 | S100-TidalService | 0 | 0 | 0 | OK |
| 18 | AVCS-BespokeExchangeSets | 16 | 2 | 16 | OK |
| 19 | SENC | 0 | 0 | 0 | OK |
| 20 | TestPenrose-S57 | 0 | 0 | 0 | OK |
| 21 | TestPenrose-S63 | 0 | 0 | 0 | OK |
| 22 | PrintedMedia | 0 | 0 | 0 | OK |
| 23 | DefenceProducts | 0 | 0 | 0 | OK |
| 24 | ADSD-ViewerUpdates | 5 | 1 | 5 | OK |

## 4. Business unit discovery

### Business Unit: `ADDS` (`Id: 1`)

- Total batch count: `108,743`
- Discovered classes: `7`
- Coverage result: `108,743 / 108,743` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 101074 | `38813F7B-07D1-4317-AEBE-000053A25DF0` | 6 | `Agency|CellName|EditionNumber|ProductCode|TraceId|UpdateNumber` |
| 2 | 6875 | `F7FE179D-6EAA-443C-80AD-0016CE394FAD` | 7 | `Agency|CellName|EditionNumber|ProductCode|Source|TraceId|UpdateNumber` |
| 3 | 547 | `4A64E8FB-C46B-441E-91F7-0031ABC9FB62` | 6 | `CellName|EditionNumber|ProductCode|Source|TraceId|UpdateNumber` |
| 4 | 239 | `F1BE8ABC-D56E-46AF-90FE-0004F5D27D20` | 5 | `Agency|CellName|EditionNumber|ProductCode|UpdateNumber` |
| 5 | 4 | `32A42D7D-2C83-4CF0-AC38-3F76F6A386D6` | 2 | `Content|Product Type` |
| 6 | 3 | `FBC3B496-30FB-45DA-8BD4-6D5C1C5B719B` | 1 | `Product Type` |
| 7 | 1 | `04B78CE7-C288-400C-BA80-73B765EADF8F` | 7 | `Catalogue Type|Content|Frequency|Product Type|Week Number|Year|Year / Week` |

#### Class `1`

- Batch count: `101,074`
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

#### Class `2`

- Batch count: `6,875`
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

#### Class `3`

- Batch count: `547`
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

#### Class `4`

- Batch count: `239`
- Representative batch id: `F1BE8ABC-D56E-46AF-90FE-0004F5D27D20`
- Sorted attribute keys: `Agency`, `CellName`, `EditionNumber`, `ProductCode`, `UpdateNumber`

| AttributeKey | AttributeValue |
|---|---|
| Agency | FR |
| CellName | FR57376B |
| EditionNumber | 2 |
| ProductCode | AVCS |
| UpdateNumber | 1 |

#### Class `5`

- Batch count: `4`
- Representative batch id: `32A42D7D-2C83-4CF0-AC38-3F76F6A386D6`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | DVD INFO |
| Product Type | AVCS |

#### Class `6`

- Batch count: `3`
- Representative batch id: `FBC3B496-30FB-45DA-8BD4-6D5C1C5B719B`
- Sorted attribute keys: `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | AVCS |

#### Class `7`

- Batch count: `1`
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

### Business Unit: `ADDS-S57` (`Id: 2`)

- Total batch count: `84,731`
- Discovered classes: `6`
- Coverage result: `84,731 / 84,731` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 82279 | `FD8197D1-A269-42F0-8A46-000000444A9E` | 7 | `Agency|CellName|EditionNumber|ProductCode|Source|TraceId|UpdateNumber` |
| 2 | 2446 | `A1347B4D-5E2B-441A-A2D0-000EB9C5C2CB` | 8 | `Agency|CellName|EditionNumber|Original Source|ProductCode|Source|TraceId|UpdateNumber` |
| 3 | 3 | `36409275-7175-418A-BC00-016951CF32F6` | 9 | `Agency|CellName|EditionNumber|Original Source|ProductCode|s57-CRC|Source|TraceId|UpdateNumber` |
| 4 | 1 | `87CE7C1C-39CD-4B03-A623-F148746D9CF5` | 3 | `Catalogue Type|Content|Product Type` |
| 5 | 1 | `67B69A84-9089-4546-9CA6-9A7268AED419` | 2 | `Content|Product Type` |
| 6 | 1 | `CA6D5B9F-B509-4DD5-A6A6-4CAFD70750DF` | 2 | `DVD INFO|Product Type` |

#### Class `1`

- Batch count: `82,279`
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

#### Class `2`

- Batch count: `2,446`
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

#### Class `3`

- Batch count: `3`
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

#### Class `4`

- Batch count: `1`
- Representative batch id: `87CE7C1C-39CD-4B03-A623-F148746D9CF5`
- Sorted attribute keys: `Catalogue Type`, `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Catalogue Type | ADC |
| Content | Catalogue |
| Product Type | AVCS |

#### Class `5`

- Batch count: `1`
- Representative batch id: `67B69A84-9089-4546-9CA6-9A7268AED419`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | DVD INFO |
| Product Type | AVCS |

#### Class `6`

- Batch count: `1`
- Representative batch id: `CA6D5B9F-B509-4DD5-A6A6-4CAFD70750DF`
- Sorted attribute keys: `DVD INFO`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| DVD INFO | AVCS |
| Product Type | AVCS |

### Business Unit: `AVCSCustomExchangeSets` (`Id: 3`)

- Total batch count: `2`
- Discovered classes: `1`
- Coverage result: `2 / 2` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 2 | `CD1E5514-688F-4054-9644-76E8A94C7276` | 3 | `Exchange Set Type|Media Type|Product Type` |

#### Class `1`

- Batch count: `2`
- Representative batch id: `CD1E5514-688F-4054-9644-76E8A94C7276`
- Sorted attribute keys: `Exchange Set Type`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Update |
| Media Type | Zip |
| Product Type | AVCS |

### Business Unit: `AVCSData` (`Id: 4`)

- Total batch count: `14`
- Discovered classes: `5`
- Coverage result: `14 / 14` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 8 | `656D2B8C-A7FC-4230-A46E-002E346E34C4` | 5 | `Exchange Set Type|Product Type|Week Number|Year|Year / Week` |
| 2 | 2 | `1AEF90F8-3433-4605-ADF9-408F9608144F` | 6 | `Catalogue Type|Content|Product Type|Week Number|Year|Year / Week` |
| 3 | 2 | `37B289EB-43ED-4A9F-B5FF-0A26FB4F77BD` | 7 | `Exchange Set Type|Media Type|Product Type|S63 Version|Week Number|Year|Year / Week` |
| 4 | 1 | `47D46FC7-F795-4230-8698-52043124302C` | 3 | `Content|Product Type|Service` |
| 5 | 1 | `254E56B0-4246-48EF-9D6C-270AB5A9153D` | 3 | `Content|Product Type|Year` |

#### Class `1`

- Batch count: `8`
- Representative batch id: `656D2B8C-A7FC-4230-A46E-002E346E34C4`
- Sorted attribute keys: `Exchange Set Type`, `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | AIO |
| Product Type | AIO |
| Week Number | 10 |
| Year | 2026 |
| Year / Week | 2026 / 10 |

#### Class `2`

- Batch count: `2`
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

#### Class `3`

- Batch count: `2`
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

#### Class `4`

- Batch count: `1`
- Representative batch id: `47D46FC7-F795-4230-8698-52043124302C`
- Sorted attribute keys: `Content`, `Product Type`, `Service`

| AttributeKey | AttributeValue |
|---|---|
| Content | Certificate |
| Product Type | AVCS |
| Service | AVCS OUS |

#### Class `5`

- Batch count: `1`
- Representative batch id: `254E56B0-4246-48EF-9D6C-270AB5A9153D`
- Sorted attribute keys: `Content`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Content | Catalogue |
| Product Type | AVCS |
| Year | 2023 |

### Business Unit: `MaritimeSafetyInformation` (`Id: 5`)

- Total batch count: `216`
- Discovered classes: `3`
- Coverage result: `216 / 216` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 105 | `BCEE859A-84E7-4D72-85EF-02119167B285` | 7 | `Content|Data Date|Frequency|Product Type|Week Number|Year|Year / Week` |
| 2 | 105 | `44DA7BAA-1665-4F2B-9B6A-00A9F8E40B98` | 6 | `Data Date|Frequency|Product Type|Week Number|Year|Year / Week` |
| 3 | 6 | `C1727824-8D5F-4420-9D74-0B3FC3946AC0` | 4 | `Data Date|Frequency|Product Type|Year` |

#### Class `1`

- Batch count: `105`
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

#### Class `2`

- Batch count: `105`
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

#### Class `3`

- Batch count: `6`
- Representative batch id: `C1727824-8D5F-4420-9D74-0B3FC3946AC0`
- Sorted attribute keys: `Data Date`, `Frequency`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Data Date | 2024-02-29 |
| Frequency | Cumulative |
| Product Type | Notices to Mariners |
| Year | 2024 |

### Business Unit: `BritishLegalDepositLibraryPublications` (`Id: 6`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `PrintToOrder` (`Id: 7`)

- Total batch count: `4,369`
- Discovered classes: `11`
- Coverage result: `4,369 / 4,369` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 2599 | `41698497-5397-4391-AC8B-003EFDB75A9A` | 11 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|Nm Number|Paper Depth|Paper Width|Plan Type|Product Type|Publication Date` |
| 2 | 892 | `721F2AC6-ACF4-46C7-B03E-00D8A87732C9` | 12 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|INT Number|Nm Number|Paper Depth|Paper Width|Plan Type|Product Type|Publication Date` |
| 3 | 576 | `D74D3EEC-4EF1-4C86-9A4B-0067B061D627` | 10 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|Paper Depth|Paper Width|Plan Type|Product Type|Publication Date` |
| 4 | 132 | `6A4BACB9-FA06-4B51-A84B-01F400444D14` | 6 | `Chart Number|Chart Title|Edition Date|Edition Number|Product Type|Publication Date` |
| 5 | 102 | `D59C5655-ECBF-4103-844C-009DF56929FA` | 11 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|INT Number|Paper Depth|Paper Width|Plan Type|Product Type|Publication Date` |
| 6 | 30 | `CC4CCF76-CD1B-4344-BDA5-040C2013BDE9` | 9 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|Paper Depth|Paper Width|Product Type|Publication Date` |
| 7 | 21 | `5152F58A-CA67-40F7-8E7A-022912DEA4E2` | 5 | `Chart Number|Chart Title|Edition Number|Product Type|Publication Date` |
| 8 | 8 | `ABBE5329-D7EB-49BF-9780-03DC2633DB9E` | 8 | `Chart Number|Chart Title|Edition Date|Edition Number|Paper Depth|Paper Width|Product Type|Publication Date` |
| 9 | 5 | `CF03BD30-B1B0-4ED0-AA03-1304416FEEC2` | 7 | `Chart Number|Chart Title|Edition Number|Paper Depth|Paper Width|Product Type|Publication Date` |
| 10 | 3 | `90BFDDA6-A75E-4D25-A959-10CDE9064801` | 7 | `Chart Number|Chart Title|Edition Date|Edition Number|INT Number|Product Type|Publication Date` |
| 11 | 1 | `2E6746BA-B5C1-43FA-8F86-A0F9514FDB5F` | 10 | `Chart Number|Chart Title|Chart Version Id|Edition Date|Edition Number|Nm Number|Paper Depth|Paper Width|Product Type|Publication Date` |

#### Class `1`

- Batch count: `2,599`
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

#### Class `2`

- Batch count: `892`
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

#### Class `3`

- Batch count: `576`
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

#### Class `4`

- Batch count: `132`
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

#### Class `5`

- Batch count: `102`
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

#### Class `6`

- Batch count: `30`
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

#### Class `7`

- Batch count: `21`
- Representative batch id: `5152F58A-CA67-40F7-8E7A-022912DEA4E2`
- Sorted attribute keys: `Chart Number`, `Chart Title`, `Edition Number`, `Product Type`, `Publication Date`

| AttributeKey | AttributeValue |
|---|---|
| Chart Number | FCS2266A |
| Chart Title | Joint Warrior 24-1 / Nordic Response 24 - Northern Overview |
| Edition Number | 1 |
| Product Type | Paper Chart |
| Publication Date | 2024-02-07 |

#### Class `8`

- Batch count: `8`
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

#### Class `9`

- Batch count: `5`
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

#### Class `10`

- Batch count: `3`
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

#### Class `11`

- Batch count: `1`
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

### Business Unit: `ADDSSupport` (`Id: 8`)

- Total batch count: `1`
- Discovered classes: `1`
- Coverage result: `1 / 1` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 1 | `A92ECF31-9DCE-4F47-A191-D4E7901C6213` | 4 | `Product Type|Week Number|Year|Year / Week` |

#### Class `1`

- Batch count: `1`
- Representative batch id: `A92ECF31-9DCE-4F47-A191-D4E7901C6213`
- Sorted attribute keys: `Product Type`, `Week Number`, `Year`, `Year / Week`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | SAR TEST |
| Week Number | 44 |
| Year | 2024 |
| Year / Week | 2024 / 44 |

### Business Unit: `ADP` (`Id: 9`)

- Total batch count: `5`
- Discovered classes: `2`
- Coverage result: `5 / 5` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 4 | `06BDBA9A-454E-4792-A22E-5488267F4CC2` | 4 | `ADP Version|Content|Media Type|Product Type` |
| 2 | 1 | `0DCA217D-4453-42B3-AFC6-9AED6C9EB421` | 3 | `Content|NavPac Version|Product Type` |

#### Class `1`

- Batch count: `4`
- Representative batch id: `06BDBA9A-454E-4792-A22E-5488267F4CC2`
- Sorted attribute keys: `ADP Version`, `Content`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| ADP Version | 23 |
| Content | ADP Software |
| Media Type | DVD |
| Product Type | ADP |

#### Class `2`

- Batch count: `1`
- Representative batch id: `0DCA217D-4453-42B3-AFC6-9AED6C9EB421`
- Sorted attribute keys: `Content`, `NavPac Version`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Software |
| NavPac Version | 4.2 Trials |
| Product Type | HMNAO |

### Business Unit: `AENP` (`Id: 10`)

- Total batch count: `100`
- Discovered classes: `4`
- Coverage result: `100 / 100` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 96 | `04D0217D-CD10-4C0D-B8F6-0229408F069F` | 4 | `Edition|Product ID|Product Type|Year` |
| 2 | 2 | `D7C49021-BAD8-42E8-A7E8-A9BA78FFB11B` | 4 | `AENP Version|Content|Media Type|Product Type` |
| 3 | 1 | `059A8A5A-0E81-4426-8FF3-58C6B7E98383` | 3 | `AENP Version|Content|Product Type` |
| 4 | 1 | `3EF9D155-7280-4B03-8503-78B36C802C55` | 2 | `Product ID|Product Type` |

#### Class `1`

- Batch count: `96`
- Representative batch id: `04D0217D-CD10-4C0D-B8F6-0229408F069F`
- Sorted attribute keys: `Edition`, `Product ID`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Edition | 2024 |
| Product ID | e-NP234B |
| Product Type | AENP |
| Year | 2024 |

#### Class `2`

- Batch count: `2`
- Representative batch id: `D7C49021-BAD8-42E8-A7E8-A9BA78FFB11B`
- Sorted attribute keys: `AENP Version`, `Content`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| AENP Version | 1.3 |
| Content | AENP Software |
| Media Type | Zip |
| Product Type | AENP |

#### Class `3`

- Batch count: `1`
- Representative batch id: `059A8A5A-0E81-4426-8FF3-58C6B7E98383`
- Sorted attribute keys: `AENP Version`, `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| AENP Version | SDK |
| Content | Software |
| Product Type | AENP |

#### Class `4`

- Batch count: `1`
- Representative batch id: `3EF9D155-7280-4B03-8503-78B36C802C55`
- Sorted attribute keys: `Product ID`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Product ID | AENP Monitoring |
| Product Type | AENP |

### Business Unit: `ARCS` (`Id: 11`)

- Total batch count: `22`
- Discovered classes: `1`
- Coverage result: `22 / 22` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 22 | `B8FC73E7-A733-4501-B170-0A723CCE3555` | 6 | `Disc|Media Type|Product Type|Week Number|Year|Year / Week` |

#### Class `1`

- Batch count: `22`
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

### Business Unit: `PaperProducts` (`Id: 12`)

- Total batch count: `2`
- Discovered classes: `2`
- Coverage result: `2 / 2` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 1 | `035DE0A1-BD0F-4119-9C9D-C6116A42500D` | 3 | `Content|POD Version|Product Type` |
| 2 | 1 | `3EA9C3DB-1FDB-4E19-B9BB-C3C3FC0FE8F2` | 2 | `Content|Product Type` |

#### Class `1`

- Batch count: `1`
- Representative batch id: `035DE0A1-BD0F-4119-9C9D-C6116A42500D`
- Sorted attribute keys: `Content`, `POD Version`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Software |
| POD Version | SPC |
| Product Type | POD |

#### Class `2`

- Batch count: `1`
- Representative batch id: `3EA9C3DB-1FDB-4E19-B9BB-C3C3FC0FE8F2`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | Images |
| Product Type | Publications |

### Business Unit: `Chersoft` (`Id: 13`)

- Total batch count: `92`
- Discovered classes: `1`
- Coverage result: `92 / 92` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 92 | `E0709FA6-ACA0-4561-8D2C-02C4592829C2` | 5 | `Content|Edition|Product ID|Product Type|Year` |

#### Class `1`

- Batch count: `92`
- Representative batch id: `E0709FA6-ACA0-4561-8D2C-02C4592829C2`
- Sorted attribute keys: `Content`, `Edition`, `Product ID`, `Product Type`, `Year`

| AttributeKey | AttributeValue |
|---|---|
| Content | Annual |
| Edition | 24 |
| Product ID | e-NP314-24 |
| Product Type | AENP |
| Year | 2024 |

### Business Unit: `VAR` (`Id: 14`)

- Total batch count: `2`
- Discovered classes: `1`
- Coverage result: `2 / 2` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 2 | `CFE2E3D1-877E-432B-91C3-03A850904D67` | 2 | `Content|Product Type` |

#### Class `1`

- Batch count: `2`
- Representative batch id: `CFE2E3D1-877E-432B-91C3-03A850904D67`
- Sorted attribute keys: `Content`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Content | VAR Start-up pack zip |
| Product Type | VAR |

### Business Unit: `ADDS-S100` (`Id: 15`)

- Total batch count: `2,631`
- Discovered classes: `4`
- Coverage result: `2,631 / 2,631` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 2567 | `BF0734FD-EE32-4578-96F8-00044623DEB6` | 8 | `Edition Number|Producing Agency|Product Code|Product Identifier|Product Name|Source|Trace ID|Update Number` |
| 2 | 62 | `6C9C6917-55C0-4A88-8C5F-0238439A1568` | 8 | `Edition Number|Producing Agency|Product Identifier|Product Name|Product Type|Source|Trace ID|Update Number` |
| 3 | 1 | `C3D41A15-023B-4D9E-B973-732A42A04B04` | 4 | `Exchange Set Type|Frequency|Media Type|Product Code` |
| 4 | 1 | `70837D39-37D8-43C8-8783-93055969BA3D` | 4 | `Exchange Set Type|Frequency|Media Type|Product Type` |

#### Class `1`

- Batch count: `2,567`
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

#### Class `2`

- Batch count: `62`
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

#### Class `3`

- Batch count: `1`
- Representative batch id: `C3D41A15-023B-4D9E-B973-732A42A04B04`
- Sorted attribute keys: `Exchange Set Type`, `Frequency`, `Media Type`, `Product Code`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Base |
| Frequency | DAILY |
| Media Type | Zip |
| Product Code | S-100 |

#### Class `4`

- Batch count: `1`
- Representative batch id: `70837D39-37D8-43C8-8783-93055969BA3D`
- Sorted attribute keys: `Exchange Set Type`, `Frequency`, `Media Type`, `Product Type`

| AttributeKey | AttributeValue |
|---|---|
| Exchange Set Type | Base |
| Frequency | DAILY |
| Media Type | Zip |
| Product Type | S-100 |

### Business Unit: ` LooseLeaf` (`Id: 16`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `S100-TidalService` (`Id: 17`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `AVCS-BespokeExchangeSets` (`Id: 18`)

- Total batch count: `16`
- Discovered classes: `2`
- Coverage result: `16 / 16` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 9 | `E0979297-3A92-41A8-82A4-14B7C3E54856` | 2 | `Audience|Content` |
| 2 | 7 | `8EA0DD33-0357-4FB1-A9BD-031813DFF456` | 8 | `Audience|Exchange Set Type|Frequency|Media Type|Product Type|Week Number|Year|Year / Week` |

#### Class `1`

- Batch count: `9`
- Representative batch id: `E0979297-3A92-41A8-82A4-14B7C3E54856`
- Sorted attribute keys: `Audience`, `Content`

| AttributeKey | AttributeValue |
|---|---|
| Audience | Forth Ports |
| Content | Bespoke README |

#### Class `2`

- Batch count: `7`
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

### Business Unit: `SENC` (`Id: 19`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `TestPenrose-S57` (`Id: 20`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `TestPenrose-S63` (`Id: 21`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `PrintedMedia` (`Id: 22`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `DefenceProducts` (`Id: 23`)

- Total batch count: `0`
- Discovered classes: `0`
- Coverage result: `0 / 0` batches covered

No classes were discovered because this business unit currently has no batches.

### Business Unit: `ADSD-ViewerUpdates` (`Id: 24`)

- Total batch count: `5`
- Discovered classes: `1`
- Coverage result: `5 / 5` batches covered

| Class | BatchCount | ExampleBatchId | AttributeKeyCount | AttributeKeySignature |
|---|---:|---|---:|---|
| 1 | 5 | `EF818CB7-DED6-4A8D-A4D8-3C94A315CEAC` | 2 | `Product Type|PublishDateTime` |

#### Class `1`

- Batch count: `5`
- Representative batch id: `EF818CB7-DED6-4A8D-A4D8-3C94A315CEAC`
- Sorted attribute keys: `Product Type`, `PublishDateTime`

| AttributeKey | AttributeValue |
|---|---|
| Product Type | ADV |
| PublishDateTime | 11/07/2025 09:10:15 +00:00 |

## 5. Cross-business-unit observations

- Two attribute signatures recur across multiple business units:
  - `Content|Product Type` appears in `ADDS` (`4` batches), `ADDS-S57` (`1`), `PaperProducts` (`1`), and `VAR` (`2`).
  - `Agency|CellName|EditionNumber|ProductCode|Source|TraceId|UpdateNumber` appears in both `ADDS` (`6,875`) and `ADDS-S57` (`82,279`).
- `ADDS` and `ADDS-S57` are strongly aligned around S-57 cell update metadata, with optional differences driven by `Source`, `Original Source`, and `s57-CRC`.
- `PrintToOrder` is the most structurally diverse business unit after the two large S-57 families, with `11` classes caused by optional combinations of `INT Number`, `Nm Number`, `Chart Version Id`, `Plan Type`, and paper dimensions.
- `ADDS-S100` forms a distinct S-100 family, separate from S-57, with identifiers such as `Producing Agency`, `Product Identifier`, `Product Name`, `Trace ID`, and `Update Number`.
- Catalogue and media-pack style shapes recur across smaller product families such as `AVCSData`, `AVCSCustomExchangeSets`, `ARCS`, `ADP`, `AENP`, `PaperProducts`, and `AVCS-BespokeExchangeSets`.
- Eight business units currently contain zero batches and therefore represent no rule-shape evidence yet: `BritishLegalDepositLibraryPublications`, ` LooseLeaf`, `S100-TidalService`, `SENC`, `TestPenrose-S57`, `TestPenrose-S63`, `PrintedMedia`, and `DefenceProducts`.
- Likely high-level data families for later rule authoring are:
  - S-57 cell update batches
  - S-100 product update batches
  - exchange set / catalogue distribution batches
  - periodic publication / MSI batches
  - print-to-order paper chart batches
  - bespoke exchange set support batches

## 6. Coverage validation

### Per-business-unit validation

All `24` business units passed the coverage check:

- For business units with batches, the sum of class counts equals the total `dbo.Batch` row count for that unit.
- For business units with zero batches, both discovered class count and covered batch count are `0`.

### Overall validation

| Metric | Value |
|---|---:|
| Total `dbo.Batch` rows | 200951 |
| Total covered batches from discovered classes | 200951 |
| Coverage result | OK |

### Anomalies / unresolved issues

- No overall coverage mismatch was found.
- No business unit with batches produced an empty-signature class in this run.
- Several business units currently have no batches, so no signature discovery is possible for them until data is loaded.
- `PrintToOrder` contains a mixed naming variation between `Edition Date` and `EditionNumber` / `Edition Number` style fields across classes; later rule authoring should treat these as distinct keys unless upstream normalization is introduced.

## 7. Appendix

### Query notes

- All database access was performed with `sqlcmd`.
- Signature discovery used sorted distinct `BatchAttribute.AttributeKey` values per batch.
- Duplicate keys within the same batch were collapsed to one key for signature purposes.
- Signatures were aggregated with `STRING_AGG(... WITHIN GROUP (ORDER BY AttributeKey))`.
- Representative examples were chosen with `MIN(BatchId)` within each business-unit/signature class.

### Assumptions

- A batch belongs to exactly one discovered class based on its distinct attribute-key signature.
- The signature is for discovery/documentation only and is not itself a final rule.
- Empty business units were included in coverage even when no classes could be discovered.

### Table definitions used for analysis

- `dbo.BusinessUnit`
  - Key columns used: `Id`, `Name`
- `dbo.Batch`
  - Key columns used: `Id`, `BusinessUnitId`
- `dbo.BatchAttribute`
  - Key columns used: `BatchId`, `AttributeKey`, `AttributeValue`

### Schema interpretation summary

- `BusinessUnit` defines the parent domain grouping.
- `Batch` defines the individual ingestible unit within a business unit.
- `BatchAttribute` provides flexible metadata as key/value pairs for signature discovery.
