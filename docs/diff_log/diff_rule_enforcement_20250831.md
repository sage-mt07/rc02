# Rule enforcement refinements (2025-08-31)

- dictionary lookups now fail when entries are missing, empty or duplicated, avoiding silent fallback
- public stream/table creation requires a partition key when emitting key SerDe
- stream–stream join error clarifies `.Within(seconds)` requirement (e.g. `Within(60)`)
