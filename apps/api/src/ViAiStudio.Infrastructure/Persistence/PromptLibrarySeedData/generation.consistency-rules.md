## Consistency requirements across batches

- The permission matrix in `00-product` is the single source for every authorization statement later.
  Endpoint specs reference its permission keys; they do not invent new ones.
- Every entity in `02-database` is reachable from an endpoint spec, a job spec, or an explicit note
  saying why it is internal.
- Every endpoint that returns a collection states its pagination style, its sort allow-list and its
  page-size cap.
- Every endpoint states the permission required and the response when it is absent.
- Every tenant-scoped entity states how isolation is enforced, and cross-tenant access returns 404.
- Every background job states its schedule, its singleton behaviour, its timeout and its idempotency
  key.
- Every message states its envelope fields, its retry ladder and its dead-letter behaviour.
- Every numeric target in the non-functional requirements appears in a quality spec as a gate.
