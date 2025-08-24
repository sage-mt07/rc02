# Kafka.Ksql.Linq.Cache namespace 責務ドキュメント

## 概要
Table 型 `EntitySet` のための簡易メモリキャッシュを提供する軽量 namespace です。
`MappingRegistry` から得た Avro 用 key/value 型を利用し、Kafka から取得したデータを POCO へ結合します。

## サブnamespace
- `Kafka.Ksql.Linq.Cache.Core` – キャッシュ本体 (`TableCache<T>`, `TableCacheRegistry`)
- `Kafka.Ksql.Linq.Cache.Configuration` – `TableCacheOptions` などの設定
- `Kafka.Ksql.Linq.Cache.Extensions` – `KsqlContext` への統合拡張

## 主なコンポーネント
### TableCacheRegistry
- 型ごとの `TableCache<T>` を登録・取得
- `RegisterEligibleTables` は現在 no-op

### TableCache<T>
- `ToListAsync` でキャッシュ内容を列挙
- `MappingRegistry` に登録された `KeyValueTypeMapping` から key/value を POCO に復元

### ReadCachedEntitySet<T>
- 読み取り専用 `IEntitySet<T>` 実装
- `ToListAsync` 経由で `TableCache<T>` の結果を返却

### KsqlContextCacheExtensions
- `UseTableCache` で `TableCacheRegistry` を初期化
- `GetTableCache<T>` で型安全に取得

## 処理フロー概要
1. `UseTableCache()` で `TableCacheRegistry` を登録
2. `RegisterEligibleTables()` によりキャッシュ対象を登録
3. `ReadCachedEntitySet<T>` が `TableCache<T>` を介してデータ取得

