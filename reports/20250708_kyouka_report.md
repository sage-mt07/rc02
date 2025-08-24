# Avroシリアライズ/デシリアライズ型不一致調査報告

## 調査背景
Avro によるメッセージ処理で、シリアライズ/デシリアライズ時に型不一致エラーが発生するとの報告を受けた。既存実装を確認し、原因および改善策を検討した。

## 現状実装の概要
- `AvroSerializerFactory` 内で値オブジェクトのシリアライズ/デシリアライズを実装している。
- 実装では Confluent の公式 Avro シリアライザを利用せず、`System.Text.Json` による JSON 変換を行っている。
- 例として `AvroValueSerializer` と `AvroValueDeserializer` の実装は以下の通り。

```csharp
// シリアライズ処理抜粋
public byte[] Serialize(object data, SerializationContext context)
{
    if (data is T typedData)
    {
        // Confluent の ISpecificRecord 要件を避けるため、JSON へ変換
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(typedData);
    }
    throw new InvalidOperationException($"Expected type {typeof(T).Name}");
}

// デシリアライズ処理抜粋
public object Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
{
    if (isNull || data.IsEmpty)
    {
        return Activator.CreateInstance<T>()!;
    }
    return System.Text.Json.JsonSerializer.Deserialize<T>(data)!
        ?? throw new InvalidOperationException("Deserialization returned null");
}
```
【引用元】`AvroSerializerFactory.cs`【F:src/Serialization/Avro/Core/AvroSerializerFactory.cs†L334-L375】

- 一方、スキーマ生成では `UnifiedSchemaGenerator` がプロパティ型を Avro 型へマッピングしている。decimal や DateTime などの特殊型は以下のように `bytes` や `long` へ変換される。

```csharp
// 型→Avro 型への変換例
if (underlyingType == typeof(decimal))
{
    var decimalAttr = property.GetCustomAttribute<DecimalPrecisionAttribute>();
    if (decimalAttr != null)
    {
        return new { type = "bytes", logicalType = "decimal", precision = decimalAttr.Precision, scale = decimalAttr.Scale };
    }
    return new { type = "bytes", logicalType = "decimal", precision = 18, scale = 4 };
}

if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
{
    var dateTimeAttr = property.GetCustomAttribute<DateTimeFormatAttribute>();
    if (dateTimeAttr?.Format == "date")
    {
        return new { type = "int", logicalType = "date" };
    }
    return new { type = "long", logicalType = "timestamp-millis" };
}
```
【引用元】`UnifiedSchemaGenerator.cs`【F:src/Serialization/Avro/Core/UnifiedSchemaGenerator.cs†L430-L475】

## 問題点
- スキーマでは decimal を `bytes`、DateTime を `long` など Avro のバイナリ表現として定義しているが、実際のメッセージは JSON 文字列として送出される。
- そのため、スキーマに従ってデシリアライズしようとすると型が一致せず `SerializationException` などが発生する。
- 特に Schema Registry から取得したスキーマでバリデーションを行うクライアントでは、バイト列と期待される箇所に JSON が格納されているため解釈できない。

## 改善案
1. **公式 Avro シリアライザの利用**  
   `AvroValueSerializer`／`AvroValueDeserializer` を Confluent.Kafka の `AvroSerializer<T>` / `AvroDeserializer<T>` へ置き換え、スキーマに則ったバイナリ形式でエンコードする。これにより特殊型のエンコードも一貫して行える。
2. **テストの追加**  
   各サポート型（decimal, DateTime, Guid など）について往復シリアライズテストを実施し、スキーマ定義とデータが一致することを確認する。
3. **スキーマ生成時の検証強化**  
   `UnifiedSchemaGenerator` で生成したスキーマと実際のシリアライズ方法の整合性を自動チェックする仕組みを導入する。

## まとめ
現在の実装では JSON ベースのシリアライザを用いているため、スキーマで定義された Avro 型と実際のデータ表現が一致していない。公式の Avro シリアライザを利用し、スキーマ通りのバイナリ形式でエンコードすることで型不一致問題を解消できると考えられる。

---

## 物理テストエラー調査 (2025-07-08 JST)

### 発生事象
`dotnet test` 実行時、`Schema not registered: orders-key` という例外でセットアップ処理が失敗した。

### 確認結果
Schema Registry に登録されていたサブジェクトは以下の通りで、`orders-key` が存在しなかった。
```
["customers-value","events-value","orders-value","orders_nullable-value","orders_nullable_key-value","source-value"]
```
`TestEnvironment.ValidateSchemaRegistrationAsync` では各トピックの `-key` サブジェクトも登録されている前提で検証しており、未登録の場合に例外が送出される。

### 原因
テーブル作成時に `KEY_FORMAT` を指定しておらず、ksqlDB の既定値 `KAFKA` が利用されたため、キーのスキーマが Schema Registry へ登録されなかった。

### 改善案
- `TestSchema.GenerateTableDdls` で `KEY_FORMAT='AVRO'` を指定し、キーもスキーマ登録されるよう修正する。
- もしくは `ValidateSchemaRegistrationAsync` を修正し、`-key` サブジェクトが存在しない場合は警告のみとする。

