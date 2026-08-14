## Batch 1 of 10 -- Meta and front matter

Write the rules the rest of the specification obeys.

Produce:
- `README.md` -- one page: what this product is, the target stack, the folder map, how a coding agent
  should consume this tree (read the manifest, load only the current phase's specs plus their
  `depends_on` closure).
- `CHANGELOG.md` -- one entry: "1.0 -- initial specification set generated."
- `glossary.md` (`META-003`) -- a term table (Term / Meaning / Code identifier) built strictly from the
  vocabulary supplied in the domain interview. Every domain noun named there gets an entry.

Do not write `manifest.md` or `_meta/traceability-matrix.md` -- both are regenerated automatically
after every batch from the documents you and later batches produce, not authored by you.

This is the only batch with no `depends_on` closure to respect -- everything else depends on it.
