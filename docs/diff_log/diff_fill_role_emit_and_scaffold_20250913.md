# Diff: Fill role EMIT and DDL scaffold

- Add EMIT CHANGES for Role.Fill via RoleTraits (already in place).
- Introduce KsqlFillStatementBuilder to generate Fill DDL by driving from HB and left-joining live.
- Wire Fill handling in DerivedTumblingPipeline.BuildDdlAndRegister to use the new builder.
- Allow pipeline to emit Hb/Fill/Prev1m (relax gating) beyond 1s where applicable.
- Update physical test note to reflect WhenEmpty DSL availability and pending physical generation.

Implications
- Fill-derived CREATE statements now consistently include EMIT CHANGES.
- Initial Fill DDL ensures contiguous buckets materialize by joining *_hb_<tf> with *_<tf>_live.
- When timeframe is 1m and prev_1m is available, projection uses COALESCE(l.col, p.Close) for Open/High/Low/Close-equivalent columns.

Next
- Incorporate prev_1m into Fill DDL (LEFT JOIN + COALESCE/CASE) to project previous-close fillers.
- Unskip WhenEmpty physical test and validate 1m→5m consistency via TimeBucket.
