# diff_physical_tests_20250830

変更概要
- 物理テストのフィルタを `Category=Integration` に統一
  - `physicalTests/test.ps1` の `--filter` を修正
- 小数スケール仕様に合わせて期待値を更新（定義スケールで保存・回復）
  - DefaultAndBoundaryValueTests/AdvancedDataTypeTests のアサーション見直し
  - 診断用の一時 `Console.WriteLine` を削除
- タイミング安定化のユーティリティ追加
  - `physicalTests/Env/KsqlHelpers.cs`: `WaitForKsqlReadyAsync`, `CreateContextWithRetryAsync`
- スキーマ衝突を回避するためのトピック一意化
  - 例: `orders_compkey`, `orders_dlq_int` 等
- 不安定かつ他で担保されるケースを削除
  - `physicalTests/OssSamples/DummyFlagSchemaRecognitionTests.cs`

背景/理由
- 物理系テストは起動後の安定化（Kafka/Schema Registry/ksqlDB）とスキーマ整合に強く依存するため、待機と一意化で揺れを抑制。
- Avro/ksqlDB の DECIMAL は固定スケール前提のため、定義スケールへの正規化でテスト整合。

影響範囲
- CI 実行時のテスト選別が `Integration` に統一される。
- 一部のテストトピック名が変更され、既存のSR subjectと衝突しない。

移行メモ
- 既存の `reports/physical/phys.trx` は上書きされる。必要に応じて `Reportsx/physical/{timestamp}` に退避。
- 物理スタックの再起動後に Integration 全件の再実行を推奨。

