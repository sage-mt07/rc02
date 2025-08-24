# Key-Value Flow Architecture (POCO ↔ Kafka)

🗕 2025年7月20日（JST）
🧐 作成者: くすのき

このドキュメントでは、POCO と LINQ クエリから生成した key/value を Kafka へ送信する流れと、受信したデータを POCO へ戻す流れをまとめています。各レイヤーの責務を把握することで、設計の指針を明確にできます。

---

## 2. 全体構造図（双方向）

[Query] ⇄ [KsqlContext] ⇄ [MappingRegistry] ⇄ [Messaging] ⇄ [Kafka]


## 3. Produce Flow（POCO → Kafka）

[Query/IEntitySet<T>]
↓ LINQ式, POCO
[KsqlContext/MappingRegistry]
↓ T → key, value
[Messaging/KafkaProducerManager.SendAsync()]
↓ Avro (key, value)
[Kafka]
→ Topic送信


### 🧱 責務一覧

| レイヤー     | クラス名             | 主な責務                                  |
|--------------|----------------------|-------------------------------------------|
| Query        | EntitySet<T>         | LINQ式とPOCOを提供                         |
| KsqlContext  | KsqlContext          | MappingRegistry への登録と連携           |
| Mapping      | MappingRegistry      | POCO ⇔ key/value 変換を管理              |
| Messaging    | KafkaProducerManager | メッセージ送信、Avro 変換                |
| Kafka        | Kafka Broker         | メッセージ配信                            |

---

## 4. Consume Flow（Kafka → POCO）

[Kafka]
↓ メッセージ受信
[Serialization/AvroDeserializer]
↓ key, value（byte[] → object）
[Messaging/KafkaConsumerManager]
↓ key, value
[MappingRegistry/KeyValueTypeMapping]
↓ POCO再構成
[Application/Callback]
→ アプリケーションロジックへ渡す



### 🧱 責務一覧

| レイヤー     | クラス名               | 主な責務                                     |
|--------------|------------------------|----------------------------------------------|
| Kafka        | Kafka Broker           | メッセージ受信                                |
| Messaging    | KafkaConsumerManager   | Avro 変換済みメッセージ取得                 |
| Mapping      | KeyValueTypeMapping    | Avro から POCO への復元                     |
| Application  | Consumer Handler       | アプリロジックへの通知・後処理              |

---

## 5. 注意点

- 全体のKey定義はLINQ式で統一（POCOの属性依存を排除）。
- key/valueのAvro変換はConfluent公式に完全依存。
- `KafkaConsumerManager` は Avro から復元した型の安全性を保持。
- 各構成はDIにより初期化、KsqlContextが統括。


## 6. サンプルコード

```csharp
var services = new ServiceCollection();
services.AddKsqlContext<MyKsqlContext>();
var provider = services.BuildServiceProvider();
var ctx = provider.GetRequiredService<MyKsqlContext>();

var user = new User { Id = 1, Name = "Alice" };
await ctx.Set<User>().AddAsync(user);
```
