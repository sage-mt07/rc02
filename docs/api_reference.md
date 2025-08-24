# API Reference (Draft)

この文書は `Kafka.Ksql.Linq` OSS における公開 DSL/API と主要コンポーネントの概要を整理したものです。今後の設計ドキュメントや実装コード、テストコードへの参照基盤として利用します。

## 既定値の参照

- 既定値一覧は [docs_configuration_reference.md](docs_configuration_reference.md) を参照してください。

## コアインタフェース

| インタフェース | 説明 | 主な実装 |
|----------------|------|----------|
| `IKsqlContext` | KSQL 操作の起点となるコンテキスト。エンティティ登録やクエリ実行を司ります。 | `KsqlContext`, `KafkaContextCore` |
| `IEventSet<T>` | ストリーム/テーブル共通のクエリ操作を定義するコアインタフェース。 | `EventSet<T>` |
| `IErrorHandlingChain<T>` | エラー処理を段階的に構築するためのチェーン。 | `ErrorHandlingChain<T>` |
| `IDlqClient` | DLQ からレコードを非同期で読み取るクライアント。 | `DlqClient` |
| `ITableCache<T>` | キー前方一致によるキャッシュ参照を提供します。 | `TableCache<T>` |

## 属性 (Attribute) 定義

| 属性                       | 役割                           | 実装状態 |
|----------------------------|--------------------------------|---------|
| `MaxLengthAttribute`       | 文字列長制限                   | ✅      |
| `ScheduleRangeAttribute`   | 取引開始・終了をまとめて指定する属性 | 🚧 |

`WithDeadLetterQueue()` は過去の設計で提案されましたが、現在は `OnError(ErrorAction.DLQ)` に置き換えられています。

## Fluent API 一覧

| メソッド | 説明 | 主なパラメータ |
|----------|------|----------------|
| `Entity<T>(readOnly = false, writeOnly = false)` | エンティティ登録とアクセスモード指定 | `readOnly`: 消費のみで書き込み禁止<br>`writeOnly`: 書き込みのみで消費禁止 |
| `.AsStream()` | ストリーム型として登録 | - |
| `.AsTable(topicName = null, useCache = true)` | テーブル型として登録 | `topicName`: `[KsqlTopic]` を上書き<br>`useCache`: `ITableCache` を有効化 |
| `.ToQuery(build)` | 新DSLでのクエリ定義 | `build`: `From`/`Join`/`Where`/`Select` を連鎖させるラムダ |

### Fluent API ガイドライン

POCO モデルを Fluent API で構成する際の設計指針と移行フローをまとめます。属性ベースから移行後は `IEntityBuilder<T>` を用いて宣言的に設定を行います。

1. `[KsqlTopic]` や `[KsqlKey]` などの属性でトピック名やキー順序を宣言します。
2. Fluent API はクエリ構築やモード指定のみを担い、スキーマ情報は属性に集約します。
3. エンティティ登録時は `readonly` `writeonly` `readwrite` の 3 種類で役割を指定し、未指定時は `readwrite` とみなします。

#### 推奨記述例
```csharp
[KsqlTopic("orders")]
public class Order
{
    ...
    builder.Entity<Order>(writeOnly: true);
}
```
`[KsqlTopic]` や `[KsqlDecimal]` 属性でトピックや精度を宣言できます。

#### 既存 POCO → Fluent API 移行フロー
1. POCO へ `[KsqlTopic]` と `[KsqlKey]` を付与してスキーマ情報を記述する。
2. `OnModelCreating` では `Entity<T>()` の登録のみ行い、その他は属性に委ねる。
3. テストを実行してキー順序やトピック設定が正しいか確認する。旧属性に関する詳細は `docs/namespaces/core_namespace_doc.md` を参照してください。

#### MappingManager との連携
`MappingManager` を利用して key/value を抽出する例です。詳細は `docs/architecture/key_value_flow.md` を参照してください。
```csharp
var ctx = new MyKsqlContext(options);
var mapping = ctx.MappingManager;
var entity = new Order { Id = 1, Amount = 100 };
var (key, value) = mapping.ExtractKeyValue(entity);
await ctx.AddAsync(entity, headers: new Dictionary<string, string> { ["is_dummy"] = "true" });
```
##### ベストプラクティス
- エンティティ登録は `OnModelCreating` 内で一括定義する。
- `MappingManager` を毎回 `new` しない。DI コンテナで共有し、モデル登録漏れを防ぐ。

##### 追加検討事項
- `[KsqlTopic]` 属性で指定できない詳細設定の扱いを検討中。
- MappingManager のキャッシュ戦略（スレッドセーフな実装範囲）を確定する必要あり。

##### サンプル実装での気づき
- `AddSampleModels` 拡張で `MappingManager` への登録をまとめると漏れ防止になる。
- 複合キーは `Dictionary<string, object>` として抽出されるため、型安全ラッパーの検討余地あり。
- `Dictionary<string,string>` 型のプロパティは Avro の `map` (`{"type": "map", "values": "string"}`) として扱われる。
  - キー・値ともに文字列のみサポート。その他の型やネスト構造は未対応。
  - `null` は許容されないため、プロパティは空ディクショナリで初期化する。
- `decimal` プロパティは `DecimalPrecisionConfig` で設定された `precision`/`scale` を持つ Avro `bytes` (`logicalType: decimal`)として生成される。
- 複数エンティティを登録するヘルパーがあると `OnModelCreating` の記述量を抑えられる。

##### AddAsync 統一に伴うポイント
- メッセージ送信 API は `AddAsync` に一本化した。旧 `ProduceAsync` は廃止予定。
- LINQ クエリ解析から `MappingManager.ExtractKeyValue()` を経由し `AddAsync` を呼び出す流れをサンプル化。
- 詳細なコード例は [architecture/query_to_addasync_sample.md](architecture/query_to_addasync_sample.md) を参照。

## LINQ 風 DSL 一覧

| DSL メソッド | 説明 | 戻り値型 | 対象レイヤ | 主なパラメータ | 実装状態 |
|--------------|------|----------|------------|----------------|---------|
| `.Where(predicate)` | 条件フィルタ | `IEventSet<T>` | Stream/Table | `predicate`: 各レコードに適用する bool 式 | ✅ |
| `.GroupBy(keySelector)` | グループ化および集約 | `IEventSet<IGrouping<TKey, T>>` | Stream/Table | `keySelector`: グループキー抽出 | ✅ |
| `.OnError(action)` | エラー処理方針指定 | `EventSet<T>` | Stream | `action`: `Skip`/`Retry`/`DLQ` | ✅ |
| `.WithRetry(count)` | リトライ設定 | `EventSet<T>` | Stream | `count`: 最大リトライ回数 | ✅ |
| `.StartErrorHandling()` | エラーチェーン開始 | `IErrorHandlingChain<T>` | Stream | - | ✅ |
| `.Limit(count)` | **保持件数制限。Table型(Set<T>)でのみ利用可。OnModelCreatingで定義し、超過分は自動削除される。** | `IEntitySet<T>` | Table | `count`: 保存上限件数 | ✅ |

- `ToList`/`ToListAsync` は Pull Query として実行されます【F:src/Query/Pipeline/DMLQueryGenerator.cs†L27-L34】。
- `ForEachAsync(..., autoCommit: false)` を指定した場合、`Commit(entity)` を呼び出して手動コミットを行います。
- `autoCommit` 既定値は `true` であり、`ConsumerConfig.EnableAutoCommit` により処理成功時に自動コミットされます。
- `appsettings.json` で `EnableAutoCommit` が指定されている場合、その値が `autoCommit` 引数より優先されます。
- Table cache は `ITableCache<T>.ToListAsync(filter, timeout)` により取得し、`filter` は NUL (\u0000) 区切りの前方一致プレフィックスとして解釈されます。
- `OnError(ErrorAction.DLQ)` を指定すると DLQ トピックへ送信されます【F:docs/old/defaults.md†L52-L52】。
- `ctx.Set<DlqEnvelope>()` を指定すると DLQ ストリームを取得できます。DLQ は無限ログのため `Take()` や `ToListAsync()` などの一括取得 API は利用できず、`ForEachAsync()` のみサポートします。また DLQ ストリームで `.OnError(ErrorAction.DLQ)` を指定すると無限ループになるため禁止されています。
- 本DLQはメタ情報のみを保持し、元メッセージ本文は含まれません。詳細は [namespaces/messaging_namespace_doc.md](namespaces/messaging_namespace_doc.md) を参照してください。
- Messaging クラス自体は DLQ 送信処理を持たず、`ErrorOccurred`/`DeserializationError`/`ProduceError` などのイベントを通じて外部で DLQ 送信を行います。
- DLQ の内容確認は `ctx.Dlq.ReadAsync(...)` を利用してください。
- `Set<T>().Limit(n)` は Table 型の保持件数を制限する DSL です。`OnModelCreating` 内で指定し、超過分のレコードは自動削除されます。Stream 型や実行時クエリでは利用できません。
- `RemoveAsync(key)` は値 `null` のトムストーンを送り、KTable やキャッシュから該当キーのデータを削除します。

### ToQuery チェーン

| メソッド | 説明 | 主なパラメータ | 注意点 |
|----------|------|----------------|--------|
| `.From<T>()` | ビュー定義の開始 | - | - |
| `.Join<T2>(condition)` | 2テーブルまでの内部結合 | `condition`: (left, right) => bool | 後続に `.Where` 必須 |
| `.Where(predicate)` | 結合条件やフィルタ | `predicate`: bool 条件式 | `.Join` 使用時は必須 |
| `.Select(selector)` | 投影 | `selector`: 出力構造 | 呼び出し順序は `From`→`Join?`→`Where`→`Select` |

これらは `modelBuilder.Entity<T>().ToQuery(q => q ... )` 内で使用し、`KsqlQueryModel` に変換されます。

## ToQuery DSL

`ToQuery` は View 定義専用の Fluent API です。`From<T>()` を起点に `Join<T2>()`、`Where(...)`、`Select(...)` を順に呼び出してチェーンを構築します。JOIN は2テーブルまでサポートされており、結合条件は `Join` メソッド内で指定します。必要に応じて `Where` で追加のフィルタリングを行えます。呼び出し順序が守られない場合や未サポートの JOIN 数は構文検証で例外となります。

```csharp
modelBuilder.Entity<OrderSummary>().ToQuery(q => q
    .From<Order>()
    .Join<Customer>((o, c) => o.CustomerId == c.Id)
    .Where((o, c) => c.IsActive)
    .Select((o, c) => new OrderSummary { OrderId = o.Id, CustomerName = c.Name }));
```

`.ToQuery(...)` で得られた `KsqlQueryModel` は `KsqlContext` 初期化時に `CREATE STREAM/TABLE AS SELECT` 文として利用されます。

## エラーハンドリング

| API / Enum                 | 説明                           | 実装状態 |
|----------------------------|--------------------------------|---------|
| `ErrorAction` (Skip/Retry/DLQ) | エラー時の基本アクション    | ✅      |
| `ErrorHandlingPolicy`      | リトライ回数やカスタムハンドラ設定を保持 | ✅ |
| `ErrorHandlingExtensions`  | `.OnError()` `.WithRetryWhen()` 等の拡張 | ✅ |
| `DlqProducer` / `DlqEnvelope` | DLQ 送信処理               | ✅      |
| `DlqOptions`    | DLQ トピックの保持期間等を指定 | ✅      |

### DLQ Read API（Avro固定）

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

- `FromBeginning=true` で earliest へシーク
- `CommitOnRead=true` で 1 件ごとにコミット
- Avro ワイヤフォーマットから RawText を可読化

**既知の制約**

- PayloadFormat は常に `"avro"`
- 再投函は非対応（読むだけ）

## 構成オプションとビルダー

| API                        | 説明                             | 実装状態 |
|----------------------------|----------------------------------|---------|
| `KsqlDslOptions`           | DLQ 設定や ValidationMode など DSL 全体の構成を保持 | ✅ |
| `ModelBuilder`             | POCO から `EntityModel` を構築するビルダー | ✅ |
| `KafkaAdminService`        | DLQ トピック作成などの管理操作  | ✅      |
| `AvroOperationRetrySettings`| Avro操作ごとのリトライ設定     | ✅      |
| `AvroRetryPolicy`          | リトライ回数や遅延などの詳細ポリシー | ✅  |

`KsqlDslOptions.DlqTopicName` は既定で `"dead-letter-queue"` です【F:src/Core/Dlq/DlqProducer.cs†L248-L256】。

## 状態監視・内部機構

| API                         | 説明                             | 実装状態 |
|-----------------------------|----------------------------------|---------|
| `ReadyStateMonitor`         | トピック同期状態の監視           | ✅      |
| `CacheBinding`         | Kafka トピックと Cache の双方向バインディング | ✅ |
| `SchemaRegistryClient`      | スキーマ管理クライアント        | ✅      |
| `ResilientAvroSerializerManager` | Avro操作のリトライ管理     | ✅      |

## 各 API の備考

- `IEventSet<T>.WithRetry()` の実装例は `EventSet.cs` にあります【F:src/EventSet.cs†L238-L258】。
- `OnError` の拡張は `EventSetErrorHandlingExtensions.cs` で提供されています【F:src/EventSetErrorHandlingExtensions.cs†L8-L20】。
- 手動コミットの利用例は [manual_commit.md](old/manual_commit.md) を参照してください。
- `StartErrorHandling()` → `.Map()` → `.WithRetry()` の流れで細かいエラー処理を構築できます。
- `AvroOperationRetrySettings` で SchemaRegistry 操作のリトライ方針を制御します【F:src/Configuration/Options/AvroOperationRetrySettings.cs†L8-L33】。
