# ksql_offset_aggregates
- Added DSL extension methods for LATEST_BY_OFFSET and EARLIEST_BY_OFFSET.
- Extended ProjectionBuilder to translate new aggregate functions.
- Extended WindowAggregatedEntitySet aggregation visitor to output LATEST_BY_OFFSET and EARLIEST_BY_OFFSET.
- Created unit tests verifying KSQL translation.
- Updated coverage docs to mark this feature as completed.
