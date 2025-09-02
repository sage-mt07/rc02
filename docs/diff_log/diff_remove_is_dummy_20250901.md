# Remove dummy message handling
- Dropped automatic dummy record materialization and `is_dummy` header usage.
- `EventSet.ForEachAsync` no longer filters out messages with `is_dummy=true`.
- Deleted priming helper APIs that produced dummy records.
