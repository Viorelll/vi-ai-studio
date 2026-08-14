# Identifier scheme

Stable IDs let specs reference each other without depending on file paths, which will move.

| Prefix | Domain | Folder |
|---|---|---|
| `META-` | Templates, rules, traceability | `_meta/` |
| `PRD-` | Product, personas, requirements | `00-product/` |
| `FR-` | Individual functional requirement | inside `00-product/02-functional-requirements.md` |
| `NFR-` | Individual non-functional requirement | inside `00-product/03-non-functional-requirements.md` |
| `ARCH-` | Cross-cutting architecture | `01-architecture/` |
| `ADR-` | Decision record (4 digits, never reused) | `01-architecture/adr/` |
| `DB-` | Schema, entities, migrations | `02-database/` |
| `BE-` | Backend concerns | `03-apps/backend/` |
| `API-` | Endpoint group | `03-apps/backend/endpoints/` |
| `FE-` | Frontend concerns | `03-apps/frontend/` |
| `UI-` | Screen group | `03-apps/frontend/screens/` |
| `SCH-` | Scheduler | `03-apps/scheduler/` |
| `MSG-` | Messaging worker | `03-apps/service-bus-worker/` |
| `INF-` | Infrastructure | `04-infrastructure/` |
| `QA-` | Quality | `05-quality/` |
| `DEL-` | Delivery, build order | `06-delivery/` |

Rules:

- IDs are allocated sequentially per prefix and **never reused**, even after deletion.
- A superseded spec keeps its ID, gets `status: superseded` and a `superseded_by:` field.
- Acceptance criteria are referenced as `<SPEC-ID>/AC-<n>`, e.g. `API-005/AC-3`.
- Permission keys, feature flag keys and error codes are **not** spec IDs; they live in their own
  registries, defined by the security and error-model architecture specs.
- Do not reuse an ID across batches. Each batch's prompt lists the IDs already allocated by earlier
  batches in this run -- treat that list as authoritative and continue the sequence from it.
