# examples 整理ステータス差分 20250912

対象: `examples/` 直下サンプルのビルド・動作可否を棚卸し。現状「ビルド不可 or 実行不可」のものを列挙し、主因を最小限でメモ。

判定基準:
- ビルド不可: `dotnet build` がエラー終了
- 実行不可: 重大な設定不整合で起動/接続が不可能（例: ポート不一致、欠落 csproj）

動作しない（要修正）一覧:
- examples/manual-commit
  - 実行不可: `csproj` 不在。README の `dotnet run --project .` は失敗
  - 設定不整合: `appsettings.json` の Schema Registry が `http://localhost:8085`（docker は 8081 公開）
- examples/configuration-mapping/ConfigurationMapping.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`, `AvroTimestamp` → `KsqlTopic`, `KsqlTimestamp` 等）。`using Kafka.Ksql.Linq.Core.Attributes;` の不足
- examples/error-handling/ErrorHandling.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`, `DecimalPrecision` → `KsqlTopic`, `KsqlDecimal`）
- examples/error-handling-dlq/ErrorHandlingDlq.csproj
  - ビルド不可: 同上（属性名/名前空間の不一致）
- examples/headers-meta/HeadersMeta.csproj
  - ビルド不可: 文字列リテラルのエスケープ崩れ（`\"cid\"` 等）により構文エラー
- examples/hello-world/HelloWorld.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`, `AvroTimestamp`）
- examples/query-filter/QueryFilter.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`）
- examples/retry-onerror/RetryOnError.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`）
- examples/schema-attributes/SchemaAttributes.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`, `KsqlKey`, `KsqlDecimal`, `AvroTimestamp` -> `KsqlTimestamp`）
- examples/table-cache-lookup/TableCacheLookup.csproj
  - ビルド不可: API 変更追従不足（`KsqlContext` 既定 ctor 廃止、`KsqlContextBuilder` 利用/Options 注入が必要）
- examples/view-toquery/ViewToQuery.csproj
  - ビルド不可: 属性名/名前空間の不一致（`Topic`）
- examples/daily-comparison/*（複数）
  - `DailyComparisonLib`: 名前空間/API の旧参照（`Kafka.Ksql.Linq.Core.Context` 等）
  - `ComparisonViewer`, `rollup-1m-5m-verify`: 上記ライブラリ依存によりビルド不可

ビルド通過（参考）:
- examples/basic-produce-consume
- examples/deduprates-producer
- examples/tumbling-live-consumer
- examples/whenempty-schedule
- examples/oss-bars-verify

修正の方向性（最小手当）:
- すべてのサンプルで `using Kafka.Ksql.Linq.Core.Attributes;` を追加
- 属性名を現行実装へ統一: `Topic`→`KsqlTopic`, `AvroTimestamp`→`KsqlTimestamp`, `DecimalPrecision`→`KsqlDecimal`, `Key`→`KsqlKey` 等
- `KsqlContext` の生成は `KsqlContextBuilder` + Options へ移行
- `manual-commit` は csproj 追加 + Schema Registry ポート 8081 に修正
- `headers-meta` はエスケープ崩れ箇所のリテラル修正

備考:
- 上記は 2025-09-12 時点の棚卸し。修正後は本ファイルを残し、新規 diff を作成して履歴保全する。

