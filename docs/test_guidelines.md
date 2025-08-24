# OSSテストガイドライン

このプロジェクトでは、ksqlDB の仕様に基づいたクエリのみをテスト対象とします。

## Pull クエリの制約

- `EMIT CHANGES` を含まない `SELECT` 文は Pull クエリとして扱われます。
- Pull クエリで `GROUP BY` を指定するとビルド時または実行時にエラーとなり、Push Query への切替を促すメッセージが表示されます。
- 集約が必要な場合は Push クエリ (`EMIT CHANGES` を付与) または事前集約済みテーブルを参照してください。

## 追加ルール

- Kafka メッセージ送信は `Chr.Avro.Confluent` による POCO 型自動スキーマ連携を基本とします。
- `GROUP BY` を含むテストは Push Query のみを対象とし、Pull Query では生成しません。
- `MIN` / `MAX` 集計は STREAM クエリでのみ使用し、テーブルでは扱いません。
- `WINDOW` 句は `GROUP BY` の直後に配置することを確認してください。
- `CASE` 式の `THEN` と `ELSE` 型が異なる場合はテストを失敗させます。

テスト自動生成処理では、上記に違反するクエリを検出した場合、自動的にスキップして実行しません。

### 物理テストの区分

- **Connectivity** : Kafka ブローカーや Schema Registry の疎通を確認。
- **KsqlSyntax** : 生成された KSQL 文が ksqlDB で受理されるかを検証。
- **OssSamples** : サンプルコードを用いた API 挙動の統合テスト。

## 物理テスト・統合テスト手順

### 事前準備
1. `.NET 6 SDK` がインストールされていることを確認します。
2. リポジトリを取得後 `dotnet restore` を実行します。
3. Kafka/ksqlDB/Schema Registry を起動します。
   ```bash
   docker-compose -f tools/docker-compose.kafka.yml up -d
   ```

### サンプル実行
最小構成のメッセージ送受信は `examples/hello-world` で試せます。
```bash
cd examples/hello-world
dotnet run
```

### 統合テスト
環境が起動している状態で次のコマンドを実行します。
```bash
dotnet test physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj --filter Category=Integration
```
- Kafka や Schema Registry を再起動した場合でも、テスト内で必要な Avro スキーマが自動登録されます。
- 失敗やスキップの原因は `logs/` フォルダや `docker logs` で確認できます。

`tools/quickstart_integration.sh` ではセットアップからテスト実行までを一括で行えます。
