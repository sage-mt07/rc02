# Examples Index

各サンプルは、先に依存起動（Kafka/ksqlDB）を実行してから `dotnet run` を行います。

共通の起動コマンド
- `docker-compose -f tools/docker-compose.kafka.yml up -d`

---

## Basic Produce & Consume
- 最小POCO + 送受信
- 実行: `dotnet run --project examples/basic-produce-consume`

## Schema & Attribute
- [KsqlKey], [KsqlDecimal], [AvroTimestamp] の典型例
- 実行: `dotnet run --project examples/schema-attributes`

## View Definition (ToQuery)
- From → Join → Where → Select の定義
- 実行: `dotnet run --project examples/view-toquery`

## WhenEmpty + Schedule (1m→5m)
- docs/chart.md の TimeFrame + Tumbling + WhenEmpty パターンを再現
- 機能: 1分足で空バケットを前バーCloseで補完（O=H=L=C）、その後5分へロールアップ
- 送信: 2分目のTickを意図的に欠損させ、WhenEmptyが動くことを検証（Tick一覧も出力）
- 実行: `dotnet run --project examples/whenempty-schedule`
- 期待: 1分足10本（2分目がフラット補完）、5分足2本（1分補完を反映）

---

# Advanced Samples

## LINQ Filter on Query Result
- `.Where(...) + .ForEachAsync(...)`
- 実行: `dotnet run --project examples/query-filter`

## Retry / OnError Handler
- 再試行やエラー時の独自処理
- 実行: `dotnet run --project examples/retry-onerror`

## Table Cache Lookup
- `.AsTable(useCache:true)` の使い方
- 実行: `dotnet run --project examples/table-cache-lookup`

## Headers & Meta
- ヘッダー付与と受信メタの利用
- 実行: `dotnet run --project examples/headers-meta`
