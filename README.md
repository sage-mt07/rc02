# <img src="./LinqKsql-logo.png?raw=true" alt="Kafka.Ksql.Linq Icon" height="32" style="vertical-align:-5px;"/><span>&nbsp;  </span> Kafka.Ksql.Linq


Kafka.Ksql.Linq は C# から Kafka/ksqlDB のクエリを LINQ 風に記述できる DSL ライブラリです。
Entity Framework の経験がある開発者がストリーム処理ロジックを直感的に書けることを目指しています。

## 特徴
- LINQライクな DSL 構文で Kafka/ksqlDB クエリを定義
- Avro + Schema Registry を前提とした型安全なシリアライズ
- Window/集約処理・Push/Pull クエリ対応
- DLQ / Retry / Commit を含む高度なエラーハンドリング

## Quick Start
```
git clone <repository-url>
cd rc01
dotnet restore

docker-compose -f tools/docker-compose.kafka.yml up -d

cd examples/hello-world
dotnet run
```
詳細サンプルは[docs/examples_reference.md](docs/examples_reference.md)を参照


## 📖 公式ドキュメントセット
- [Getting Started](docs/getting-started.md)
- [API Reference](docs/api_reference.md)
- [Configuration Reference](docs/docs_configuration_reference.md)
- [Advanced Rules](docs/docs_advanced_rules.md)

## 📚 ドキュメント構成ガイド
### 🧑‍🔧 現場担当者
- [docs/getting-started.md](docs/getting-started.md)
- [docs/troubleshooting.md](docs/troubleshooting.md)
- [docs/physical_test_minimum.md](docs/physical_test_minimum.md)

### 🧑‍🏫 初級〜中級者
- [docs/sqlserver-to-kafka-guide.md](docs/sqlserver-to-kafka-guide.md)
- 

### 🛠️ 上級開発者
- [docs/dev_guide.md](docs/dev_guide.md)
- [docs/namespaces/](docs/namespaces)

### 🏗️ アーキテクト・運用担当
- [docs/docs_advanced_rules.md](docs/docs_advanced_rules.md)
- [docs/docs_configuration_reference.md](docs/docs_configuration_reference.md)
- [docs/architecture_overview.md](docs/architecture_overview.md)
- [docs/test_guidelines.md](docs/test_guidelines.md)
- [docs/amagiprotocol/README.md](docs/amagiprotocol/README.md)
