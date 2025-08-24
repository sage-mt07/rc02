詩音への指示

目的:
テスト基盤のSETUP/TEARDOWN処理、クエリ仕様、スキーマ整合性の再点検を行い、物理テストの安定性を高めること。

要求事項:
1. **SETUP/TEARDOWN強化**
   - テスト開始前に必要なテーブル・ストリーム・トピック・スキーマが存在するか確認し、足りなければ `CREATE TABLE/STREAM` で作成する。
   - テスト終了後は必ず `DROP` と Schema Registry のサブジェクト削除を行う。

2. **スキーマ名の整合性**
   - 既存スキーマと `name`/`namespace` が一致しない場合、テスト前に `DELETE /subjects/{subject}` でサブジェクトをクリアすること。
   - 新旧スキーマが混在しないよう注意する。
   - 物理テスト実行時には `customers-value` サブジェクトも事前に削除する。REST API 例: `DELETE http://<schema-registry>/subjects/customers-value`。

3. **クエリ仕様の整理**
   - ksqlDB の Pull/Pull-Query 区別や `EMIT CHANGES` 必須など、サポート関数・型を一覧化し、非サポート構文を自動的に除外またはスキップするロジックを導入する。

4. **Kafka疎通・DLQ準備**
   - Kafka、ksqlDB、SchemaRegistry、DLQ topic の稼働確認をSETUPフェーズで実施。存在しない場合は自動生成し、疎通エラー時はテストを Skip してよい。

5. **実装例**
   - テストクラス初期化時に以下を参考実装すること。
```csharp
await ksqlDbClient.ExecuteStatementAsync("DROP TABLE IF EXISTS ORDERS_NULLABLE_KEY;");
await schemaRegistryClient.DeleteSubjectAsync("orders_nullable_key-value");
await schemaRegistryClient.DeleteSubjectAsync("orders_nullable_key-key");
await ksqlDbClient.ExecuteStatementAsync(@"\
  CREATE TABLE ORDERS_NULLABLE_KEY (...省略...);\
");
```
   - 型不一致（例: CASE文のTHENとELSEが異なる型など）はコード生成側で修正すること。サポートされていない関数（例: `UPPER`) は自動テストから除外する。

6. **その他**
   - ksqlDB/Kafka サービス起動直後は数秒待機もしくはリトライを入れる。
   - 互換性エラーや疎通エラーはテスト失敗ではなく "保留" 扱いとして構わない。
