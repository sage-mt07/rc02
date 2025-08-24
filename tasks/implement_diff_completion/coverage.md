# ✏️ カバレッジ進行状況

下表は `task/implement-diff.txt` の未実装または部分実装項目について、現在のカバレッジ状況を整理したものです。

| 機能カテゴリ | 機能 | 状況 | 対応タスク名 | 備考 |
| --- | --- | --- | --- | --- |
| Topics | Fluent APIによるトピック設定 | ✅ 実装済 | topic_fluent_api_extension | EntityModelBuilder, extension methods |
| Topics | パーティショニング戦略設定 | ✅ 実装済 | topic_fluent_api_extension | Partitioner option added |
| Topics | ISRの最小数設定 | ❌ 未実装 | topic_fluent_api_extension | |
| Streams | Window DSL機能 | ✅ 実装済 | window_dsl_feature | TumblingWindow等 |
| Streams | 購読モードの固定化制御 | ⏳ 部分実装 | subscription_mode_fixed | UseManualCommitの実行時切替未実装 |
| Tables | LATEST_BY_OFFSET / EARLIEST_BY_OFFSET | ✅ 実装済 | ksql_offset_aggregates | ProjectionBuilderで変換完了 |
| Tables | 複数ウィンドウ定義とアクセス | ✅ 実装済 | multi_window_access | |
| Tables | HasTopic()メソッド | ✅ 実装済 | has_topic_api_extension | EntityModelBuilder & tests |
| Tables | WindowStart / WindowEndプロパティ | ✅ 実装済 | window_start_end_support | ProjectionBuilder, WindowDDLExtensions |
| Query & Subscription | 手動コミット購読処理の型分岐 | ✅ 実装済 | manual_commit_extension | ForEachAsyncでIManualCommitMessageを返す |
| Query & Subscription | 購読処理の完全実装 | ✅ 実装済 | manual_commit_extension | Commit/NegativeAck呼び出し対応 |
| Query & Subscription | yield型ForEachAsyncでのtry-catch | ✅ 実装済 | foreach_trycatch_support | |
| Special Types | char型警告 | ✅ 実装済 | special_type_handling | ModelBuilderで警告 |
| Special Types | short型自動int変換 | ✅ 実装済 | special_type_handling | DDL/Avro変換対応 |
| Error Handling | チェーン可能なエラー処理DSL | ✅ 実装済 | chainable_error_dsl | EventSetErrorHandlingExtensionsで実装 |
| Error Handling | デシリアライズエラーポリシー | ✅ 実装済 | deserialization_error_policy | 失敗バイト列をDLQへ送信 |
| Error Handling | ModelBuilderでのDLQ設定 | ❌ 未実装 | dlq_configuration_support | グローバルDLQ固定 |
