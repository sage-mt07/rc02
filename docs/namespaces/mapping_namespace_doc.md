# Kafka.Ksql.Linq.Mapping Namespace 責務定義書

## 概要
LINQ 解析結果や `EntityModel` から Avro 用 key/value 型を動的生成し、`MappingRegistry` に登録する namespace です。
生成された型情報と Avro スキーマは Messaging や Cache から利用されます。

## 主要コンポーネント
- **MappingRegistry**
  - `RegisterEntityModel` や `RegisterQueryModel` で `KeyValueTypeMapping` を生成
  - 生成した Avro スキーマと型を保持し提供
- **KeyValueTypeMapping**
  - 生成された key/value 型と `PropertyMeta` 情報のペアを保持
- **SpecificRecordGenerator**
  - Avro `ISpecificRecord` 実装を動的生成

## 責任境界
- ✅ POCO プロパティから Avro 用 key/value 型を生成
- ✅ Avro スキーマ文字列の保持
- ❌ Kafka 通信や Serializer 管理（Messaging が担当）
- ❌ EntityModel の構築（Core が担当）

