## Template: an integration event or command

```markdown
# Message: `<Name>IntegrationEvent` v1

**Kind.** event (past tense fact) | command (imperative)
**Exchange / queue.** `<exchange>` -> `<queue>` * routing key `<module>.<name>.v1`
**Publisher.** `<module>` via transactional outbox
**Consumers.** `<list>` -- each maintains its own inbox row

## Payload

```jsonc
{
  "messageId": "uuid",          // idempotency key, unique per publish
  "correlationId": "uuid",
  "causationId": "uuid|null",
  "tenantId": "uuid",
  "occurredAtUtc": "2026-01-01T00:00:00Z",
  "schemaVersion": 1,
  "data": {
    "<field>": "<value>"
  }
}
```

## Compatibility rules

- Adding an optional field -> same version.
- Removing, renaming or retyping a field -> new version, both published in parallel for >= 1 release.
- Consumers MUST ignore unknown fields.

## Delivery guarantees

At-least-once. Consumers MUST be idempotent via the inbox table. Ordering is **not** guaranteed;
handlers MUST tolerate out-of-order arrival.

## Failure handling

| Attempt | Delay |
|---|---|
| 1-5 | 5 s, 30 s, 2 min, 10 min, 1 h (+/- 20 % jitter) |
| after 5 | dead-letter queue `<queue>.dlq`, alert, manual replay via admin |

## Acceptance criteria

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | the same `messageId` delivered twice | consumer runs twice | side effect applied exactly once |
```
