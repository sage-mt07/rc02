# ToListAsync における PK フィルタと RocksDB キャッシュ

🗕 2025年8月24日（JST）
🧐 作成者: くすのき

`ToListAsync()` は Table 型の `IEntitySet<T>` に対する Pull Query として実行され、Streamiz が管理する RocksDB キャッシュから結果を取得します。本ドキュメントでは、主キーを用いたフィルタ処理とキャッシュ連携の仕組みを示します。

## 1. PK フィルタの仕組み
- `ToListAsync(filter)` は主キーの各パートを `List<string>` として受け取ります。
- 受け取ったパートは NUL (\u0000) で連結し、前方一致プレフィックスとして扱います。
- RocksDB のキーは同じ区切りで保存されているため、`StartsWith` 判定だけで効率的な範囲検索が可能です。

```csharp
// 例: Broker + Symbol の複合PKを指定
var orders = await ctx.Set<Order>().ToListAsync(new() { "BrokerA", "AAPL" });
```

## 2. RocksDB キャッシュ連携
- 初回呼び出し時に `TableCache` が `KafkaStream` の状態を `RUNNING` まで待機します。
- キャッシュが有効なテーブルでは、RocksDB に保持された key/value を列挙し、必要に応じて主キーでフィルタします。
- フィルタが空の場合は全件を返します。

RocksDB を利用することで、Kafka への Pull Query を発行せずローカルで高速に結果を取得できます。

## 3. 利用シナリオ
1. `UseTableCache()` でテーブルキャッシュを有効化する。
2. `ctx.Set<T>().ToListAsync(filter)` を呼び出し、主キーで絞り込んだ結果を取得する。
3. フィルタを省略するとテーブル全件を列挙する。

---
この設計により、`ToListAsync()` は主キーに基づく部分一致検索と RocksDB キャッシュによる低レイテンシ読取を両立します。
