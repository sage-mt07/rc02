# TimeBucket empty result handling

🗕 2025-08-24 JST
🧐 作業者: naruse

## 差分タイトル
TimeBucket set-level ToListAsync throws when no rows, bucket aggregates results

## 変更理由
Calling code previously checked for empty lists after querying each topic. The responsibility is moved into ToListAsync so callers get fail-fast behavior and TimeBucket can aggregate live and final results cleanly.

## 追加・修正内容
- `ITimeBucketSet.ToListAsync` throws `InvalidOperationException` when RocksDB range scan yields no rows
- `TimeBucket<T>.ToListAsync` catches per-topic empties, combines results, and rethrows when both live and final are empty
- Added unit test ensuring live topic is used when final topic has no rows

## 参考文書
- docs/chart.md
