## Template: generic specification

Use for any specification not covered by a more specific template below.

```markdown
# <Title>

## Purpose

One paragraph: what part of the system this document fully defines, and what it deliberately
excludes (with a pointer to the spec that owns the excluded part).

## Scope

- In scope: ...
- Out of scope: ... -> see `<SPEC-ID>`

## Behaviour

Normative statements. MUST / MUST NOT / SHOULD / MAY only.

## Contracts

Concrete schemas, signatures, DTOs, configuration keys.

## Failure and edge cases

| Situation | Required behaviour |
|---|---|
| ... | ... |

## Observability

What is logged, at what level, with which structured fields; which metrics and traces are emitted.

## Security notes

Authentication, authorisation, tenant scoping, PII handling, rate limits.

## Acceptance criteria

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | ... | ... | ... |
```
