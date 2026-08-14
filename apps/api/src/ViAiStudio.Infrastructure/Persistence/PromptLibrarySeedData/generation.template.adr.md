## Template: an architecture decision record

```markdown
# ADR-<nnnn> -- <Decision title>

## Context

The forces at play: constraints, team size, budget, deployment targets, existing skills, deadlines.
Facts only; no advocacy yet.

## Options considered

| Option | Pros | Cons | Cost of reversing |
|---|---|---|---|
| A | | | |
| B | | | |

## Decision

We will **<X>**.

## Consequences

- Positive: ...
- Negative / accepted trade-off: ...
- Specs affected: `<SPEC-IDs>`

## Revisit when

A concrete trigger, e.g. "more than 3 engineers" or "p95 write latency > 200 ms".
```

ADRs also carry `date`, `supersedes` and `superseded_by` fields in their front-matter in addition to
the standard keys.
