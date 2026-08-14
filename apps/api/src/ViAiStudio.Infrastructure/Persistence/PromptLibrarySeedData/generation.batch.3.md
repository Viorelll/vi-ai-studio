## Batch 3 of 10 -- Architecture and decision records

Cross-cutting decisions that bind every later component.

Produce, under `01-architecture/`:
- `00-system-context.md` (`ARCH-001`) -- containers, external actors.
- `01-solution-structure.md` (`ARCH-002`) -- repository/solution layout for the selected deployables.
- `02-coding-conventions.md` (`ARCH-003`).
- `03-multi-tenancy.md` (`ARCH-004`) -- realize the selected tenant-isolation model concretely (query
  filters, provisioning).
- `04-security-model.md` (`ARCH-005`) -- realize the selected identity model and identity features;
  this is where permission *keys* (not the matrix, which lives in `PRD-002`) get their naming
  convention.
- `05-error-model.md` (`ARCH-006`) -- error envelope shape, status code table, error code naming.
- `06-integration-events.md` (`ARCH-007`) -- only if a message worker was selected as a deployable or
  "messaging + events" was selected as a functional area; otherwise state explicitly why it is
  omitted rather than silently skipping it.
- `07-configuration-model.md` (`ARCH-008`).
- `08-observability-model.md` (`ARCH-009`).
- `adr/adr-0001-*.md` onward -- one ADR per consequential decision this batch makes, including every
  decision the intake sheet forced (e.g. the chosen database, the chosen frontend, the chosen identity
  model), using the ADR template's Context / Options considered / Decision / Consequences / Revisit
  when shape.

Skip any file whose subject was not selected in the intake (e.g. no `06-integration-events.md` with no
messaging selected) -- state the omission in `README.md`'s scope note instead of writing an empty file.
