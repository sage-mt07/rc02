# Key-Value Flow (Naruse View)

この文書は [shared/key_value_flow.md](../shared/key_value_flow.md) を参照し、実装担当の鳴瀬がクラス連携の観点から再整理したものです。

## 依存順

```
Query -> KsqlContext -> Messaging -> Serialization -> Kafka
```

## 責務分離

| コンポーネント | 主なクラス例 | 役割 |
|---------------|-------------|------|
| **Builder** | `KsqlContextBuilder` | DSL設定を集約し `KsqlContext` を生成 |
| **Pipeline** | `QueryBuilder` | LINQ式を解析して key/value を抽出 |
| **Context** | `KsqlContext` | Produce/Consume の統括と DI 初期化 |
| **Serializer** | `AvroSerializer` | key/value を Avro フォーマットへ変換 |
| **Messaging** | `KafkaProducer`, `KafkaConsumer` | トピック単位の送受信を担当 |

## LINQ式ベースの流れ

1. アプリケーションは `EntitySet<T>` で LINQ クエリを記述します。
2. `QueryBuilder` が式ツリーを解析し、`QuerySchema` を生成します。
3. `KsqlContext` の `ExtractKeyValue` (内部で `PocoMapper` を利用) により key/value が得られます。
4. `KsqlContextBuilder` が各種オプションをまとめ `KsqlContext` を構築します。
5. `KsqlContext` から `KafkaProducer` または `KafkaConsumer` を取得し、メッセージの送受信を実行します。
6. `AvroSerializer` がオブジェクトをシリアライズし、Kafka ブローカーへ配信します。

## PocoMapper API

```csharp
namespace Kafka.Ksql.Linq.Mapping;

public static class PocoMapper
{
    public static (object Key, TEntity Value) ToKeyValue<TEntity>(TEntity entity, QuerySchema schema) where TEntity : class;
    public static TEntity FromKeyValue<TEntity>(object? key, TEntity value, QuerySchema schema) where TEntity : class;
}
```

### Query → PocoMapper → KsqlContext サンプル

```csharp
var modelBuilder = new ModelBuilder();
modelBuilder.Entity<User>();
var model = modelBuilder.GetEntityModel<User>()!;


var schema = QueryAnalyzer.AnalyzeQuery<User, User>(q => q.Where(u => u.Id == 1)).Schema!;

var query = context.Set<User>().Where(u => u.Id == 1);
var entity = new User { Id = 1, Name = "Alice" };
var (key, value) = PocoMapper.ToKeyValue(entity, schema);
await context.AddAsync(entity, headers: new Dictionary<string, string> { ["is_dummy"] = "true" });
```

上記のように `Query` から生成したエンティティを `PocoMapper` で key/value に変換し、`KsqlContext` 経由で Kafka へ送信します。

## 運用フロー詳細(抜粋)
1. POCO定義・LINQ式生成
    - Query namespaceでkey/value候補を取得し、key未指定時はGuidを自動割当。
2. Mapping登録処理
    - KsqlContextがPOCOとkey/value情報をMappingに登録し、DLQ用POCOも合わせて設定。
3. KSQLクラス名生成
    - namespaceとクラス名から一意なschema名を生成し、登録時と一致させる。
4. スキーマ登録
    - schema registryへKSQL名でスキーマを登録。
5. インスタンス生成
    - POCOごとにMessaging/Serializationを初期化し、OnModelCreating直後に実施。

### ベストプラクティス
- `MappingManager` へ登録するモデルは `OnModelCreating` で一括定義する。
- `QueryBuilder` から返される KSQL 文はデバッグログで確認しておく。
- `KsqlContext` のライフサイクルは DI コンテナに任せ、使い回しを避ける。

## Mapping拡張の実装ポイント
sharedドキュメントの型情報管理フローを受け、実装担当として以下を意識します。
1. `PropertyMeta` 定義は Fluent API で決定し、クラス属性には依存しない。
2. `MappingManager` で自動生成される KeyType/ValueType を中心に型情報をやり取りする。
3. Messaging 層では `KafkaProducerManager` / `KafkaConsumerManager` が POCO と key/value の Avro 変換を担当し、Serializer/Deserializer をキャッシュして安定した処理を実現する。
