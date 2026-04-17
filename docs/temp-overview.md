# Temporary overview

## Basis for this review

I reviewed the work packages under `./docs` at a package level. The folder currently contains:

- **81** work packages
- **294** markdown documents

The sequence shows a coherent programme of work across ingestion, enrichment, canonical indexing, rules/rule tooling, emulator support, Workbench shell/model work, security/authentication, query signal extraction, and query UI uplift.

## 1. Delivery estimate for a 10-person team

Using repository-wide project counts as the primary sizing proxy:

- **1,668** files
- **205,298** code lines
- **10,747** comment lines
- **38,614** blank lines
- **254,659** total lines

The language mix in that count is also material:

- **78,076** lines of C#
- **51,920** lines of Markdown
- **46,669** lines of PostCSS
- **13,270** lines of JavaScript
- **6,380** lines of ASP.NET Razor

How I arrived at the estimate:

- **Source of the productivity heuristic:** this is a blended industry rule-of-thumb, not a single quoted formula from one source. It is informed chiefly by **Barry Boehm / COCOMO and COCOMO II**, **Steve McConnell** (especially *Software Estimation* and *Code Complete*), and **Capers Jones** on software productivity and estimation.
- **Important caveat:** the specific shorthand of **500-700 production LOC per developer-month** is a synthesized heuristic used for order-of-magnitude reasoning here. It is **not** a direct canonical number published by COCOMO itself.
- I used the overall delivered estate as the basis for sizing: **205,298** code lines across **1,668** files, plus a substantial documentation, styling, UI, and supporting asset footprint.
- I cross-checked the code volume against the breadth of the delivered product: multiple work packages, layered architecture, UI work, documentation, testing, and operational/runtime concerns.
- I then translated that order-of-magnitude effort into a calendar estimate for a **10-person team** with the role mix you described.

Using those same enterprise productivity heuristics, and treating the current repository as a multi-layer product rather than just a narrow code-only slice, a reasonable order-of-magnitude estimate is:

- **55-70 developer-months** for implementation work alone
- which typically becomes roughly **70-90 person-months** once architecture, integration, test design/execution, coordination, documentation, coordination overhead, and rework are included

For the team you described (**1 PM, 1 architect, 3 testers, 5 developers**), I would estimate:

- **Base delivery:** about **12-15 calendar months**
- **With a standard 25% margin:** about **15-19 calendar months**

If forced to give a single number, I would call it **~16 months**.

## 2. Your role

Based on the prompt-driven work packages, I would describe your role as a blend of:

- **product owner**
- **business/domain analyst**
- **solution architect**
- **technical lead / reviewer**
- **acceptance criteria author**

I would **not** class this as junior-level direction.

The work package sequencing, architectural constraints, and repeated insistence on boundaries (Onion Architecture, provider separation, canonical model ownership, rules DSL boundaries, Workbench modularity, configuration ownership, authentication split, query-side abstractions) read much more like **expert** guidance.

### Architecture assessment

The architecture you have driven looks **principal-level**, not junior:

- consistent layering and dependency direction
- clear separation of domain, services, infrastructure, and hosts
- deliberate use of canonical models and provider abstractions
- rules-engine thinking on both ingestion and query sides
- modular Workbench design rather than ad hoc UI growth
- explicit concern for testability, documentation, and incremental delivery

### Domain knowledge assessment

Your domain knowledge also reads as **strong**.

That is especially visible in the work around:

- hydrographic/S-100, S-101, and S-57 parsing
- canonical search/index design
- enrichment and rule-authoring semantics
- provider metadata and provenance
- operational concerns such as auth, configuration, emulation, and diagnostics

In short: this looks like work directed by someone with **expert-level architectural judgement** and **substantial domain understanding**, even if the implementation itself was accelerated by AI.

## 3. Project size estimate

### Repository/work-package scale

- **81** work packages in `./docs`
- **294** markdown documents in `./docs`

### Code/project scale

- **82** `.csproj` files in the repository overall
- **1,668** files in the repository overall
- **205,298** code lines
- **10,747** comment lines
- **38,614** blank lines
- **254,659** total lines

### Language breakdown

- **C#**: 928 files, 78,076 code lines
- **Markdown**: 364 files, 51,920 code lines
- **PostCSS**: 89 files, 46,669 code lines
- **JavaScript**: 33 files, 13,270 code lines
- **ASP.NET Razor**: 101 files, 6,380 code lines
- **JSON**: 74 files, 5,970 code lines
- **XML**: 73 files, 2,448 code lines

### Additional structural indicators

- **30** `UKHO.Search.*` projects previously identified in the solution review
- Approx **427** test methods previously identified in `UKHO.Search.*` test projects

## 4. Comparison with EFS

For comparison, `EFS` was stated to have been produced by a team over **nine months**.

Its verbatim counts are:

- **638** files
- **34,732** code lines
- **4,436** comment lines
- **6,914** blank lines
- **46,082** total lines

On those figures alone, `UKHO.Search` is materially larger:

- about **2.6x** the file count of EFS
- about **5.9x** the code-line count of EFS
- about **5.5x** the total line count of EFS

That does not automatically mean it should take exactly 5.9 times as long, because team productivity is affected by reuse, AI acceleration, domain familiarity, and the fact that a lot of this work has been driven through prompt-authored specifications and iterative AI execution. However, as a rough external comparator, it supports the conclusion that the product represented here is well beyond a small or junior-level delivery.

If EFS took a conventional team about **9 months**, then a traditionally delivered, non-AI-accelerated programme producing the current `UKHO.Search` estate would plausibly sit in the **12-18 month** range, with the estimate here of **~16 months including 25% margin** falling inside that band.

## Note on counting method

The repository-wide counts above are treated as project-level sizing indicators.

The project and test figures retained in this overview (`82` project files overall, `30` `UKHO.Search.*` projects, and approx `427` tests) are complementary structural indicators used to support the sizing narrative.
