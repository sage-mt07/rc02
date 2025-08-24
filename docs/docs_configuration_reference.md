## ⚙️ Kafka.Ksql.Linq appsettings.json 構成仕様

Kafka.Ksql.Linq では、`appsettings.json` を通じて柔軟なDSL設定が可能です。以下はその構成要素と意味です。
標準的なデフォルト値は `examples/configuration/appsettings.json` にまとめられています。

---

### 1 📐 基本構造

```json
{
  "KsqlDsl": {
    "ValidationMode": "Strict|Relaxed",
    "Common": { /* 共通設定 */ },
    "Topics": { /* トピック別設定 */ },
    "SchemaRegistry": { /* スキーマレジストリ設定 */ },
    "TableCache": [ /* エンティティ／キャッシュ設定 */ ],
    "DlqTopicName": "dead-letter-queue",
    "DlqOptions": { /* DLQ トピック設定 */ },
    "DeserializationErrorPolicy": "Skip|Retry|DLQ",
    "ReadFromFinalTopicByDefault": false,
    "DecimalPrecision": 38,
    "DecimalScale": 9
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
| `GroupId` | コンシューマーグループID |
| `AutoOffsetReset` | `Latest` or `Earliest` |
| `EnableAutoCommit` | 自動コミット可否。`ForEachAsync` の `autoCommit` より優先 |
| `AutoCommitIntervalMs` | 自動コミット間隔(ms) |
| `SessionTimeoutMs` | セッションタイムアウト(ms) |
| `HeartbeatIntervalMs` | ハートビート送信間隔(ms) |
| `MaxPollIntervalMs` | 最大ポーリング間隔(ms) |
| `MaxPollRecords` | 最大ポーリングレコード数 |
| `FetchMinBytes` | フェッチ最小バイト数 |
| `FetchMaxWaitMs` | フェッチ最大待機(ms) |
| `FetchMaxBytes` | フェッチ最大バイト数 |
| `PartitionAssignmentStrategy` | パーティション割当戦略 |
| `IsolationLevel` | アイソレーションレベル |
| `AdditionalProperties` | 追加Consumer設定 |

---

### 🧬 1.3 SchemaRegistry（スキーマレジストリ設定）

```json
"SchemaRegistry": {
  "Url": "http://localhost:8081",
  "MaxCachedSchemas": 1000,
  "RequestTimeoutMs": 30000,
  "BasicAuthUserInfo": "user:pass",
  "BasicAuthCredentialsSource": "UserInfo",
  "AutoRegisterSchemas": true,
  "LatestCacheTtlSecs": 300,
  "SslCaLocation": "/path/ca.pem",
  "SslKeystoreLocation": "/path/keystore.p12",
  "SslKeystorePassword": "secret",
  "SslKeyPassword": "secret",
  "AdditionalProperties": {}
}
```

| 項目 | 説明 |
|------|------|
| `Url` | スキーマレジストリURL |
| `MaxCachedSchemas` | クライアント側でキャッシュする最大スキーマ数 |
| `RequestTimeoutMs` | リクエストタイムアウト(ms) |
| `BasicAuthUserInfo` | Basic認証用クレデンシャル（形式：`user:pass`） |
| `BasicAuthCredentialsSource` | `UserInfo` or `SaslInherit` |
| `AutoRegisterSchemas` | スキーマを自動登録するかどうか |
| `LatestCacheTtlSecs` | 最新スキーマキャッシュTTL(sec) |
| `SslCaLocation` | CA証明書パス |
| `SslKeystoreLocation` | キーストア(PKCS#12)パス |
| `SslKeystorePassword` | キーストアパスワード |
| `SslKeyPassword` | 秘密鍵パスワード |
| `AdditionalProperties` | 追加設定 |

---

### 🏪 1.4 TableCache（テーブルキャッシュ設定）

```json
"TableCache": [
  {
    "Entity": "OrderEntity",
    "SourceTopic": "orders",
    "EnableCache": true,
    "Windows": [5, 15, 60],
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
| `Windows` | タンブリングウィンドウサイズ（整数：分単位） |
| `StoreName` | キャッシュ名（省略時はトピック名を基に自動生成） |
| `BaseDirectory` | RocksDBディレクトリのルートパス |

---

### 🛡️ 1.5 ValidationMode

| 値 | 説明 |
|-----|------|
| `Strict` | 起動時に全構成値を検証。欠落や不整合があるとエラー（推奨） |
| `Relaxed` | 柔軟に読み込む。テスト・一時構成に使用可 |

```json
"ValidationMode": "Strict"
```

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

### ⚙️ 1.7 その他オプション

| 項目 | 説明 |
|------|------|
| `DeserializationErrorPolicy` | `Skip` / `Retry` / `DLQ` のエラーハンドリング方針 |
| `ReadFromFinalTopicByDefault` | Finalトピックを既定で参照するか |
| `DecimalPrecision` | decimal型のprecisionを一括設定 |
| `DecimalScale` | decimal型のscaleを一括設定 |

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
| Windowサイズ               | `.Window(new[] { 5, 15, 60 })`       | `KsqlDsl:Entities[].Windows`              | DSL/設定どちらでも指定可（整合性が必要） |

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
        .WithAutoOffsetReset(AutoOffsetReset.Earliest)
        .Window(new[] { 5 });

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
      "order-counts-consumer": {
        "GroupId": "order-counts-consumer",
        "AutoOffsetReset": "latest"
      }
    }
  },
  "KsqlDsl": {
    "SchemaRegistry": {
      "Url": "http://localhost:8081"
    },
    "KsqlDbUrl": "http://localhost:8088",
    "Topics": {
        "orders": {
          "NumPartitions": 3,
          "ReplicationFactor": 1
        },
        "order_counts": {
          "NumPartitions": 1,
          "ReplicationFactor": 1,
          "CleanupPolicy": "compact"
        }
      }
    },
    "TableCache": [
      {
        "Type": "Order",
        "Windows": [5]
      }
    ],
    "DlqTopicName": "dead-letter-queue",
    "DlqOptions": {
      "RetentionMs": 5000,
      "NumPartitions": 1,
      "ReplicationFactor": 1
    }
  }
}
```



### 💡 備考：複数GroupId構成と整合性

- Kafkaでは1つのトピックに対して複数のコンシューマグループを定義可能です。
- 本DSLでは `Entity<T>` ごとに `GroupId` を指定することで、複数のグループ単位の並列処理や責務分離を実現できます。
- それに対応して `appsettings.json` では `Kafka:Consumers.<name>` として複数グループの構成を記述します。
- 各DSL定義と `Consumers` のキー名（例: `orders-consumer`）が一致している必要があります。

これにより、「DSLで定義するグループID = 運用時の構成名」として論理的に整合した設計が実現されます。




