# diff_stream_multi_key_20250906
- Stream DDL generation no longer groups multiple keys into a STRUCT.
- Each key column is marked with `KEY` individually.
- Added unit test `GenerateCreateStream_WithMultipleKeys_UsesColumnKeys`.
