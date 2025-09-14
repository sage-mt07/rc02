# physicalTests インフラ修正 (2025-09-14)

目的: 物理テストで Kafka 到達不可 (127.0.0.1:39092) が多発していたため、ホスト↔コンテナ間のリスナー不整合を解消。

変更点
- physicalTests/docker-compose.yaml
  - Kafka をデュアルリスナー構成に変更。
    - KAFKA_LISTENERS: `PLAINTEXT://0.0.0.0:29092, PLAINTEXT_HOST://0.0.0.0:39092`
    - KAFKA_ADVERTISED_LISTENERS: `PLAINTEXT://kafka:29092, PLAINTEXT_HOST://127.0.0.1:39092`
    - ポート公開を `39092:39092` に変更。
- physicalTests/up.ps1
  - TCP 待機ポートを 9092→39092 に修正。
  - 変数名の衝突と補間の不具合を修正。

影響
- ホスト側テストが `127.0.0.1:39092` で安定接続可能に。
- Connectivity/Admin/Schema Registry 系の短時間テストが全て成功することを確認。

確認ログ
- `reports/physical/physical_KafkaConnectivityTests.trx` (pass)
- `reports/physical/physical_KafkaAdminServiceIntegrationTests.trx` (pass)
- `reports/physical/physical_PortConnectivityTests.trx` (pass)
- `reports/physical/physical_SchemaRegistryResetTests.trx` (pass)

備考
- 先行の失敗結果は `reports/physical/physical.trx` に残置。
