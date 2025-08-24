# 実装カバレッジ状況

`task/implement-diff.txt` の未対応項目を基準に、現在の実装状況と担当タスクを一覧化する。

| 機能 | 状況 | 対応タスク名 | 備考 |
|---|---|---|---|
| OnError → Map → Retry | ✅ 実装済 | - | `EventSetErrorHandlingExtensions.cs` で確認済 |
| LATEST_BY_OFFSET / EARLIEST_BY_OFFSET | ✅ 実装済 | ksql_offset_aggregates | ProjectionBuilder, WindowAggregatedEntitySet 対応 |
| DLQ設定（ModelBuilder） | ⏳ 部分実装 | dlq_configuration_support | `TopicAttribute` 定義はある |
| HasTopic API | ✅ 実装済 | has_topic_api_extension | EntityBuilderTopicExtensions|
| ManualCommit切替 | ✅ 実装済 | manual_commit_extension | ForEachAsync型分岐対応 |
| char/shortサポート | ✅ 実装済 | special_type_handling | 警告出力とint変換対応 |
