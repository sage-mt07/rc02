# 差分履歴: serialization_namespace_reduction

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
Serialize namespace を POCO ⇄ key/value 変換専用に整理

## 変更理由
- PM 指示に基づき独自 Avro 管理層を廃止し、Confluent 公式パッケージへ一本化した
- Query/Mapping 連携に必要な変換ヘルパーのみ残すため

## 追加・修正内容（反映先: oss_design_combined.md）
- `ConfluentSerializerFactory` `ISerializerFactory` `IDeserializerFactory` を追加
- `KeyValueConverter` を追加し `KeyExtractor`/`KeyMerger` をラップ
- `KafkaProducerManager` `KafkaConsumerManager` を ConfluentSerializerFactory 利用に改修
- 旧 `Serialization/Abstractions` `Serialization/Avro` 以下のクラスを全削除

## 削除された主なクラス
- AvroSerializationManager
- AvroSerializerFactory / AvroSerializer / AvroDeserializer
- AvroSchemaBuilder など Schema 管理クラス一式
- AvroEntityConfiguration 系ビルダー

## 利用例 (抜粋)
```csharp
var factory = new ConfluentSerializerFactory(schemaRegistryClient);
var keyType = KeyExtractor.DetermineKeyType(model);
var keySer = factory.CreateSerializer(keyType);
var valSer = factory.CreateSerializer<MyEntity>();
```

## 参考文書
- ~~`restructure_plan/serialization_namespace.md`~~ （計画完了によりファイル削除）

### 2025-07-13 Interfaces moved
- `ISerializerFactory` and `IDeserializerFactory` relocated to `Serialization.Abstractions`
- Updated consumer and producer managers to use new namespace

- Removed unit tests for deleted Avro components
### 2025-07-13 Cleanup follow-up
- Removed AvroEntityConfiguration integration from KsqlContext and helpers
- Deleted AvroSchemaInfoExtensions and related project references
- ConfluentSerializerFactory now returns sync-over-async serializers

### 2025-07-13 Test updates
- Revised consumer and producer manager tests for new constructor-based setup
- Removed obsolete caching tests relying on deleted managers
