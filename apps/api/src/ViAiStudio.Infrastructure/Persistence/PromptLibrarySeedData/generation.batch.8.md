## Batch 8 of 10 -- Infrastructure

Produce, under `04-infrastructure/`:
- `00-environments.md` (`INF-000`) -- an environment matrix covering exactly the selected target
  environments: {{environments}}.
- `01-docker-compose.md` (`INF-001`) -- local Compose stack covering the primary database plus every
  selected supporting infrastructure item.
- `02-dockerfiles.md` (`INF-002`) -- one image per selected deployable.
- `03-reverse-proxy-and-secrets.md` (`INF-003`).
- `04-observability-stack.md` (`INF-005`).
- `05-ci-cd.md` (`INF-006`).
- `06-backup-and-dr.md` (`INF-007`) -- omit RPO/RTO rigor for a "prototype" rigour selection; for
  "regulated", add residency and formal sign-off requirements per the selected compliance items:
  {{compliance}}.
- `07-local-developer-setup.md` (`INF-008`).

Selected rigour: {{rigour}}. Scale every budget/target in this batch to that rigour level.
