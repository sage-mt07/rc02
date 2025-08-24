# API Reference

`Kafka.Ksql.Linq` の公開 DSL/API を使用頻度の高い順にまとめたリファレンスです。

## 目次
- [属性 (Attributes)](#%E5%B1%9E%E6%80%A7-attributes)
- [Fluent API](#fluent-api)
  - [ToQuery チェーン](#toquery-%E3%83%81%E3%82%A7%E3%83%BC%E3%83%B3)
- [LINQ 風 DSL](#linq-%E9%A2%A8-dsl)
- [エラーハンドリング](#%E3%82%A8%E3%83%A9%E3%83%BC%E3%83%8F%E3%83%B3%E3%83%89%E3%83%AA%E3%83%B3%E3%82%B0)
- [コアインタフェース](#%E3%82%B3%E3%82%A2%E3%82%A4%E3%83%B3%E3%82%BF%E3%83%95%E3%82%A7%E3%83%BC%E3%82%B9)
- [構成オプションとビルダー](#%E6%A7%8B%E6%88%90%E3%82%AA%E3%83%97%E3%82%B7%E3%83%A7%E3%83%B3%E3%81%A8%E3%83%93%E3%83%AB%E3%83%80%E3%83%BC)
- [既定値の参照](#%E6%97%A2%E5%AE%9A%E5%80%A4%E3%81%AE%E5%8F%82%E7%85%A7)

## 属性 (Attributes)

POCO モデルで最も利用される属性です。

| 属性 | 役割 | 主な引数 | 備考 |
|------|------|----------|------|
| `KsqlTopicAttribute` | トピック名・パーティション・レプリケーション指定 | `name`, `PartitionCount`, `ReplicationFactor` | モデルのトピックを構成し、設定より優先されます。 |
| `KsqlKeyAttribute` | 複合キー順序の定義 | `order` | 小さい順にキーが並びます。 |
| `KsqlDecimalAttribute` | `decimal` 型の精度とスケール指定 | `precision`, `scale` | Avro の `bytes` (logicalType: decimal) として生成。 |
| `KsqlDatetimeFormatAttribute` | 日時文字列の解析フォーマット | `format` | `DateTime.ParseExact` 互換。 |
| `KsqlTimestampAttribute` | イベントタイムとなるプロパティを指定 | - | `ROWTIME` の代替に利用。 |
| `KsqlStreamAttribute` | クラスを Stream として扱う | - | 明示指定が必要な場合のみ使用。 |
| `KsqlIgnoreAttribute` | スキーマから除外 | - | 無視するプロパティに付与。 |
| `MaxLengthAttribute` | 文字列長制限 | `length` | 超過時は例外。 |
| `ScheduleRangeAttribute` | 開始・終了プロパティ名の対指定 | `openPropertyName`, `closePropertyName` | 取引時間帯などの範囲指定。 |

`WithDeadLetterQueue()` は `OnError(ErrorAction.DLQ)` に置き換えられました。

## Fluent API

エンティティの登録やクエリ構築を行うための API です。

| メソッド | 説明 | 主なパラメータ |
|----------|------|----------------|
| `Entity<T>(readOnly = false, writeOnly = false)` | エンティティを登録しアクセスモードを指定 | `readOnly`, `writeOnly` |
| `.AsStream()` | ストリームとして登録 | - |
| `.AsTable(topicName = null, useCache = true)` | テーブルとして登録 | `topicName`, `useCache` |
| `.ToQuery(build)` | 新 DSL でビュー定義 | `build`: `From`/`Join`/`Where`/`Select` を連鎖 |

### Fluent API ガイドライン

1. `[KsqlTopic]` や `[KsqlKey]` などの属性でスキーマ情報を宣言。
2. Fluent API はクエリ構築やモード指定に限定し、スキーマ設定は属性へ集約。
3. エンティティ登録時は `readOnly`/`writeOnly`/`readwrite` の 3 種類。未指定は `readwrite`。

#### 推奨記述例

```csharp
[KsqlTopic("orders")]
public class Order
{
    ...
    builder.Entity<Order>(writeOnly: true);
}
```

#### 既存 POCO → Fluent API 移行フロー

1. POCO へ `[KsqlTopic]` と `[KsqlKey]` を付与。
2. `OnModelCreating` では `Entity<T>()` の登録のみ行う。
3. テストでキー順序やトピック設定を確認。

### ToQuery チェーン

View 定義専用の Fluent API です。

| メソッド | 説明 | 主なパラメータ | 注意点 |
|----------|------|----------------|--------|
| `.From<T>()` | ビュー定義の開始 | - | - |
| `.Join<T2>(condition)` | 2 テーブルまでの内部結合 | `(left, right) => bool` | 後続に `.Where` 必須 |
| `.Where(predicate)` | 結合条件やフィルタ | `predicate`: bool 条件式 | `.Join` 使用時は必須 |
| `.Select(selector)` | 投影 | `selector`: 出力構造 | 呼び出し順序は `From`→`Join?`→`Where`→`Select` |

```csharp
modelBuilder.Entity<OrderSummary>().ToQuery(q => q
    .From<Order>()
    .Join<Customer>((o, c) => o.CustomerId == c.Id)
    .Where((o, c) => c.IsActive)
    .Select((o, c) => new OrderSummary { OrderId = o.Id, CustomerName = c.Name }));
```

`.ToQuery(...)` で得られた `KsqlQueryModel` は `CREATE STREAM/TABLE AS SELECT` 文として利用されます。

## LINQ 風 DSL

ストリーム/テーブル共通のクエリ操作を提供します。

| DSL メソッド | 説明 | 戻り値型 | 対象レイヤ | 主なパラメータ | 実装状態 |
|--------------|------|----------|------------|----------------|---------|
| `.Where(predicate)` | 条件フィルタ | `IEventSet<T>` | Stream/Table | `predicate`: bool 式 | ✅ |
| `.GroupBy(keySelector)` | グループ化と集約 | `IEventSet<IGrouping<TKey,T>>` | Stream/Table | `keySelector` | ✅ |
| `.OnError(action)` | エラー処理方針 | `EventSet<T>` | Stream | `Skip`/`Retry`/`DLQ` | ✅ |
| `.WithRetry(count)` | リトライ設定 | `EventSet<T>` | Stream | `count`: 最大回数 | ✅ |
| `.StartErrorHandling()` | エラーチェーン開始 | `IErrorHandlingChain<T>` | Stream | - | ✅ |
| `.Limit(count)` | **保持件数制限** | `IEntitySet<T>` | Table | `count`: 上限件数 | ✅ |

- `ToList`/`ToListAsync` は Pull Query として実行されます。
- `ForEachAsync(..., autoCommit: false)` では `Commit(entity)` による手動コミットが必要です。
- `autoCommit` 既定値は `true` で、`ConsumerConfig.EnableAutoCommit` により自動コミットされます。
- `ctx.Set<DlqEnvelope>()` で DLQ ストリーム取得。`Take()` や `ToListAsync()` は利用不可。

## エラーハンドリング

| API / Enum | 説明 | 実装状態 |
|------------|------|---------|
| `ErrorAction` (`Skip`/`Retry`/`DLQ`) | 基本アクション | ✅ |
| `ErrorHandlingPolicy` | リトライ回数やカスタムハンドラ設定 | ✅ |
| `ErrorHandlingExtensions` | `.OnError()` `.WithRetryWhen()` など | ✅ |
| `DlqProducer` / `DlqEnvelope` | DLQ 送信処理 | ✅ |
| `DlqOptions` | DLQ トピックの保持期間等 | ✅ |

### DLQ Read API（Avro 固定）

```csharp
public interface IKsqlContext
{
    IDlqClient Dlq { get; }
}

public interface IDlqClient
{
    IAsyncEnumerable<DlqRecord> ReadAsync(
        DlqReadOptions? options = null,
        CancellationToken ct = default);
}
```

**使い方サンプル**

```csharp
await foreach (var rec in ctx.Dlq.ReadAsync())
{
    Console.WriteLine(rec.RawText);
}
```

**仕様**

- `FromBeginning=true` で earliest へシーク。
- `CommitOnRead=true` で 1 件ごとにコミット。
- Avro ワイヤフォーマットから RawText を可読化。

**既知の制約**

- `PayloadFormat` は常に `"avro"`。
- 再投函は非対応（読むだけ）。

## コアインタフェース

| インタフェース | 説明 | 主な実装 |
|----------------|------|----------|
| `IKsqlContext` | KSQL 操作の起点となるコンテキスト。エンティティ登録やクエリ実行を司る。 | `KsqlContext`, `KafkaContextCore` |
| `IEventSet<T>` | ストリーム/テーブル共通のクエリ操作を定義。 | `EventSet<T>` |
| `IErrorHandlingChain<T>` | エラー処理を段階的に構築するチェーン。 | `ErrorHandlingChain<T>` |
| `IDlqClient` | DLQ からレコードを非同期で読み取るクライアント。 | `DlqClient` |
| `ITableCache<T>` | キー前方一致によるキャッシュ参照を提供。 | `TableCache<T>` |

## 構成オプションとビルダー

| API | 説明 | 実装状態 |
|-----|------|---------|
| `KsqlDslOptions` | DLQ 設定や ValidationMode など DSL 全体の構成を保持 | ✅ |
| `ModelBuilder` | POCO から `EntityModel` を構築するビルダー | ✅ |
| `KafkaAdminService` | DLQ トピック作成などの管理操作 | ✅ |
| `AvroOperationRetrySettings` | Avro 操作ごとのリトライ設定 | ✅ |
| `AvroRetryPolicy` | リトライ回数や遅延などのポリシー | ✅ |

`KsqlDslOptions.DlqTopicName` は既定で `"dead-letter-queue"` です。

## 既定値の参照

- 既定値一覧は [docs_configuration_reference.md](docs_configuration_reference.md) を参照してください。
