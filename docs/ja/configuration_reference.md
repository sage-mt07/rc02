## ⚙️ Kafka.Ksql.Linq appsettings.json 構成仕様

Kafka.Ksql.Linq では、`appsettings.json` を通じて柔軟なDSL設定が可能です。以下はその構成要素と意味です。
標準的なデフォルト値は `examples/configuration/appsettings.json` にまとめられています。

---

### 1 📐 基本構造

```json
{
  "KsqlDsl": {
    
    "Common": { /* 共通設定 */ },
    "Topics": { /* トピック別設定 */ },
    "SchemaRegistry": { /* スキーマレジストリ設定 */ },
    "TableCache": [ /* エンティティ／キャッシュ設定 */ ],
    "DlqTopicName": "dead-letter-queue",
    "DlqOptions": { /* DLQ トピック設定 */ },
    "DeserializationErrorPolicy": "Skip|Retry|DLQ",
    "ReadFromFinalTopicByDefault": false
  }
}
```

---

### 🧱 1.1 Common（共通Kafka設定）

| 項目 | 説明 |
|------|------|
| `BootstrapServers` | Kafkaブローカーの接続先 |
| `ClientId` | 接続クライアント識別子 |
| `RequestTimeoutMs` | Kafka操作タイムアウト（ms） |
| `MetadataMaxAgeMs` | メタデータの最大有効期間（ms） |
| `SecurityProtocol` | `Plaintext` / `SaslPlaintext` など |
| `SaslMechanism` | 認証方式（例：`Plain`, `ScramSha256`） |
| `SaslUsername`, `SaslPassword` | SASL認証情報 |
| `SslCaLocation` | CA証明書ファイルパス |
| `SslCertificateLocation` | クライアント証明書ファイルパス |
| `SslKeyLocation` | 秘密鍵ファイルパス |
| `SslKeyPassword` | 秘密鍵パスワード |
| `AdditionalProperties` | 追加Kafka設定（key-value） |

```json
"Common": {
  "BootstrapServers": "localhost:9092",
  "ClientId": "ksql-dsl-client",
  "RequestTimeoutMs": 30000,
  "MetadataMaxAgeMs": 300000,
  "SecurityProtocol": "Plaintext",
  "SaslMechanism": "Plain",
  "SaslUsername": "user",
  "SaslPassword": "pass",
  "SslCaLocation": "/path/ca.pem",
  "SslCertificateLocation": "/path/cert.pem",
  "SslKeyLocation": "/path/key.pem",
  "SslKeyPassword": "secret",
  "AdditionalProperties": {}
}
```

---

### 📦 1.2 Topics（トピックごとの詳細設定）

Producer の設定は `Kafka.Ksql.Linq.Configuration.Messaging.ProducerSection`、
Consumer の設定は `ConsumerSection` クラスにそれぞれマッピングされます。
アプリ設定ファイルの項目名とクラスプロパティが 1 対 1 で対応するため、
カスタム設定を追加する際はこれらのクラスを拡張してください。

```json
"Topics": {
  "my-topic": {
    "Producer": {
      "Acks": "All",
      "CompressionType": "Snappy",
      "EnableIdempotence": true,
      "MaxInFlightRequestsPerConnection": 1,
      "LingerMs": 5,
      "BatchSize": 16384,
      "DeliveryTimeoutMs": 120000,
      "RetryBackoffMs": 100,
      "Retries": 2147483647,
      "BufferMemory": 33554432,
      "Partitioner": null
    },
    "Consumer": {
      "GroupId": "my-group",
      "AutoOffsetReset": "Latest",
      "EnableAutoCommit": true,
      "AutoCommitIntervalMs": 5000,
      "SessionTimeoutMs": 30000,
      "HeartbeatIntervalMs": 3000,
      "MaxPollIntervalMs": 300000,
      "MaxPollRecords": 500,
      "FetchMinBytes": 1,
      "FetchMaxWaitMs": 500,
      "FetchMaxBytes": 52428800,
      "PartitionAssignmentStrategy": null,
      "IsolationLevel": "ReadUncommitted"
    },
    "Creation": {
      "NumPartitions": 1,
      "ReplicationFactor": 1,
      "Configs": {},
      "EnableAutoCreation": false
    }
  }
}
```

| Producer設定 | 説明 |
|------------------|------|
| `Acks` | 書き込み応答の強度設定（例：`All`, `1`） |
| `CompressionType` | 圧縮方式（`Snappy`, `Gzip`, `Lz4`など） |
| `EnableIdempotence` | 冪等性設定（重複防止） |
| `MaxInFlightRequestsPerConnection` | 同時送信要求上限 |
| `LingerMs` | バッチ送信待機時間（ms） |
| `BatchSize` | バッチ書き込み単位（byte） |
| `DeliveryTimeoutMs` | 配信タイムアウト（ms） |
| `RetryBackoffMs` | リトライ待機時間（ms） |
| `Retries` | 最大リトライ回数 |
| `BufferMemory` | 送信バッファサイズ（byte） |
| `Partitioner` | パーティショナー指定 |
| `AdditionalProperties` | 追加Producer設定 |

| Consumer設定 | 説明 |
|------------------|------|
| `GroupId` | コンシューマグループID |
| `AutoOffsetReset` | 既読位置制御 |
| `EnableAutoCommit` | 自動コミット |
| `AutoCommitIntervalMs` | 自動コミット間隔 |
| `SessionTimeoutMs` | セッションタイムアウト |
| `HeartbeatIntervalMs` | ハートビート間隔 |
| `MaxPollIntervalMs` | 最大ポーリング間隔 |
| `MaxPollRecords` | 1回の最大取得件数 |
| `FetchMinBytes` | 最小フェッチバイト数 |
| `FetchMaxWaitMs` | フェッチ待機最大時間 |
| `FetchMaxBytes` | 最大フェッチバイト数 |
| `PartitionAssignmentStrategy` | パーティション割当戦略 |
| `IsolationLevel` | 読み取り隔離レベル |

---

#### 🆕 動的トピックの設定

実行時に生成されるトピック（例: `rate_1m_pair` や `rate_hb_1m` など）は、基底エンティティに付与された `[Topic]` 属性の `PartitionCount` と `ReplicationFactor` を継承します。`appsettings.json` の `Topics` セクションに完全な名前でエントリが存在する場合は、その設定が属性値より優先されます。

```json
"Topics": {
  "rate_1m": {
    "Creation": {
      "NumPartitions": 2,
      "Configs": { "retention.ms": "60000" }
    }
  },
  "rate_hb_1m": {
    "Creation": {
      "NumPartitions": 3,
      "Configs": { "retention.ms": "120000" }
    }
  }
}
```

上記例では、`rate_hb_1m` は `rate_1m` の属性値を継承しますが、エントリがあるため設定が上書きされます。

---

### 🏪 1.4 Entities（Table cache settings）

```json
"Entities": [
  {
    "Entity": "OrderEntity",
    "SourceTopic": "orders",
    "EnableCache": true,
    "StoreName": "orders_store",
    "BaseDirectory": "/var/lib/ksql_cache"
  }
]
```

| 項目 | 説明 |
|------|------|
| `Entity` | 対象POCOクラス名 |
| `SourceTopic` | 入力元となるKafkaトピック名 |
| `EnableCache` | キャッシュ有効化（bool） |
| `StoreName` | キャッシュ名（省略時はトピック名を基に自動生成） |
| `BaseDirectory` | RocksDBディレクトリのルートパス |

---

### 🛡️ 1.5 Validation

- Validation mode is always Strict. The configuration key has been removed.

---

### 💌 1.6 DLQ 設定

```json
"DlqTopicName": "dead-letter-queue",
"DlqOptions": {
  "RetentionMs": 5000,
  "NumPartitions": 1,
  "ReplicationFactor": 1,
  "EnableAutoCreation": true,
  "AdditionalConfigs": {
    "cleanup.policy": "delete"
  }
}
```

未指定の場合、`DlqTopicName` は `dead-letter-queue` が使用されます。

| 項目 | 説明 |
|------|------|
| `DlqTopicName` | DLQ用トピック名 |
| `RetentionMs` | メッセージ保持時間(ms) |
| `NumPartitions` | パーティション数 |
| `ReplicationFactor` | レプリケーション係数 |
| `EnableAutoCreation` | 自動作成を行うか |
| `AdditionalConfigs` | 追加トピック設定 |

---

### 🧩 DSL記述とappsettingsの対応関係

| Kafka設定項目             | DSLでの指定                          | appsettings.jsonキー                         | 補足説明 |
|----------------------------|--------------------------------------|---------------------------------------------|--------|
| Bootstrap Servers          | なし                                 | `Kafka:BootstrapServers`                   | Kafka接続先クラスタ |
| Schema Registry URL       | なし                                 | `KsqlDsl:SchemaRegistry:Url`              | POCOスキーマ自動登録時に使用 |
| ksqlDB URL                | なし                                 | `KsqlDsl:KsqlDbUrl`                       | ksqlDB RESTエンドポイント |
| Auto Offset Reset | `.WithAutoOffsetReset(...)` | `Kafka:Consumers.<name>.AutoOffsetReset` | トピックごとの既読位置制御（複数可） | 通常は `earliest` or `latest` |
| GroupId | `.WithGroupId(...)` | `Kafka:Consumers.<name>.GroupId` | コンシューマグループID（複数可） | コンシューマグループID |
| トピック名                 | `[KsqlTopic("orders")]`             | `KsqlDsl:Topics.orders` で上書き可         | 属性またはFluent APIで指定 |
| パーティション数           | `[KsqlTopic("orders", PartitionCount = 12)]` | `KsqlDsl:Topics.orders.NumPartitions` 等    | DSLと設定の併用可能 |
| Replication Factor        | なし（構成ファイルで指定）          | `KsqlDsl:Topics.orders.ReplicationFactor`  | Kafkaクラスタ構成に依存 |
| DLQ構成                    | `.OnError(ErrorAction.DLQ)`          | `KsqlDsl:DlqTopicName`, `DlqOptions` | DLQの有効化、保持期間指定など |

---

### 📦 2. 実装例との対応（MyKsqlContext & Order & OrderCount）

```csharp
public class Order
{ 
    public string ProductId { get; set; }
    public decimal Amount { get; set; }
}

public class MyKsqlContext : KsqlContext
{
    protected override void OnModelCreating(KsqlModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .WithGroupId("orders-consumer")
        .WithAutoOffsetReset(AutoOffsetReset.Earliest);

    modelBuilder.Entity<OrderCount>()
        .WithGroupId("order-counts-consumer")
        .WithAutoOffsetReset(AutoOffsetReset.Latest)
        .UseFinalTopic();
});
    }
}
```

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Consumers": {
      "orders-consumer": {
        "GroupId": "orders-consumer",
        "AutoOffsetReset": "earliest"
      },
