# Codex向け作業共有資料：ToQuery DSL導入（2025-08-01）

## 🎯 目的

Kafka.Ksql.Linq における `ToQuery` ベースの View定義型DSLを導入し、Entity FrameworkライクなLINQ表現から KSQL CREATE STREAM/TABLE 文を安全に生成できる構成を確立する。

この実装により、Kafka/KSQLを知らない.NET開発者でも、安全かつ型安全にView構築・Join処理が記述可能になる。

---

## 🧭 全体構成（高レベル設計）

```csharp
modelBuilder.Entity<OrderSummary>().ToQuery(q => q
    .From<Order>()
    .Join<Product>((o, p) => o.ProductId == p.Id)
    .Where((o, p) => p.Category == "Books")
    .Select((o, p) => new OrderSummary {
        OrderId = o.Id,
        ProductName = p.Name
    }));
```

この構文は `EventSet<T>.ToQuery(...)` に対応し、以下を実現する：

- DSL構文でView（KSQLクエリ）を定義
- `KsqlQueryModel` に中間表現として構文情報を保持
- `.ToQuery(...)` 呼び出し時に構文エラーは即例外
- `OnModelCreating` 中に記述し、KsqlContext による `CREATE STREAM/TABLE` 実行を支援
- **2テーブルJOINまでを正式サポート対象とする**（将来的な拡張余地は残すが、今回のスコープは2テーブル）

### 🔄 2テーブルJOINのサンプルコード

```csharp
modelBuilder.Entity<OrderSummary>().ToQuery(q => q
    .From<Order>()
    .Join<Customer>((o, c) => o.CustomerId == c.Id)
    .Select((o, c) => new OrderSummary {
        OrderId = o.Id,
        CustomerName = c.Name
    }));
```

※3テーブル以上のJOINは現時点では未サポート。意図しない使用を防ぐため制約として明示。

---

## 🛠️ Codexに依頼する作業一覧

### ✅ DSL構文側の定義

- `KsqlQueryable<T1>` / `KsqlQueryable<T1, T2>` の拡張実装
- `From<T>()`, `Join<T>()`, `Where(...)`, `Select(...)` の連鎖定義（※Tumblingは後日）
- 各種ラムダ式は `Expression` 解析を用いて `KsqlQueryModel` に変換

### ✅ 中間モデルの整備

- `KsqlQueryModel`：From/Join/Where/Select の構文情報を持つ（Windowは除外）
- `ToQuery(...)` 実行時に構文妥当性を検証し、モデルを返す

### ✅ 変換器との連携

- `KsqlStatementBuilder`：`KsqlQueryModel` を KSQL CREATE 文に変換する
- `EventSet<T>.ToQuery(...)` 呼び出し時点で `KsqlStatementBuilder` に変換を依頼
- `ksqlContext` 側で `CREATE STREAM/TABLE` を実行

### ✅ ToQuery の責務

- KSQL文（CREATE STREAM/TABLE）の生成
- Key/Value に分割されたプロパティ一覧の生成（KSQL定義順に準拠）
- KSQL出力およびキー構造の妥当性チェック（不整合時には例外）

### ✅ DSL構文のための機能詳細

- Join処理のための2引数ラムダ式対応（`.Join<T2>((t1, t2) => ...)`）
- Lambda式のプロパティ選択を明示的に解析（`MemberExpression`）
- `Select` 節では匿名型・新規POCO・既存POCOへのマッピングをサポート
- `From<T>()` 単体でも `.ToQuery(...)` によりCreate文を構成可能
- `ToQuery(...)` 呼び出し時点でKey/Value構造を確認し、不備があれば例外化
- **対象スコープは2テーブルJOINまで。3テーブル以上のJoinはサポート外（誤解防止のため明示）**

### ✅ 既存コードの整理（不要ロジックの削除）

- 旧 `HasQuery(...)`, `HasQueryFrom(...)`, `DefineQuery(...)`, `DefineQueryFrom(...)` ベースのロジックを段階的に削除
- 古い View 構文モデルの生成コード（例：内部 DSL スタブ、サンプルなど）
- 置き換え可能な既存テストコード（新構文で再実装予定）

### ✅ KsqlContext接続の整備

- `InitializeWithSchemaRegistration()` によるスキーマ登録とKSQL登録の流れを実装：
  1. `ConfigureModel()` によるPOCOとView定義の構築
  2. `MappingRegistry.RegisterEntityModel(...)` により Avro型情報を整理
  3. `KsqlStatementBuilder` により `KSQL CREATE STREAM/TABLE` 文を生成
  4. ksqlDBへのCREATE文発行（`ksqlContext`）
  5. Kafka側接続確認（`EnsureKafkaReadyAsync()`）

---

## 🚦 進め方・進行フロー

0. **既存View定義コードの設計整理（Codex）**\
   旧 `HasQuery(...)`, `HasQueryFrom(...)`, `DefineQuery(...)`, `DefineQueryFrom(...)` ベースの構文を `ToQuery` にどう移行するか、対象クラスやDSL構文の観点で整理・資料化する。

1. **DSL構文の土台構築（Codex）**\
   `KsqlQueryable<T1>`, `KsqlQueryable<T1,T2>` を実装し、`From`, `Join`, `Where`, `Select` のメソッドチェーン構造を確立する。

2. ``** の中間モデル生成（Codex）**\
   DSL構文から中間モデル `KsqlQueryModel` を構築。各セクション（ソース・条件・選択列）を明示。

3. **構文検証と例外対応（Codex）**\
   `ToQuery()` 呼び出し時に構文妥当性を検証。異常時は即例外スロー。

4. **KSQL生成と登録（Codex → KsqlStatementBuilder）**\
   `KsqlQueryModel` を `KsqlStatementBuilder` 経由で KSQLに変換。`ksqlContext` による登録処理に接続。\
   ※この前提として `MappingRegistry` によるAvro型情報の準備が必要。

5. **テストコード・例外検証（鳴瀬）**\
   DSLの式構文が正しくモデル化・KSQL化されることを確認。Join構文や未サポート演算の例外も検証。

6. **仕様・APIドキュメント更新（天城）**\
   `api_reference.md` と `dev_guide.md` に使用例と背景設計を反映。

7. **旧ロジックの整理（Codexまたは天城）**\
   完全移行後、旧View構文系コードを削除し、依存解消を確認。

---

## 👥 担当と責務

| 担当    | 役割                                                  |
| ----- | --------------------------------------------------- |
| Codex | DSL構文の設計・実装、KsqlQueryModel生成、ビルダー接続、旧ロジック削除、旧構文設計整理 |
| 天城    | 指示の構造整理、成果物の `api_reference.md` 反映、Codex出力レビュー      |
| 鳴瀬    | テストコード作成、例外系テスト設計（特にJoin/型不整合）                      |

---

## 📏 技術ルール（進行中の合意）

- `ToListAsync()` は RocksDBキャッシュから取得（Stream不可）
- `Table` の場合のみ全件取得可、`Stream` は `.ForEachAsync()` を使用
- `EventSet<T>` で POCOとクエリ構造を明確に分離する
- `ToQuery()` による構文エラーはその場で例外（Create文出力前に検知）
- `CREATE STREAM/TABLE` は `ksqlContext` 側が発行
- DSL構文にはTumbling等のWindow関数は含まない（後日対応）

---

## 📚 参考

- 旧資料：`toquery_migration_plan.md`
- 進行中API：`api_reference.md`（Codex実装完了後に反映）

## 調査進捗 (Codex)
### 2025-08-01 20:42 JST [codex]
- `HasQuery`, `HasQueryFrom`, `DefineQuery`, `DefineQueryFrom` の実装位置を確認。
- 依存する構文モデルとして `QuerySchema`, `QueryBuilder<T>`, `QueryAnalyzer` を把握。
- これらを利用するテストコード (`HasQueryFromTests` など) を特定。
- 役割を定義系・実行系・テスト系に分類し、旧DSLで残すべき部分と削除候補を整理。

### 調査結果まとめ
| 区分 | 内容 |
| ---- | ---- |
| **残すべき構造** | `QuerySchema` 抽出ロジックと `KsqlContextQueryExtensions` による登録処理、`KsqlCreateStatementBuilder` などのKSQL生成部 |
| **削除すべきコード** | `HasQuery` 系拡張メソッド、`DefineQuery` 系拡張メソッド、`QueryBuilder<T>` とその関連テスト |
| **再設計が必要な点** | View定義DSLを `ToQuery` チェーンに統合し、`KsqlQueryModel` 中間表現へ置き換える部分 |

### 2025-08-01 20:48 JST [codex]
- Phase 0補足調査を実施。詳細は以下に追記。

#### QuerySchema / QueryBuilder<T> / QueryAnalyzer の役割
- **QuerySchema**: ソース型・ターゲット型のメタ情報と Key/Value スキーマを保持する単純モデル。`IsValid` や `ExecutionMode` を持ち、`GetStreamTableType()` で Stream/Table 判定を返す。
- **QueryBuilder<T>**: `FromSource` で基点型を指定し、`AsPull`/`AsPush` などで実行モードを設定するビルダー。`GetSchema()` で `QueryAnalyzer` を呼び出して `QuerySchema` を生成する。
- **QueryAnalyzer**: LINQ 式を走査して GroupBy/Select/Where を解析し、`QuerySchema` を構築する。型検証と自動キー抽出もここで行う。

#### 拡張メソッドの ToQuery 置換可否
| メソッド | 置換可否 | 備考 |
| --- | --- | --- |
| `HasQuery` | 部分移行 | `.AsPull()` など実行モード指定は未対応のため一部機能は残留 |
| `HasQueryFrom` | 置換可能 | `ToQuery(q => q.From<TSource>() ...)` で代替可能 |
| `DefineQuery` | 置換可能 | エンティティ定義と同時に `ToQuery` へ移行 |
| `DefineQueryFrom` | 置換可能 | ソース型明示も `From<T>()` で表現 |

#### View定義系テスト一覧
- **DSL基盤**: `HasQueryFromTests`, `QueryAnalyzerTests`, `QueryBuilderExecutionModeTests`
- **パイプライン生成**: `DDLQueryGeneratorTests`, `DMLQueryGeneratorTests`, `JoinQueryGeneratorTests`, `QueryAssemblyContextTests`, `ExpressionAnalysisResultTests`
- **ビルダー/Visitor**: `GroupByClauseBuilderTests`, `HavingClauseBuilderTests`, `JoinClauseBuilderTests`, `SelectClauseBuilderTests`, `WhereClauseBuilderTests`, `WindowClauseBuilderTests`, `FunctionTranslatorTests`, `JoinLimitationEnforcerTests` および Visitors テスト群
- **DSLクラス**: `Dsl/KsqlQueryable2Tests`
- **実行セット**: `JoinableEntitySetTests`, `TypedJoinResultEntitySetTests`, `ValidationResultTests`, `QueryMetadataTests`

#### MappingRegistry / AvroSchemaRegistry との関連
- `KsqlContextQueryExtensions.GetQuerySchema` で `MappingRegistry.Register` を呼び出し、取得した `QuerySchema` の Key/Value プロパティを動的型として登録する。
- ReadOnly エンティティの場合は `SchemaRegistryMetaProvider.GetMetaFromSchemaRegistry` 経由で Avro Schema Registry からメタ情報を取得し、その結果を `MappingRegistry` へ登録する。
- `QuerySchema` 自体は Registry 依存を持たないが、スキーマ登録フローのハブとして利用されている。

#### KsqlQueryModel 対応マッピング（旧 → 新）
| QuerySchema | KsqlQueryModel | 備考 |
| --- | --- | --- |
| `SourceType` | `SourceTypes[0]` ほか | JOIN 時は複数型を `SourceTypes` に保持 |
| `TargetType` | - | 新DSLではエンティティ型がそのまま EventSet に対応 |
| `KeyProperties` / `ValueProperties` | - | マッピング登録後に `MappingRegistry` が保持 |
| `ExecutionMode` | - | ToQuery DSL では今後オプションで指定予定 |
| - | `JoinCondition` / `WhereCondition` / `SelectProjection` | LINQ式を保持し、`KsqlCreateStatementBuilder` が解釈 |
| - | `Window` | Window指定を直接モデル化 |
| - | `IsAggregateQuery` | 集約関数使用時にフラグを立てる |
## 2025-08-01 12:05 JST [codex]
- Implemented KsqlQueryable<T> and root KsqlQueryRoot with Join/Where/Select chain.
- Added EntityBuilder.ToQuery extension storing KsqlQueryModel.
- Added tests for join clause generation.


## 2025-08-01 12:16 UTC [codex]
- Reverted variable rename in KsqlContext due to inline comment.
## 2025-08-01 21:31 JST [codex]
- Integrated ToQuery DSL with KsqlCreateStatementBuilder and schema registration.
- Added MappingRegistry registration from KsqlQueryModel and EMIT CHANGES clause.

## 2025-08-01 13:45 JST [codex]
- Added QueryBuildStage enum and state tracking to enforce DSL call order.
- Guarded Join and Select methods with order checks and a JOIN requires WHERE rule.
- Implemented MappingRegistry.GetLastRegistered and additional validation.
- Provided KsqlQueryModel.Dump() for debugging.
- Created tests covering valid JOIN generation and WHERE clause requirement.

## 2025-08-02 01:46 JST [codex]
- Phase3 instruction to integrate the ToQuery DSL into `KsqlContext`.
- Detect `EventSet<T>.ToQuery(...)` chains during `OnModelCreating` and store the
  resulting `KsqlQueryModel`.
- Use `KsqlCreateStatementBuilder` to generate `CREATE STREAM/TABLE AS SELECT`
  statements, automatically appending `EMIT CHANGES` based on
  `ExecutionMode`.
- Register key/value schemas derived from the `KsqlQueryModel` with
  `MappingRegistry` when `InitializeWithSchemaRegistration()` is executed.
- Add tests verifying SQL generation and schema registration through
  `KsqlContext` initialization.


## 2025-08-02 02:43 JST [codex]
Codex作業指示（フェーズ4：EntitySet / POCO統合）

🎯 タイトル: ToQuery DSL による EntitySet 宣言・スキーママッピング統合対応

🔍 目的
KsqlContext の EventSet<T> に対して .ToQuery(...) チェーンを用いた View/Table 定義を正式にサポートする。POCOクラスとの統合により、Entity Framework のような使い勝手を実現する。

✅ 作業項目
- EventSet<T> 拡張
  - ToQuery(...) を呼び出した EventSet<T> は View 定義（KsqlQueryModel）として登録される。
  - View の場合、T は結果型（Select投影後）とし、.Select(...) による匿名型は不許可とする。
- POCO側属性補完（必要に応じて）
  - KsqlKey, KsqlIgnore, KsqlPrecision などの属性は View 生成時にも有効とする。
  - .Select(...) 内のプロパティ順が KSQL のカラム順になるよう MappingRegistry に記録する。
- MappingRegistry 拡張
  - RegisterQueryModel(...) を追加し、KsqlQueryModel を元に Key/Value のスキーマをマッピング登録する。
  - 順序保証・Precision情報・型名などを含めて Avro に変換可能な状態に整える。
- テスト追加
  - .ToQuery(...) による EntitySet 宣言が CREATE STREAM/TABLE AS SELECT を正しく生成し、スキーマ登録されることを確認する。
  - キー付き / キーなし、Selectの順序違いなどで挙動が一貫していることを検証。

📎 備考
このステップで「POCO ↔ Query DSL ↔ KSQL ↔ Avro」の一連の統合が完了します。
.ToQuery(...) が HasQuery(...) の完全上位互換になる設計が前提です。
