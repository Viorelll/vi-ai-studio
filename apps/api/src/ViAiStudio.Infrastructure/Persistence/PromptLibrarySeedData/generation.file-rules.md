## Every file obeys these rules

1. YAML front-matter, exactly these keys in this order:
   `id, title, component, status, version, depends_on, provides (optional), generates (optional)`
   - `id` follows the ID scheme. Allocate sequentially per prefix. Never reuse an ID.
   - `status` is `draft` if it rests on an unresolved open question, otherwise `ready`.
   - `depends_on` lists spec IDs only, and must form a directed acyclic graph.
   - `generates` lists the code paths this spec owns. Two specs must never claim the same path.
2. Normative language: MUST, SHOULD, MAY. Never "we could", "consider", "it might be good to".
3. Contracts are shown, not described. Real JSON bodies, real column tables with
   type/nullability/default/constraint, real status codes, real cron expressions, real header names.
4. Every file ends with an acceptance criteria table: `| # | Given | When | Then |`. Every row must be
   implementable as exactly one automated test. A row that cannot be tested is not a criterion -- it is
   an aspiration, and it does not belong in the table.
5. Dense over verbose. Tables where a table works. 60-200 lines per file. If a file exceeds 250 lines
   it is two specs.
6. State the reason only where a reader would otherwise reverse the decision. One sentence, not a
   paragraph.
7. Use the vocabulary from the domain interview verbatim.
8. Cross-reference by ID (`BE-004`), never by file path.
9. Where the intake left an open question, write the spec against the proposed default, mark
   `status: draft`, and add a line naming the `OPEN-n` it depends on. Never silently invent an answer.
10. An architecture decision record for every choice that a competent reader would question in six
    months, including the ones the intake sheet forced.
