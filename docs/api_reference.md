# API Reference

このページを読み終えると、送受信とビュー定義を試せます。

## よく使う流れ

### 1. コンテキストを作る（設定から始める）
- 設定を読み込み、ビルダーへ渡します。
- スキーマレジストリの URL を指定します。
- ログを有効化し、生成クエリを確認します。

```csharp
var configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json").Build();

var ctx = KsqlContextBuilder.Create()
  .UseConfiguration(configuration)
  .UseSchemaRegistry(configuration["KsqlDsl:SchemaRegistry:Url"]!)
  .EnableLogging(LoggerFactory.Create(b => b.AddConsole()))
  .BuildContext<MyAppContext>();
```

- 要約: まず ctx を作り、以後の操作はここから始めます。

### 2. エンティティを登録する（使う型を決める）
- トピック名を `[KsqlTopic]` で指定します。
- 時刻は `[KsqlTimestamp]` を付けます。
- `OnModelCreating` で `Entity<T>()` を登録します。

```csharp
[KsqlTopic("basic-produce-consume")]
public class BasicMessage
{
  public int Id { get; set; }
  [KsqlTimestamp] public DateTime CreatedAt { get; set; }
  public string Text { get; set; } = string.Empty;
}

protected override void OnModelCreating(IModelBuilder b)
  => b.Entity<BasicMessage>();
```

- 要約: 登録後は `ctx.Set<BasicMessage>()` が使えます。

### 3. 送って、受け取って、確かめる
- 送信は `AddAsync` を呼びます。
- 少し待ってから `ForEachAsync` で購読します。
- 期待するメッセージを標準出力で確認します。

```csharp
await ctx.Set<BasicMessage>().AddAsync(new BasicMessage
{
  Id = Random.Shared.Next(),
  CreatedAt = DateTime.UtcNow,
  Text = "Basic Flow"
});

await Task.Delay(500);
await ctx.Set<BasicMessage>().ForEachAsync(m =>
{
  Console.WriteLine($"Consumed: {m.Text}");
  return Task.CompletedTask;
});
```

- 期待結果: `Consumed: Basic Flow` が表示されます。
- 要約: 送受信は Set<T>() の2呼び出しで完結します。

### 4. ビューを定義する（ToQuery）
- 永続ビューは `ToQuery(...)` で宣言します。
- `From/Join/Where/Select` を順に組みます。
- 一時的な絞り込みは LINQ を併用します。

```csharp
modelBuilder.Entity<OrderSummary>().ToQuery(q => q
  .From<Order>()
  .Join<Customer>((o, c) => o.CustomerId == c.Id)
  .Where((o, c) => c.IsActive)
  .Select((o, c) => new OrderSummary { OrderId = o.Id, CustomerName = c.Name }));

await ctx.Set<OrderSummary>()
  .Where(x => x.CustomerName.StartsWith("A"))
  .ForEachAsync(x => { /* consume */ return Task.CompletedTask; });
```

- 要約: 定義は ToQuery、臨時の絞り込みは LINQ です。

### 5. 失敗を拾う（DLQ で追う）
- 失敗したレコードは DLQ に送られます。
- `ctx.Dlq.ReadAsync()` で内容を確認します。
- 必要なら修正して再投入します。

```csharp
await foreach (var rec in ctx.Dlq.ReadAsync())
{
  Console.WriteLine(rec.RawText);
}
```

- 要約: 異常は DLQ を巡回すれば必ず見つかります。

---

## 主要アノテーションとAPI
- [KsqlTopic]: エンティティをトピックに結びます。
- [KsqlTimestamp]: イベント時刻を Avro 互換で扱います。
- [KsqlTable]: Table として扱うことを示す（Stream は既定）。
- Entity<T>(): 型を登録して操作を可能にします。
- ToQuery(...), Where(...): ビューと絞り込みを定義します。
- OnError(...), ctx.Dlq.ReadAsync(): エラー処理と調査を担います。

---

## API署名（要点）

- `IEventSet<T> AddAsync(T entity, CancellationToken ct=default)`
  - 送信する。戻り値は `ValueTask`。失敗時は `KafkaException` を投げます。
- `IEventSet<T> ForEachAsync(Func<T,Task> handler, CancellationToken ct=default)`
  - 購読する。ハンドラで処理する。キャンセルで停止します。
- `ModelBuilder.Entity<T>(bool readOnly=false, bool writeOnly=false)`
  - 型を登録する。既定は両方 false（読み書き可）。
- `QueryBuilder ToQuery(Func<IQueryBuilder,IQueryBuilder> build)`
  - ビューを宣言する。生成時に KSQL を適用します。

要約: 送受信・登録・定義の要所だけを短く覚えます。

## 設定スキーマ（最小）

```json
{
  "KsqlDsl": {
    "Common": { "BootstrapServers": "localhost:9092", "ClientId": "app" },
    "SchemaRegistry": { "Url": "http://localhost:8085" },
    "KsqlDbUrl": "http://localhost:8088",
    "DlqTopicName": "dead-letter-queue",
    "DeserializationErrorPolicy": "DLQ"
  }
}
```

- 必須: `Common.BootstrapServers`, `SchemaRegistry.Url`, `KsqlDbUrl`
- 推奨: `DlqTopicName`, `DeserializationErrorPolicy`

要約: 上記を入れれば最小構成で動きます。

## 型抜粋（DLQなど）

```csharp
public sealed class DlqRecord
{
  public string SourceTopic { get; init; } = "";
  public string ErrorCode  { get; init; } = "";
  public string RawText    { get; init; } = "";
}
```

- `RawText` を見て原因を切り分けます。

要約: まず `RawText`、次に `SourceTopic` を確認します。

## 生成KSQLの例（代表）

```csharp
modelBuilder.Entity<OrderView>().ToQuery(q => q
  .From<Order>()
  .Where(o => o.Amount > 0)
  .Select(o => new OrderView { Id = o.Id, Amount = o.Amount }));
```

出力例（概念）
```
CREATE STREAM OrderView AS
SELECT Id, Amount
FROM Order
WHERE Amount > 0;
```

要約: ToQuery は CSAS/CTAS 形式のKSQLを生成します。

---

## API リファレンス（一覧）

### 属性（Attributes）
- `[KsqlTopic(name)]`: エンティティを Kafka トピックへバインドする。
  - パラメータ: `name` トピック名（必須）。
  - オプションプロパティ: `PartitionCount`（既定 1）, `ReplicationFactor`（既定 1）。
  - 用例: `[KsqlTopic("orders")]` / `[KsqlTopic("orders", PartitionCount=3, ReplicationFactor=2)]`
- `[KsqlTimestamp]`: 当該プロパティをイベントタイム（タイムスタンプ）として扱う。
  - 対象型: `DateTime` または `DateTimeOffset` を推奨（UTC を想定）。
  - 備考: 生成される KSQL の `TIMESTAMP` に対応。
- `[KsqlDecimal(precision, scale)]`: 小数の精度（桁数）と小数点以下の桁数を指定。
  - パラメータ: `precision` 総桁数, `scale` 少数桁数。
  - 用例: `[KsqlDecimal(18, 4)]`
- `[KsqlDatetimeFormat(format)]`: 文字列として表現する日時のフォーマットを指定。
  - パラメータ: `format` 日時フォーマット文字列（`yyyy-MM-ddTHH:mm:ss.fffZ` など）。
- `[KsqlKey(order)]`: 複合キーでの順序（並び）を指定。
  - パラメータ: `order` 0 以上の整数。小さいほど先頭キー。
  - 用例: `Broker` を 0、`Symbol` を 1 など。
- `[KsqlIgnore]`: スキーマ定義および送受信からプロパティを除外。
  - 備考: 内部計算や一時的なメモ用に使用。
- `[KsqlTable]`: このエンティティを Table として扱う（デフォルトは Stream）。
  - 備考: 既定動作は Stream。Table にしたい場合のみ付与。
- `[MaxLength(length)]`: 文字列プロパティの最大長を制限。
  - パラメータ: `length` 1 以上の整数。
- `[KsqlTimeFrameClose]`: タイムフレームの確定時刻を示すプロパティを明示。
  - 備考: 集計の「確定」タイミング列を区別したいケースで使用。

注記: スケジュール範囲の扱いは属性ではなく、`TimeFrame<TSchedule>` と `MarketSchedule` エンティティ（`Open/Close/MarketDate`）の組み合わせで行います。`[ScheduleRange]` は現行実装で使用していないため公開リファレンスから除外しました。

### コンテキストとビルダー
- `KsqlContextBuilder.Create()`: ビルダーを作る。
- `.UseConfiguration(IConfiguration cfg)`: 設定を渡す。
- `.UseSchemaRegistry(string url)`: SR を設定する。
- `.EnableLogging(ILoggerFactory lf)`: ログを有効にする。
- `.BuildContext<TContext>()`: `IKsqlContext` を生成する。

### Fluent API（モデル登録）
- `ModelBuilder.Entity<T>(readOnly=false, writeOnly=false)`: 型を登録する。
- `.ToQuery(Func<IQueryBuilder,IQueryBuilder> build)`: ビューを定義する。
- `From<TSource>()`: ソースを指定する。
- `Join<TRight>(expr)`: 関連を結合する。
- `Where(expr)`: 条件で絞り込む。
- `Select(selector)`: 出力形を定義する。

### イベント操作（送受信）
- `IKsqlContext.Set<T>() -> IEventSet<T>`: 型のセットを得る。
- `IEventSet<T>.AddAsync(T entity, CancellationToken? ct=null)`: 送信する。
- `IEventSet<T>.ForEachAsync(Func<T,Task> handler, CancellationToken? ct=null)`: 購読する。

### エラー処理と DLQ
- `IEventSet<T>.WithRetry(opts)`: 再試行方針を設定する。
- `IEventSet<T>.OnError(handler)`: 失敗時の処理を設定する。
- `IDlqClient.ReadAsync(CancellationToken? ct=null) -> IAsyncEnumerable<DlqRecord>`: DLQ を読む。

### コアインタフェース
- `IKsqlContext`: KSQL 連携を管理する。
- `IEventSet<T>`: 型付き操作を提供する。
- `IDlqClient`: DLQ の読み出しを提供する。
- `ITableCache<T>`: Table のキャッシュを提供する。

### 主な構成キー（appsettings.json）
- `KsqlDsl.Common.BootstrapServers`: Kafka 接続先を指定する。
- `KsqlDsl.SchemaRegistry.Url`: SR の URL を指定する。
- `KsqlDsl.KsqlDbUrl`: ksqlDB の URL を指定する。
- `KsqlDsl.DlqTopicName`: DLQ のトピック名を指定する。
- `KsqlDsl.DeserializationErrorPolicy`: 逆直列化時の方針を指定する。

---

## 詳細リファレンス（要点＋用例）

### EventSet<T>.AddAsync
- シグネチャ: `Task AddAsync(T entity, Dictionary<string,string>? headers=null, CancellationToken ct=default)`
- 動作: レコードを送信する。任意でヘッダーを付与する。
- 例外: 送信失敗時は例外を投げる。
- 用例:
  ```csharp
  await ctx.Set<Order>().AddAsync(order, new(){["cid"]=cid});
  ```
- まとめ: 送信は AddAsync、ヘッダーで相関IDを渡せる。

### EventSet<T>.ForEachAsync（オーバーロード）
- シグネチャ: `Task ForEachAsync(Func<T,Task> handler, TimeSpan timeout=default, bool autoCommit=true, CancellationToken ct=default)`
- シグネチャ: `Task ForEachAsync(Func<T,Dictionary<string,string>,Task> handler, TimeSpan timeout=default, bool autoCommit=true, CancellationToken ct=default)`
- シグネチャ: `Task ForEachAsync(Func<T,Dictionary<string,string>,MessageMeta,Task> handler, TimeSpan timeout=default, bool autoCommit=true, CancellationToken ct=default)`
- 動作: Push で購読し、必要に応じてヘッダー/メタ情報を受け取る。
- 中断: `CancellationToken` で停止する。
- 用例:
  ```csharp
  await ctx.Set<Order>().ForEachAsync((o,h,meta)=> Task.CompletedTask);
  ```
- まとめ: 目的に応じて3つのハンドラ形から選ぶ。

### ModelBuilder.Entity<T>
- シグネチャ: `Entity<T>(bool readOnly=false, bool writeOnly=false)`
- 動作: 型を登録し、Set<T>() を有効化する。
- 補足: 読み専用/書き専用の宣言ができる。
- 用例:
  ```csharp
  b.Entity<Tick>(readOnly:true);
  ```
- まとめ: 登録が無い型は操作できない。

### ToQuery（ビュー定義）
- シグネチャ: `ToQuery(Func<IQueryBuilder,IQueryBuilder> build)`
- 動作: CSAS/CTAS 相当の KSQL を生成する。
- 用例:
  ```csharp
  b.Entity<OrderView>().ToQuery(q => q.From<Order>().Where(o => o.Amount>0));
  ```
- まとめ: 集計や結合は ToQuery に残す。

### IKsqlContext.Set<T>
- シグネチャ: `IEventSet<T> Set<T>()`
- 動作: 型に対する操作ハンドルを得る。
- まとめ: 送受信と購読の起点になる。

---

## 設定キー（最小で使う）

- 必須: `KsqlDsl.Common.BootstrapServers`
  - 説明: Kafka の接続先を指定する。
  - 例: `"localhost:9092"`
- 必須: `KsqlDsl.SchemaRegistry.Url`
  - 説明: Schema Registry の URL。
  - 例: `"http://localhost:8085"`
- 必須: `KsqlDsl.KsqlDbUrl`
  - 説明: ksqlDB の URL。
  - 例: `"http://localhost:8088"`
- 推奨: `KsqlDsl.DlqTopicName`
  - 説明: DLQ のトピック名。
  - 例: `"dead-letter-queue"`
- 推奨: `KsqlDsl.DeserializationErrorPolicy`
  - 説明: 逆直列化エラー時の方針。
  - 例: `"DLQ"` / `"Skip"` / `"Retry"`

まとめ: 上記5つを埋めれば動作確認に進める。

---

## 生成 KSQL（代表パターン）

### 単純な選択（CSAS/CTAS）
```csharp
b.Entity<ViewA>().ToQuery(q => q.From<SourceA>().Select(x => new ViewA{ Id=x.Id }));
```
概念出力:
```
CREATE STREAM ViewA AS SELECT Id FROM SourceA;
```

### 結合（JOIN）
```csharp
b.Entity<OrderXCustomer>().ToQuery(q => q
  .From<Order>()
  .Join<Customer>((o,c) => o.CustomerId==c.Id)
  .Select((o,c) => new OrderXCustomer{ OrderId=o.Id, Name=c.Name }));
```

### 窓集計（Window）
```csharp
b.Entity<TickAvg1m>().ToQuery(q => q
  .From<Tick>() /* 代表表現。実際の集計 DSL に合わせて実装 */);
```

### グループ化（GroupBy）
```csharp
// 代表例。詳細は実装の GroupBy 対応に合わせる。
```

まとめ: ToQuery は代表的な KSQL 生成に対応する。
### そのほかの主要メンバー（EventSet<T>）
- `Task<List<T>> ToListAsync(CancellationToken ct=default)`: 現在のストリーム/テーブルを列挙する。
- `Task RemoveAsync(T entity, CancellationToken ct=default)`: レコードを削除する。
- `void Commit(T entity)`: 明示コミットを行う。
- `string GetTopicName()`: バインドされたトピック名を返す。
- `EntityModel GetEntityModel()`: エンティティのモデル情報を返す。
- `IKsqlContext GetContext()`: バックエンドのコンテキストを返す。
- `EventSet<T> WithRetry(int maxRetries, TimeSpan? retryInterval=null)`: 再試行方針を設定する。
- `EventSet<TResult> Map<TResult>(Func<T,Task<TResult>> mapper)` / 同同期版: メッセージを変換する。

### 拡張（エラー処理関係）
- `EntitySetErrorHandlingExtensions.OnError<T>(this IEntitySet<T>, ErrorAction)`: 失敗時の処理を設定する。

### ビルダー/オプション（拡張メソッド）
- `KsqlContextOptionsExtensions.UseSchemaRegistry(...)`: スキーマレジストリを設定する。
- `KsqlContextOptionsExtensions.EnableLogging(...)`: ログを有効化する。
- `KsqlContextOptionsExtensions.ConfigureValidation(...)`: 検証モード等を設定する。
- `KsqlContextOptionsExtensions.WithTimeouts(...)`: タイムアウトを設定する。
