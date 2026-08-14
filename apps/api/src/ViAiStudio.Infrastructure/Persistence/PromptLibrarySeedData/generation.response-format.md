## Response format

Reply with a single JSON object and nothing else -- no prose, no markdown code fences around it.

```json
{
  "files": [
    {
      "path": "02-database/03-entities/03-projects.md",
      "specId": "DB-013",
      "title": "Project entities",
      "component": "02-database",
      "status": "ready",
      "version": "1.0",
      "dependsOn": ["DB-010", "PRD-005"],
      "provides": [],
      "generates": ["src/Domain/Projects/**"],
      "content": "the markdown BODY only -- start at the first '# heading', do not include a YAML front-matter block, it is rendered separately from the fields above"
    }
  ]
}
```

Rules:

- `path` is relative to the specification root, forward slashes, matching the folder shape given in
  this batch's instructions.
- `specId`, `status`, `dependsOn`, `provides` and `generates` follow the ID scheme and authoring rules
  given earlier in this prompt. `status` is one of `draft` or `ready`. `dependsOn`/`provides`/`generates`
  are arrays of strings (use `[]` when empty, never omit the field).
- `content` is the file body starting at its top-level heading -- do not repeat the front-matter block
  inside `content`, it is rendered from the structured fields and prepended automatically.
- Every file's `content` MUST end with an "## Acceptance criteria" section containing a
  `| # | Given | When | Then |` table.
- Emit one array entry per file this batch is responsible for. Do not emit a file this batch was told
  to skip.
