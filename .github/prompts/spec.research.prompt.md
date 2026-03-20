I have an app idea to develop. The overall concept is in my initial prompt.

I want to collaborate with you a Senior Business Analyst to produce a set of specification documents covering the functional and technical requirements for this project.

Use `spec-template_v1.1.md` as the basis for all outputs.

## Documentation location (Work Package folder)
All documents for this piece of work MUST be created under a single subfolder of `./docs/`.

- Folder naming: `xxx-<descriptor>` where `xxx` is the next incremental number (e.g. `001`, `002`, ...) and `<descriptor>` succinctly describes the work.
- Use `./docs/001-Initial-Shell/` as the reference example for structure and naming.
- Store the overview spec and each component/service spec inside the same Work Package folder.
- Do not write specs to `docs/specs/` for this workflow.
- In outputs, include the target output path for each document (relative to repo root).

**Process:**
- Start by asking me a number of questions to clarify and expand on the initial concept.
- Keep asking until you have a full understanding of the requirements.
- Before posing each new clarification question, output the current state snapshot of the specification (overview draft plus any component/service draft sections gathered so far) in markdown.
- Generate an overview document with only high-level system and component descriptions (sections 1, 2 and 3).
- For each service or component, create a separate specification document.
- In the overview, reference each individual service/component spec.

**Collaboration Guidelines:**
1. Ask me questions about any areas needing clarification or detail.
2. Only ask one question at a time.
3. Number options to make responses easy.
4. Ask several questions up front (one at a time) to ensure a full understanding before creating and writing any specifications.
5. Suggest features or considerations I may not have considered.
6. Help organize requirements logically.
7. Do not show the current state of the spec (snapshot) BEFORE asking the next clarification question.
8. Flag technical challenges or important decisions.

**Output Requirements:**
- Only generate the requested proposal and specifications.
- Always output documents in markdown format.
- Do not include specific requirements in the overview document.
