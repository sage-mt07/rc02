# 2025-09-04 物理テスト失敗整理（詩音）

- 実行: `tools/run_physical_tests_per_test.sh`
- ベース出力: `Reportsx/physical/20250903-235641/`

再現手順（WSL推奨）
- 1) 事前pull: `wsl PREPULL=true /mnt/c/dev/rc02/tools/run_physical_tests_per_test.sh 'FullyQualifiedName~<pattern>'`
- 2) 失敗単体例:
  - Kafkaダウン送信: `... 'FullyQualifiedName=Kafka.Ksql.Linq.Tests.Integration.KafkaServiceDownTests.AddAsync_ShouldThrow_WhenKafkaIsDown'`
  - 管理系: `... 'FullyQualifiedName=Kafka.Ksql.Linq.Tests.Integration.KafkaAdminServiceIntegrationTests.EnsureTopic_CreatesWithConfiguredStructure'`

失敗一覧（15件、抜粋）
- Kafka耐障害: `KafkaServiceDownTests.*`, `BigBang_KafkaConnection_TolerantTests.*`
- 管理: `KafkaAdminServiceIntegrationTests.EnsureTopic_CreatesWithConfiguredStructure`（修正済み）
- サンプル/DSL: `DefaultAndBoundaryValueTests.*`, `ManualCommitIntegrationTests.ManualCommit_PersistsOffset`, `JoinIntegrationTests.TwoTableJoin_Query_ShouldBeValid`, `DlqIntegrationTests.ForEachAsync_OnErrorDlq_WritesToDlq`, `NoKeyPocoTests.SendAndReceive_NoKeyRecord`, `SchemaNameCaseSensitivityTests.LowercaseField_ShouldSucceed`

T1〜T5（優先）
1. Kafkaダウン時の例外サーフェス整合（ArgNull/NRE排除）
2. 既定値の適用保証（Admin/Producer/Consumer）
3. JOIN/トピック構造の前提整備（EnsureTopicとの整合）
4. Decimal/Boundary の丸め・精度定義とテスト期待の同期
5. KSQL生成の妥当/不当パターンの最小ケース化

ログの見所
- 各 `NNN_*/dotnet_test.log` 内の最上位例外と InnerException
- `compose_ps_after.out` で停止させたサービスの状態（Kafka down/ksqldb down/schema-registry down）

