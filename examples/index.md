# Examples Index（統合版）

このフォルダ配下のサンプルを、目的別に素早く辿れるよう整理しました。まずは前提を満たしてから、関心のあるカテゴリへ進んでください。

## 共通前提（最初に一度だけ）
- .NET 8 SDK をインストール
- ローカルの Kafka + Schema Registry + ksqlDB を起動
  - `docker-compose -f tools/docker-compose.kafka.yml up -d`

## 実行方法（共通）
- 例: `basic-produce-consume`
  - `cd examples/basic-produce-consume`
  - `dotnet run`
- 例で必要な `appsettings.json` は各フォルダ内に同梱（または README 参照）

---

## Basics（最初に触る）
- `basic-produce-consume`：Producer/Consumer の基本。`BasicMessage` を送信し `ForEachAsync` で受信
- `hello-world`：最小構成で POCO 定義→送信→待機→受信（All-in-`Program.cs`）

ワンライナー実行（basics/README.md 準拠）
```
dotnet run --project examples/basic-produce-consume
```

## Configuration（設定・属性）
- `configuration`：appsettings.json と Builder 設定の最小構成（接続/Topic/Consumer/Producer）
- `configuration-mapping`：環境別のログ設定（Development/Production）切替と構成例
- `schema-attributes`：`[KsqlKey]` / `[KsqlDecimal]` / `[KsqlTimestamp]` の使い方
- `headers-meta`：メッセージヘッダとメタ情報の取り扱い

## Query Basics（LINQ→KSQL 基本）
- `query-basics`：LINQ→KSQL の基本形（View/ToQuery の基礎と導入）
- `query-filter`：`.Where(...)` によるフィルタ
- `view-toquery`：View/ToQuery の基礎
- `table-cache-lookup`：`[KsqlTable]` とローカルキャッシュ参照

## Windowing（時間窓・集計｜統合）
- `windowing`：TUMBLING/HOPPING/SESSION の基礎に加え、ライブ集計（Push）と 1分→5分ロールアップを集約
  - 統合対象: `examples/tumbling-live-consumer` / `examples/rollup-1m-5m-verify`
- `whenempty-schedule`：WhenEmpty スケジュールの挙動（DSLでの利用時は Select に WindowStart() を1回含めること）

## Error Handling（運用・再処理）
- `error-handling`：OnError/Retry の基本（リトライ戦略の導入）
- `error-handling-dlq`：不正メッセージを DLQ に退避（`.OnError(ErrorAction.DLQ)`）
- `manual-commit`：手動コミット（autoCommit: false）での確定制御
- `retry-onerror`：再試行（Retry）パターン

## Advanced（検証・応用）
- `daily-comparison`：日次集計（レート取り込み→1/5/60分→日次までの集計検証）
- `oss-bars-verify`：Bar 系の OSS 検証
- `deduprates-producer`：重複排除レートの投入

---

## 参考ドキュメント（クリックで開く）
- OnModelCreating サンプル集：`docs/onmodelcreating_samples.md`
- 関数/型対応表：`docs/ksql-function-type-mapping.md`
- SQLServer→ksqlDB ガイド：`docs/sqlserver-to-kafka-guide.md`
