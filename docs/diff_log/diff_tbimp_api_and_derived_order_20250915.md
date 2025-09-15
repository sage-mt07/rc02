 - Physical test TimeBucketImportTumblingTests: switched to dedicated topics and fixed ksqlDB API payloads; added push fallback and DESCRIBE wait.
   - Input topic: ticks_tbimp. Output topic: bar_tbimp.
   - Live tables: bar_tbimp_1m_live / bar_tbimp_5m_live.
   - /query and /query-stream now send body with "sql" and "properties" (ksqlDB v0.26+). /ksql retains "ksql" + "streamsProperties".
   - Pull failure falls back to push via /query-stream with EMIT CHANGES LIMIT 10.
   - Table readiness checks DESCRIBE via /ksql until both live tables are available.
   - Fixes 400 responses caused by incorrect payload keys.

 - DerivedTumblingPipeline ordering and DDL:
   - Create 1s hub STREAM before 1s TABLE so downstream DDL can reference the hub immediately.
   - Removed IF NOT EXISTS from hub STREAM DDL to match UT expectations.
   - Keeps DDL emission deterministic for WhenEmpty path (hb/fill/prev ordering unchanged).

 Files:
 - physicalTests/OssSamples/TimeBucketImportTumblingTests.cs
 - src/Query/Analysis/DerivationPlanner.cs
 - src/Query/Analysis/DerivedTumblingPipeline.cs

 Notes:
 - No runtime behavior change for table semantics; only REST payload compatibility and DDL ordering are adjusted.
