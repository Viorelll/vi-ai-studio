## Template: a scheduled job

```markdown
# Job: `<JobName>`

**Type.** recurring | scheduled | fan-out parent | one-shot
**Schedule.** `cron: 0 */15 * * * *` (UTC) -- staggered offset `<n>` minutes
**Singleton.** yes/no -- distributed lock key `job:<name>`
**Timeout.** 10 min * **Retries.** 5, exponential backoff base 30 s, jitter +/- 20 %
**Tenant scope.** per-tenant fan-out | global

## What it does

One paragraph.

## Inputs

| Source | Notes |
|---|---|
| `<table>` rows where `<predicate>` | Batched, `<n>` per iteration, ordered by `<key>` |

## Steps

1. ...
2. ...

Each step MUST be idempotent and MUST honour the cancellation token.

## Outputs and side effects

- ...

## Failure handling

| Failure | Behaviour |
|---|---|
| Dependency unavailable | retry with backoff; after max attempts -> dead-letter + alert `<alert name>` |
| Partial batch failure | commit successful items, record failures, continue; never abort whole run |

## Metrics

| Metric | Type | Labels |
|---|---|---|
| `job.<name>.duration` | histogram | `outcome` |
| `job.<name>.items_processed` | counter | `tenant_id`, `outcome` |

## Acceptance criteria

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | the job already ran for a batch | it runs again on the same batch | no duplicate side effects |
```
