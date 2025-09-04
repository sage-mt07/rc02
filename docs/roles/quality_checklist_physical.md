# 物理テスト品質チェックリスト（鏡花）

- 前提確認
  - Docker compose: Kafka/ZooKeeper/Schema Registry/ksqlDB が up しヘルスOK
  - 事前の残骸（RocksDB一時ディレクトリ、ボリューム）をクリア
- 例外サーフェス基準（Kafkaダウン系）
  - AddAsync/ForeachAsync: `KafkaException` もしくは `ProduceException`、メッセージに `refused`/`serialization`/`Register schema` を含む
  - `ArgumentNullException`/`NullReferenceException` は不適切（キー無し/設定既定の扱いミス）
- 既定値適用
  - Admin/Producer/Consumer の主要設定は 0/未設定で不正にならない（未設定時はライブラリ既定にフォールバック）
- トピック作成
  - RF > broker 台数の場合は警告の上 RF=1 へフォールバック
  - Heartbeat/`rate_1m_*` の互換性警告
- レポート
  - `Reportsx/physical/<UTC>/` に `summary.csv`/`report.md`/各 `dotnet_test.log` が保存されている

