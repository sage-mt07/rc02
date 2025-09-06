# diff: physical tests -> API tests (2025-09-06)

対象: physicalTests/OssSamples/
- BarDslExplainTests.cs
- BarScheduleAnchorTests.cs
- BarScheduleDataTests.cs
- BarScheduleExplainTests.cs
- BarWeekAnchorCompareTests.cs

変更概要:
- ksqlDB への実接続/EXPLAIN/INSERT ベースの物理テストを廃止し、DSL/API モデル検証と SQL 生成検証へ切り替え。
- フローを「OnModelCreating(IModelBuilder)でモデル定義 → ToQuery でクエリ定義 → CREATE SQL を生成（マテリアライズ） → アサート」に統一。
- docs/chart.md の使用例に合わせ、`KsqlQueryRoot` + `.TimeFrame()` + `.Tumbling()` + `.GroupBy()` + `.Select()` で OHLC を表現。
- モデル検証では `HasTumbling`, `Windows`, `WeekAnchor`, `BasedOnType` を確認。
- SQL 検証では `KsqlCreateStatementBuilder.Build()` の SELECT 句に `EARLIEST_BY_OFFSET / LATEST_BY_OFFSET / MIN / MAX` が含まれることを確認。

非対応/留意点:
- EMIT FINAL/GRACE 等の最終確定は DSL モデル外（ビルダー側）で扱うため、本変更ではモデルレベルの意図確認に留める。
- 物理的な件数確認や ksqlDB への HTTP 呼び出しは行わない。

理由:
- 物理テストがインフラ依存で不安定なため、設計仕様（chart.md）に沿った API テストへ移行。
