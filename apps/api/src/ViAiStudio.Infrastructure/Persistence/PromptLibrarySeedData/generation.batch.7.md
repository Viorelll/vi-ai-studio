## Batch 7 of 10 -- Remaining deployables

The remaining selected deployables consume the same entities (batch 4) and integration events
(`ARCH-007`) as the backend -- they do not redefine them.

Selected deployables for this product: {{deployables}}. Backend and frontend were already covered in
batches 5-6 if selected; write a folder under `03-apps/` **only** for whichever of the following remain
selected and were not already covered:

- "scheduler host" -> `03-apps/scheduler/` -- overview (`SCH-001`), job catalogue (`SCH-002`, one row
  per recurring/on-demand job implied by the selected functional areas, e.g. digests for notifications,
  purge jobs for soft-delete retention, export jobs for compliance), reliability and locking
  (`SCH-003`), using the job template for any job worth its own file.
- "message worker" -> `03-apps/service-bus-worker/` -- overview (`MSG-001`), message contracts and
  consumers (`MSG-002`, one row per integration event from `ARCH-007` and its consumers), outbox relay
  and sagas (`MSG-003`), using the message template.
- "operator admin app", "public API gateway", "mobile client", "desktop client", "CLI" -- if selected,
  a minimal `03-apps/<name>/00-overview.md` stating its purpose, its relationship to the backend API,
  and what it deliberately does not duplicate from the web frontend.

Write nothing under `03-apps/` for any deployable not in the selected list above.
