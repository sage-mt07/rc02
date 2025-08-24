## KSQL DSLにおけるクラス責務とスキーマ管理の設計指針

### 🎯 設計前提

- LINQクエリは POCO 型と紐づく
- Key/Value の構造は LINQ DSL 構文から抽出される
- Query namespace は構文解析とスキーマ抽出までを担い、管理責務は持たない
- スキーマの永続的管理は KsqlContext（アプリケーション側）が行う
- 抽出された Key/Value 型は `Mapping` namespace に登録され、Avro 用の型として利用される

---

### 🧩 各コンポーネントの責務一覧

| クラス / コンポーネント名 | 主な責務 | 管理内容 | 備考 |
|----------------------------|-----------|----------|------|
| `KsqlContext` | Queryモデルの登録・制約検証 | POCO型とスキーマの紐づけ | 最終的なスキーマ責任者 |
| `IModelBuilder` / `EntityBuilder<T>` | Query構文のDSL記述支援 | LINQ式の記述提供 | `.ToQuery()` を通じたDSL記述 |
| `Query.Analyzer` | LINQ式の式木解析 | GroupBy/Select 構造の抽出 | Key/Value候補の識別 |
| `Query.SchemaExtractor` | LINQ式からスキーマ生成 | 一時スキーマの抽出 | serializer連携用途 |
| `Query.Abstract.*` | Query定義のIF群 | DSL解析の標準化 | Interface管理の中心 |
| `Serialization.*` | シリアライザ生成と最適化 | 実行時スキーマに基づく | キャッシュ対応可能 |
| `Cache.*` | 状態管理構造 | Materialized View用途 | 今回の範囲では直接関係なし |

---

### 📌 スキーマの管理責任と境界

| 領域 | 管理責任 | 補足 |
|------|----------|------|
| クエリDSL構文とPOCOの紐づけ | `KsqlContext` | `.ToQuery()` で記述 |
| LINQ構文からKey/Value抽出 | `Query.Analyzer`, `SchemaExtractor` | 解析のみで永続管理はしない |
| スキーマの永続保持 | `KsqlContext` | `RegisterQuerySchema<T>()` で登録 |
| 実行時のスキーマ選択 | `Serialization.*` | スキーマに基づきSerializer生成 |

---

### 🔄 時系列の処理フロー

1. `OnModelCreating()` にて `.ToQuery()` を記述
2. `Query.Analyzer` が LINQ 式を解析し、Key/Value 構造を抽出
3. `KsqlContext` が `RegisterQuerySchema<T>()` にてスキーマ登録
4. 実行時に `SerializerFactory` がスキーマを参照し、該当のSerializerを生成
5. ストリームの出力やバリデーション処理に適用

---

### 🧪 クエリ具体例

```csharp
modelBuilder.Entity<CategoryCount>()
    .ToQuery(root => root.From<ApiMessage>()
        .Where(x => x.Category == "A")
        .Select(g => new CategoryCount { Key = g.Category, Count = 1 }));
```

このクエリに対して：

- `Query.Analyzer` が式木を解析し、`GroupBy(x => x.Category)` により Key を `Category` と判断
- `Select(...)` により `CategoryCount` の構造が Value として特定される
- `KsqlContext` 側がこのモデルを登録し、MappingRegistry によりキー/値型が生成される
- `Serialization.*` はこの情報をもとに、適切な Key/Value の Serializer を選定

---

### 💡 今後の拡張候補

- `QueryModel` メタ情報のキャッシュ
- `.ToQuery()` を通じた自動登録の仕組み
- Query解析結果の標準DTO設計
- Queryスキーマに対するバリデーション機能強化（静的チェック）

---

この構成を基盤に、AIエージェント（鳴瀬、鏡花、詩音、じんと）とのレポートライン構築も容易となり、責務範囲に基づいたスキーマ・構文・実行の一貫性が担保される。

