# Specification: Wiki review and overhaul for `UKHO.Search`

Version: `v0.01`  
Status: `Draft`  
Work Package: `docs/090-wiki-overhaul/`  
Based on: `docs/026-s57-parser/spec-template_v1.1.md`

## 1. Overview

### 1.1 Purpose
Define the documentation overhaul needed to bring `./wiki` into alignment with the current implementation, expand the depth and clarity of developer guidance, and establish a much richer Workbench documentation set for new contributors.

### 1.2 Scope
This work package covers markdown-only deliverables for the repository wiki.

In scope:

- review every existing page under `wiki/`
- update phase-oriented wording so the wiki always describes the current state of development
- preserve all current information while improving structure, accuracy, and readability
- verify all existing Mermaid diagrams for currency and correctness
- expand pages so they explain concepts, rationale, examples, and developer workflows more clearly
- substantially extend Workbench documentation, including a multi-page `Developer's Guide to Workbench`
- define explicit wiki maintenance rules, update expectations, and ownership guidance for future work packages
- add a repository instruction file at `.github/instructions/wiki.instructions.md`
- require future work-package execution prompts to treat wiki updates as mandatory in scope where relevant, in the same explicit way that documentation-pass is currently mandated
- require future work-package planning prompts to include wiki review and update expectations in the specification-to-plan workflow

Out of scope:

- code changes
- deletion of valid current information from the wiki
- rewriting historical work-package specifications under `docs/`

### 1.3 Stakeholders
- new developers onboarding to `UKHO.Search`
- existing maintainers of ingestion, query, infrastructure, and Workbench areas
- technical leads who rely on the wiki as a current-state reference
- contributors extending Workbench modules and contribution points
- GitHub Copilot operating through the repository work-package prompts and instructions, as the mandatory agent responsible for performing end-of-work-package wiki updates

### 1.4 Definitions
- **Current-state wiki**: documentation that reflects how the repository works now, not how it worked in an earlier implementation phase
- **Workbench**: the desktop-style Blazor-based host and supporting projects under the Workbench solution areas
- **Contribution-based model**: the Workbench design where tools, menus, toolbars, explorers, and related UI capabilities are composed through bounded contracts rather than hard-coded shell coupling
- **Developer's Guide to Workbench**: the new multi-page documentation set intended to read like a guided narrative rather than terse reference material
- **Mandatory wiki update**: a required review and update step performed by GitHub Copilot at the end of each relevant work package before the work can be treated as complete

## 2. System context

### 2.1 Current state
The repository already contains a wiki with pages covering core architecture, setup, ingestion, rules, provider concepts, tools, and some Workbench material. The content provides useful knowledge, but some pages still describe implementation phases or transitional states, which can make the wiki harder for a new developer to trust as a source of truth.

The existing Workbench documentation is present but comparatively narrow relative to the scope of the Workbench model now in the repository. The user has identified a need for a much more substantial guide that explains not only what exists, but why the model works the way it does and how a developer should use and extend it.

### 2.2 Proposed state
The wiki will become a richer, current-state, developer-focused guide that:

- describes the repository as it exists now
- preserves valid current information while improving wording, structure, and discoverability
- validates and refreshes Mermaid diagrams wherever needed
- explains concepts in a more narrative, example-driven style suitable for developers unfamiliar with the project
- expands Workbench coverage into a cohesive multi-page guide with introductions, conceptual explanation, extension guidance, and worked examples
- fully redesigns the wiki information architecture where that creates a clearer, more book-like learning journey
- strongly prioritises Workbench depth while also substantially expanding architecture, setup, and ingestion guidance
- removes phase history from the main narrative unless that history is essential to understanding the current design
- includes a very detailed Workbench guide with separate pages for core concepts, examples or tutorials, and troubleshooting guidance
- uses both conceptual examples and step-by-step developer recipes in the Workbench guide
- adopts a broadly book-like structure across the wider wiki, using smaller narrative pages across major topics where that improves the developer journey
- keeps general pages focused on concepts and current-state understanding, while adding dedicated walkthrough pages for detailed code-oriented explanation of major sections and concepts
- uses an explicit guided reading order across major sections, with clear start-here and recommended sequence pages
- defines clear repository rules for keeping the wiki current after each work package
- introduces a dedicated `.github/instructions/wiki.instructions.md` file for wiki maintenance standards
- requires `.github/prompts/spec.execute.prompt.md` to make wiki updates an explicit mandatory end-of-work-package activity, similar in force to the existing documentation-pass requirement
- makes GitHub Copilot explicitly responsible for carrying out that wiki update step as part of finishing each relevant work package
- assumes GitHub Copilot, not human authors, will write and maintain the wiki pages covered by this workflow
- requires `.github/prompts/spec.plan.prompt.md` to make wiki review and update planning an explicit part of turning specifications into implementation plans
- applies the mandatory Copilot wiki step to every work package as a required review, while requiring an actual wiki edit only when relevant project knowledge has changed
- requires future implementation plans to both include wiki expectations in each work item's definition of done and include a final explicit wiki review or update work item
- records the result of the mandatory wiki review in the final work item or final summary, including either the wiki pages updated or the reason no update was needed
- defines `.github/instructions/wiki.instructions.md` as a comprehensive repository instruction covering maintenance workflow, prompt obligations, current-state rules, reporting requirements, and practical examples
- treats `.github/instructions/wiki.instructions.md` as mandatory, but slightly lighter-weight than `.github/instructions/documentation-pass.instructions.md`, while still acting as a completion gate for relevant wiki review and update work
- gives `wiki/Home.md` a richer start-here role with reading paths, audience guidance, and short summaries of each major area
- uses a structured, book-like naming convention for related wiki pages so reading order and topic grouping are obvious
- allows the exact naming pattern to be chosen during implementation so it best fits the final information architecture
- updates links and references throughout the wiki to the new structure rather than preserving compatibility or bridge pages
- provides dedicated walkthrough pages outside Workbench for architecture, setup, and ingestion, with especially deep coverage of the ingestion rule engine, its syntax, and usage examples covering all supported syntax areas
- includes practical command examples throughout the wiki, especially in setup and walkthrough pages, while preserving critical setup commands verbatim and in full
- uses text, code blocks, and Mermaid diagrams only, without screenshots or other visual assets
- includes troubleshooting guidance covering common and moderately advanced issues for setup, ingestion, and Workbench
- uses a narrative, explanatory writing style that fits the subject matter, explains concepts before introducing them, and reads like a friendly but business-like developer guide book
- uses Mermaid regularly for major concepts and flows, but does not force a diagram onto every page
- includes one central glossary so key project terminology is defined consistently across the wiki
- keeps the wiki focused on current guidance rather than adding a dedicated documentation-lineage or source-map page
- includes a single `current as of` currency note on `wiki/Home.md` rather than repeating currency markers across major pages
- includes a small number of appendix-style reference pages where they provide high-value support to the main narrative and walkthrough content
- uses appendix-style reference material primarily for command references and rule-syntax quick reference content where those help developers without interrupting the main narrative
- absorbs the existing `wiki/Workbench-Shell.md` content into the new multi-page Workbench guide and retires that standalone page

### 2.3 Assumptions
- the repository wiki under `wiki/` is the target documentation surface
- existing wiki pages can be edited, reorganized, and supplemented as long as current information is preserved
- the documentation should prefer repository-relative links and GitHub-friendly Mermaid diagrams
- the wiki should explain implementation reality, not speculative future phases, unless clearly labelled as planned work

### 2.4 Constraints
- no current information may be removed during the review
- pages that still mention historical implementation phases must be rewritten so the wiki reflects the current state at all times
- Workbench documentation must be expanded substantially beyond the existing page set
- the final writing style should be more descriptive, explanatory, and newcomer-friendly than the current wiki tone
- setup commands and other critical operational commands must not be paraphrased, abbreviated, or partially rewritten when carried into the overhauled wiki; they must be preserved verbatim and in full where accuracy matters
- the overhauled wiki must not depend on screenshots or other visual assets; diagrams should be Mermaid-based where visual explanation is needed

## 3. Component / service design (high level)

### 3.1 Documentation components
This work package covers:

1. review and revision of all existing `wiki/*.md` pages
2. verification and correction of all Mermaid diagrams in the wiki
3. expansion of existing pages where concepts are currently under-explained
4. creation of a multi-page `Developer's Guide to Workbench`, expected to include at minimum:
   - an introduction
   - architecture
   - shell
   - modules and how they work
   - the contribution-based Workbench model
   - commands and tool activation
   - tabs and layout
   - output and notifications
   - practical usage and extension guidance for developers
   - examples or tutorials
   - troubleshooting
5. broader restructuring of non-Workbench areas into smaller narrative pages where that improves readability, especially in architecture, setup, and ingestion areas
6. dedicated walkthrough pages for major sections or concepts so developers can move from narrative overview into detailed code-oriented understanding without making the general pages too implementation-heavy
7. a repository instruction document at `.github/instructions/wiki.instructions.md` defining wiki maintenance rules, update triggers, review expectations, and ownership guidance
8. prompt updates specified for `.github/prompts/spec.execute.prompt.md` so future implementation work must explicitly consider wiki updates as part of completion criteria where relevant
9. prompt updates specified for `.github/prompts/spec.plan.prompt.md` so future planning work must explicitly include wiki review and update expectations as part of converting specifications into plans
10. a comprehensive structure for `.github/instructions/wiki.instructions.md`, including purpose, ownership, mandatory triggers, writing standards, walkthrough standards, Mermaid standards, maintenance workflow, planning and execution prompt obligations, current-state rules, reporting requirements, and worked examples
11. dedicated non-Workbench walkthrough coverage for architecture, setup, and ingestion, with a particularly in-depth rules-engine guide covering syntax, semantics, and worked examples
12. a small appendix/reference set, focused on command reference material and a rule-syntax quick reference where these add value alongside the narrative pages

### 3.2 Documentation flows
The revised wiki should help a reader move through these knowledge flows:

- from repository overview to detailed subsystem understanding
- from current implementation concepts to practical developer usage
- from Workbench introduction to module composition and contribution-based extension
- from individual pages to deeper related pages through explicit cross-linking and narrative progression
- from concept pages into dedicated walkthrough pages that explain how the implementation works in code terms
- through a formal guided reading path for each major section so newcomers know where to start and what to read next

### 3.3 Key decisions
- Prefer rewriting pages into present-tense current-state documentation rather than preserving phase-oriented wording.
- Preserve valid information by integrating it into clearer structures instead of deleting it.
- Use Mermaid diagrams where they materially improve understanding, especially for architecture, composition, and Workbench interactions.
- Treat Workbench as a major documentation area deserving a guided, multi-page narrative rather than a single summary page.
- Allow a full redesign of page structure and navigation if that produces a better developer journey, while preserving valid current content.
- Remove most historical phase detail from primary page flows unless it is necessary to explain why the current design exists.
- Define the future ownership model in agent terms: GitHub Copilot must review and update the wiki at the end of each relevant work package rather than leaving wiki maintenance as an optional follow-up.
- Choose the final structured wiki naming pattern during implementation according to the resulting section hierarchy rather than forcing a fixed prefix scheme too early.
- Prefer a clean cutover to the new wiki structure by updating links everywhere rather than maintaining compatibility pages for old entry points.
- Reuse and redistribute valuable material from `wiki/Workbench-Shell.md` across the new Workbench guide and retire the old standalone page once its content has been fully absorbed.

## 4. Functional requirements

1. The work shall review all existing pages under `wiki/`.
2. The work shall update any page that refers to current or past implementation phases so that the wiki reflects the current state of development.
3. The work shall preserve all valid current information already present in the wiki.
4. The work shall review every Mermaid diagram in the wiki for currency and accuracy.
5. The work shall improve page style so that explanations are clearer and more descriptive for a developer who knows nothing about `UKHO.Search`.
6. The work shall expand the existing Workbench documentation rather than leaving it as a single lightly detailed page.
7. The work shall create a multi-page `Developer's Guide to Workbench`.
8. The Workbench guide shall explain the purpose of Workbench, how modules work, how the contribution-based model works, why the model is designed that way, and how developers use and extend it.
9. The Workbench guide shall include examples and Mermaid diagrams where they improve comprehension.
10. The revised wiki shall read more like a guided technical book or article series than a terse reference manual.
11. The revised wiki shall maintain cross-links so a reader can navigate between overview and deep-dive topics.
12. The work package shall define explicit maintenance rules, update expectations, and ownership guidance for keeping the wiki current after the overhaul.
13. The work package shall specify a new repository instruction file at `.github/instructions/wiki.instructions.md`.
14. The `wiki.instructions.md` file shall define when wiki updates are mandatory, how current-state accuracy is maintained, and how contributors should handle walkthrough pages, diagrams, and narrative structure.
15. The work package shall specify updates to `.github/prompts/spec.execute.prompt.md` so wiki updates are mandatory at the end of each relevant work package, in a manner comparable to the mandatory `documentation-pass.instructions.md` requirement.
16. The execution prompt updates shall make clear that implementation work is not complete until relevant wiki updates have been made or explicitly justified as not applicable.
17. The execution prompt updates shall make clear that GitHub Copilot is responsible for carrying out the required wiki review and update step before marking a work package complete.
18. The work package shall specify updates to `.github/prompts/spec.plan.prompt.md` so the specification-to-plan workflow explicitly includes wiki review and update expectations.
19. The planning prompt updates shall require plans to treat wiki review as mandatory for every work package and to include wiki-update work when the specified changes affect current developer-facing behavior, architecture, workflows, or concepts.
20. The mandatory Copilot wiki step shall apply to every work package as a required review, but the resulting wiki edit may be recorded as not applicable when no relevant project knowledge has changed.
21. The planning prompt updates shall require future implementation plans to carry wiki obligations in two ways: within each work item's definition of done and as a final explicit wiki review or update work item.
22. The execution and planning guidance shall require the mandatory wiki review result to be recorded in the final work item or final summary, stating which wiki pages were updated or why no update was necessary.
23. The work package shall specify `.github/instructions/wiki.instructions.md` as a comprehensive instruction file rather than a minimal checklist, and shall require it to cover maintenance workflow, planning and execution obligations, current-state documentation rules, reporting expectations, and examples.
24. The work package shall specify `.github/instructions/wiki.instructions.md` as a mandatory repository instruction with hard expectations for wiki review and relevant updates, but lighter-weight than `.github/instructions/documentation-pass.instructions.md`.
25. The redesigned `wiki/Home.md` shall act as a richer start-here landing page that includes recommended reading paths, audience guidance, and concise summaries of major wiki areas.
26. The redesigned wiki page set shall use a structured, book-like naming convention so related pages clearly read as part of the same guided sequence.
27. The redesigned wiki shall include dedicated walkthrough pages for architecture, setup, and ingestion.
28. The ingestion documentation shall include an in-depth explanation of the ingestion rule engine, its syntax, semantics, and usage examples covering all aspects of the syntax supported by the current implementation.
29. The redesigned wiki shall include practical command examples throughout, especially in setup pages and walkthrough pages.
30. Critical setup and operational commands shall be preserved verbatim and in full, including but not limited to Azure Container Registry data-image get and put commands, full Keycloak realm export commands, and any other setup commands whose exact syntax is operationally significant.
31. The overhauled wiki shall use text, code blocks, and Mermaid diagrams only, and shall not introduce screenshots or other visual assets.
32. The overhauled wiki shall include troubleshooting guidance for common and moderately advanced issues in setup, ingestion, and Workbench areas.
33. The overhauled wiki shall include one central glossary or terminology reference for the whole wiki.
34. The overhauled wiki shall not include a dedicated documentation-lineage or source-map page, and shall remain focused on current guidance.
35. The overhauled wiki shall include one `current as of` note at the bottom of `wiki/Home.md` and shall not add page-level currency markers elsewhere unless later required.
36. The overhauled wiki may include a small number of appendix-style reference pages where they add clear value to the main narrative, walkthrough, glossary, and troubleshooting content.
37. Where appendix-style reference pages are included, they shall prioritise command/reference material and a rule-syntax quick reference.
38. The existing `wiki/Workbench-Shell.md` content shall be absorbed into the new Workbench guide structure and the standalone page shall be retired once its useful content has been redistributed.

## 5. Non-functional requirements

- Writing should be accurate, current, descriptive, and developer-oriented.
- Terminology should be consistent across all pages.
- Mermaid diagrams should render cleanly in GitHub markdown viewers.
- Pages should remain maintainable and avoid unnecessary duplication where cross-linking is sufficient.
- Workbench pages should be especially fluent and explanatory because they are intended for developers with no prior project knowledge.
- Copilot-generated pages should use a narrative, explanatory structure that suits the subject matter rather than forcing a rigid page template.
- New concepts should not be introduced without explanation.
- The tone should remain friendly, business-like, and technically correct.
- Mermaid should be used regularly for major concepts and flows, but omitted where it adds little value.

## 6. Data model
Not applicable beyond markdown page structure, headings, links, and Mermaid diagrams.

## 7. Interfaces & integration
The work integrates with:

- existing markdown pages under `wiki/`
- historical and architectural context under `docs/`
- current implementation details in the repository source tree where the wiki must accurately describe present behavior

## 8. Observability (logging/metrics/tracing)
Not a runtime feature area for this work package, but the revised wiki should accurately describe observability concepts where existing pages cover them.

## 9. Security & compliance
The documentation overhaul should avoid introducing secrets or sensitive values and should keep environment-specific examples safe for repository publication.

## 10. Testing strategy
Validation for this work item is documentation-based:

- confirm all existing wiki pages have been reviewed
- confirm phase-oriented wording has been removed or rewritten into current-state language
- confirm valid current information has been preserved
- confirm Mermaid diagrams are accurate and renderable
- confirm the Workbench guide is expanded into multiple pages with narrative flow, examples, and cross-links
- confirm the wiki is understandable to a developer with no prior project context
- gather targeted evidence from the current codebase to confirm current-state accuracy for rewritten pages
- explicitly spot-check rewritten pages against the current implementation areas they describe, especially for Workbench, architecture, setup, ingestion, and prompt or instruction workflow changes

## 11. Rollout / migration
- update the existing `wiki/` pages in place where appropriate
- add new Workbench guide pages under `wiki/` as required by the final information architecture
- preserve historical detail by integrating it into the refreshed current-state narrative rather than removing valid content
- add `.github/instructions/wiki.instructions.md` as the canonical repository guidance for wiki maintenance
- update `.github/prompts/spec.execute.prompt.md` so future work-package execution explicitly includes mandatory wiki review and update expectations where relevant
- update `.github/prompts/spec.plan.prompt.md` so future planning work carries wiki review and update obligations forward from specification into plan output

## 12. Open questions
1. None at present.

## Target output paths
- `docs/090-wiki-overhaul/spec-wiki-overhaul_v0.01.md`
