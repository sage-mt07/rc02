# Remove SyncHint and heartbeat sync

- Dropped `SyncHint` from derived entities and adapters.
- Removed `SyncHb1m` handling and prev/1m sync utilities from query builders.
- Updated role traits to only track window and emit behavior.
