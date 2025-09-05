# Advanced Rules（実務で効く要点）

この文書は「開発者が OSS を運用・拡張する際に直面する具体課題」への指針を示す。構成は 基盤 → 処理 → データ進化 → 集計 → 検証 の順とする。

## 目次
- 1. トピック管理（基盤）
- 2. Push/DLQ（処理）
- 3. Table/キャッシュ（基盤）
- 4. スキーマ互換・命名（データ進化）
- 5. Window/時間（集計）
- 6. CI/物理検証（検証）

## 1. トピック管理（基盤）
- 目的: 内部トピックの理由を理解し、誤削除を防ぐ。
- 前提: ksqlDB/Streams は内部用トピックを自動作成する。

#### 自動作成されるトピック（この OSS 固有）
1. **DLQ（デッドレターキュー）**
   - 既定名: `dead-letter-queue`
   - `KsqlDsl.DlqTopicName` で変更可能。

2. **ビューのシンク用トピック**
   - `ToQuery` で定義したエンティティ型名を小文字化して作成。
   - 例: `OrderSummary` → `ordersummary`

⚠️ 補足: ksqlDB/Streams が内部処理で生成するトピック  
- 再分散用: `<シンク名>-repartition`  
- 状態保持用: `<シンク名>-changelog`  
これらはプラットフォームの管理対象であり、本 OSS の設計対象外。

### 命名規約
- ストリーム/テーブル: エンティティ型名を小文字化（例: `BasicMessage` → `basicmessage`）
- 明示指定: ソース側は `[KsqlTopic("<name>")]` で上書き可
- ビューのシンク: ビュー用エンティティ型名を小文字化（`ToQuery` で定義した型名）

### 保持（retention）の設定
- DLQ は appsettings.json で設定する。
```json
{
  "KsqlDsl": {
    "DlqTopicName": "dead-letter-queue",
    "DlqOptions": { "RetentionMs": 604800000 }
  }
}
```
- シンク/内部トピックの保持は設定対象外（プラットフォーム側で管理）。

要約: 自動作成は DLQ/シンク/内部。命名は小文字化。DLQ 保持は設定で管理。

## 2. Push/DLQ（処理）
- 目的: 正常系は Push 購読で処理し、異常系は DLQ で追う。
- 指針: `ForEachAsync` で購読。DLQ は定期巡回し原因を記録する。
```csharp
await ctx.Set<Event>().ForEachAsync(e => { /* use */ return Task.CompletedTask; });
await foreach (var rec in ctx.Dlq.ReadAsync()) Console.WriteLine(rec.RawText);
```
要約: 正常は Push、異常は DLQ。両輪で監視する。

## 3. Table/キャッシュ（基盤）
- 目的: 参照負荷を抑え、読みを安定させる。
- 指針: 参照主体は `.AsTable(useCache:true)` を選ぶ。
```csharp
protected override void OnModelCreating(IModelBuilder b)
  => b.Entity<RefData>().AsTable(useCache: true);
```
要約: 参照は Table+cache。頻繁更新は Stream。

## 4. スキーマ互換・命名（データ進化）
- 目的: 互換を保ち、破壊変更を避ける。
- 指針: 追加は null 許容。破壊は新トピックへ移行。型→Avro の対応を把握する。
```csharp
public class Rate { public string Symbol { get; set; } = ""; public decimal Price { get; set; } public string? Source { get; set; } }
```
備考: `decimal`+[KsqlDecimal]→bytes(decimal)、`DateTime`→long(timestamp-millis)。
要約: 追加は互換、破壊は移行。命名は型名基準で一貫。

## 5. Window/時間（集計）
- 目的: 集計の時間軸を誤らない。
- 指針: 時間は UTC。窓長は要件から逆算。Final は遅延許容。
```csharp
modelBuilder.Entity<Candle>().ToQuery(q => q.From<Tick>().Where(t => t.Symbol == "USDJPY"));
```
要約: UTC 基準で窓を選ぶ。集計はビューに残す。

## 6. CI/物理検証（検証）
- 目的: 依存起動→テスト→疎通で安全に検証する。
- 指針: 依存を起動し、テスト実行し、`/info` で疎通確認。ロングランは physicalTests を用いる。
```bash
docker-compose -f tools/docker-compose.kafka.yml up -d
dotnet restore && dotnet test -v minimal
curl -sf http://localhost:8088/info >NUL
```
要約: 起動→テスト→疎通。長時間検証は physicalTests を使う。
# Advanced Rules（実務で効く要点）

この文書は「開発者が OSS を運用・拡張する際に直面する具体課題」への指針を示す。構成は 基盤 → 処理 → データ進化 → 集計 → 検証 の順とする。

## 1. トピック管理（基盤）
- 目的: 内部トピックの理由を理解し、誤削除を防ぐ。
- 前提: ksqlDB/Streams は内部用トピックを自動作成する。

### 1-1. どんなトピックが自動作成されるか
- DLQ: `dead-letter-queue`（既定。`KsqlDsl.DlqTopicName` で変更可）。
- ビューのシンク: `ToQuery` 定義エンティティ名を小文字化（例: `ordersummary`）。
- 内部: `<シンク名>-repartition`, `<シンク名>-changelog`（再分散/状態保持）。
- 注意: `_schemas`, `__consumer_offsets` などのシステム系は対象外。

### 1-2. トピック命名をどう決めるか
- ストリーム/テーブル: 型名を小文字化（例: `BasicMessage` → `basicmessage`）。
- 明示指定: ソース側は `[KsqlTopic("<name>")]` で上書き可能。
- ビューのシンク: ビュー用の型名を小文字化（`ToQuery` で定義した型名）。

### 1-3. 保持期間をどのように管理するか
- DLQ の保持は appsettings.json で設定する。
```json
{
  "KsqlDsl": {
    "DlqTopicName": "dead-letter-queue",
    "DlqOptions": { "RetentionMs": 604800000 }
  }
}
```
- 内部トピックの保持は対象外（Kafka 側のポリシーで管理）。

要約: 自動作成は DLQ/シンク/内部。命名は小文字化が既定。DLQ 保持は設定で管理する。

## 2. Push/DLQ（処理）
- 目的: 正常系は Push 購読、異常系は DLQ で追跡する。
- 指針: Push は `ForEachAsync`、DLQ は定期巡回し原因を記録する。
```csharp
await ctx.Set<Event>().ForEachAsync(e => { /* use */ return Task.CompletedTask; });
await foreach (var rec in ctx.Dlq.ReadAsync()) Console.WriteLine(rec.RawText);
```
要約: 正常は Push、異常は DLQ。両輪で監視する。

## 3. Table/キャッシュ（基盤）
- 目的: 参照負荷を抑え、読みを安定させる。
- 指針: 参照主体は `.AsTable(useCache:true)` を基本とする。頻繁更新は Stream を選ぶ。
```csharp
protected override void OnModelCreating(IModelBuilder b)
  => b.Entity<RefData>().AsTable(useCache: true);
```
要約: 参照は Table+cache。頻繁更新は Stream。

## 4. スキーマ互換・命名（データ進化）
- 目的: 互換性を保ち、破壊的変更を避ける。
- 指針: 追加は null 許容で安全に適用する。破壊変更は新トピックへ移行する。型と Avro 対応を把握する。
```csharp
public class Rate { public string Symbol { get; set; } = ""; public decimal Price { get; set; } public string? Source { get; set; } }
```
備考: `decimal`+[KsqlDecimal]→ bytes(decimal)、`DateTime`→ long(timestamp-millis)。
要約: 追加は互換、破壊は移行。命名は型名基準で一貫する。

## 5. Window/時間（集計）
- 目的: 集計の時間軸を誤らない。
- 指針: 時間は UTC に統一する。ウィンドウ長は要件から逆算する。Final 出力は遅延を許容する。
```csharp
modelBuilder.Entity<Candle>().ToQuery(q => q.From<Tick>().Where(t => t.Symbol == "USDJPY"));
```
要約: UTC 基準で窓を選ぶ。集計はビューに残す。

## 6. CI/物理検証（検証）
- 目的: 依存起動→テスト→疎通の流れで安全に検証する。
- 指針: 依存サービスを起動してからテストする。`/info` で疎通確認する。長期検証は physicalTests を利用する。
```bash
docker-compose -f tools/docker-compose.kafka.yml up -d
dotnet restore && dotnet test -v minimal
curl -sf http://localhost:8088/info >NUL
```
要約: 起動→テスト→疎通。長時間検証は physicalTests を使う。
