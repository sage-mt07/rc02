# <img src="./src/KsqlLinq-icon-128.png" alt="Kafka.Ksql.Linq Icon" width="78"   style="vertical-align: middle; margin-right: 8px;" /> Kafka.Ksql.Linq



Kafka.Ksql.Linq は C# から Kafka/ksqlDB のクエリを LINQ 風に記述できる DSL ライブラリです。
Entity Framework の経験がある開発者がストリーム処理ロジックを直感的に書けることを目指しています。

## 特徴
- LINQ ライクな DSL で Kafka/ksqlDB のクエリを構築
- Schema Registry の SpecificRecord と連携した Avro シリアライズ
- Window ベースの集約と Push Query 生成をサポート

## Quick Start
1. .NET 6 SDK をインストールし、リポジトリを取得して依存関係を復元します。
   ```bash
   git clone <repository-url>
   cd rc01
   dotnet restore
   ```
2. Kafka/ksqlDB/Schema Registry を起動します。
   ```bash
   docker-compose -f tools/docker-compose.kafka.yml up -d
   ```
3. サンプルを実行します。
   ```bash
   cd examples/hello-world
   dotnet run
   ```
   さらに詳しいサンプルや誤用例/推奨パターンは [docs/examples_reference.md](docs/examples_reference.md) を参照してください。

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
- [docs/examples_reference.md](docs/examples_reference.md)

### 🛠️ 上級開発者
- [docs/dev_guide.md](docs/dev_guide.md)
- [docs/namespaces/](docs/namespaces)

### 🏗️ アーキテクト・運用担当
- [docs/docs_advanced_rules.md](docs/docs_advanced_rules.md)
- [docs/docs_configuration_reference.md](docs/docs_configuration_reference.md)
- [docs/architecture_overview.md](docs/architecture_overview.md)
- [docs/test_guidelines.md](docs/test_guidelines.md)
- [docs/amagiprotocol/README.md](docs/amagiprotocol/README.md)
