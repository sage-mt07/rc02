# appsettings.json とクラス構造のトレース

`appsettings.json` に記載された設定が、どの Options プロパティへマッピングされ、どのクラス（namespace）で利用されるのかを整理した一覧です。設計意図が不明瞭な項目はその旨を記載しています。

| appsettingsキー | Optionsプロパティ | 利用クラス (ネームスペース) | 設計意図・備考 |
|--------------------|------------------------|--------------------------------|----------------------------------|
| `KsqlDsl:Common:BootstrapServers` | `CommonSection.BootstrapServers` | `KafkaProducerManager`, `KafkaConsumerManager`, `KafkaAdminService` | Kafka クラスタの接続元を指定 |
| `KsqlDsl:Common:ClientId` | `CommonSection.ClientId` | 同上 | Kafka クライアントID |
| `KsqlDsl:Common:SecurityProtocol` など | `CommonSection.*` | 同上、セキュリティ設定で利用 | TLS/SASL などの設定 |
| `KsqlDsl:Topics.<name>.Producer.*` | `TopicSection.Producer.*` | `KafkaProducerManager` | Producer 個別設定 |
| `KsqlDsl:Topics.<name>.Consumer.*` | `TopicSection.Consumer.*` | `KafkaConsumerManager` | Consumer 個別設定 |
| `KsqlDsl:Topics.<name>.Creation.*` | `TopicSection.Creation.*` | `KafkaAdminService` | トピック自動生成の設定 |
| `KsqlDsl:SchemaRegistry:*` | `SchemaRegistrySection.*` | `KafkaProducerManager`, `KafkaConsumerManager`, `KsqlContext` | Avro スキーマの登録、取得設定 |
| `KsqlDsl:TableCache` | `KsqlDslOptions.Entities` | `KsqlContextCacheExtensions`, `TableCacheRegistry` | エンティティごとのテーブルキャッシュ設定 |
| `KsqlDsl:DlqTopicName` | `KsqlDslOptions.DlqTopicName` | `KafkaAdminService`, `DlqProducer` | DLQ 用トピック名 |
| `KsqlDsl:DlqOptions.*` | `DlqOptions.*` | `KafkaAdminService` | DLQ トピック生成方針 |
| `KsqlDsl:DeserializationErrorPolicy` | `KsqlDslOptions.DeserializationErrorPolicy` | `KafkaConsumerManager` | デシリアライズ失敗時のポリシー |
| `KsqlDsl:DecimalPrecision`, `KsqlDsl:DecimalScale` | `KsqlDslOptions.DecimalPrecision`, `DecimalScale` | `KsqlContext` (通して `DecimalPrecisionConfig` に伝播), Query Builders | decimal 型の精度/スケール一括設定 |
| `KsqlDsl:ReadFromFinalTopicByDefault` | `KsqlDslOptions.ReadFromFinalTopicByDefault` | 利用部位不明 | 意図不明、設計規定が不定 |
| `Kafka:Consumers.<name>.*` | `KafkaSubscriptionOptions` へ取り込み | `KafkaConsumerManager` | 複数グループ設定に対応 |

待続けているオプションや設定については、docs/docs_configuration_reference.md に詳細がまとめられています。
