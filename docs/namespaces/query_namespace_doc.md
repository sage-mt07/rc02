# Kafka.Ksql.Linq.Query Namespace 責務資料

## 概要
LINQ式をKSQLクエリに変換する責務を担うnamespace。責務分離設計により、抽象化層を通じて他のnamespaceとの疎結合を実現。

## サブnamespace別責務

### 1. Abstractions（抽象化層）- 最重要
**責務**: 他namespaceとのインターフェース定義
**重要度**: ★★★★★

#### 主要インターフェース
- **IKsqlBuilder**: KSQL構文ビルダーの共通インターフェース
- **IQueryTranslator**: LINQ式からKSQL文への変換責務
- **IKsqlQueryable**: LINQ クエリのエントリポイント

#### 設計原則
- 他namespaceからの依存はこの層のみ
- Builder分割による責務明確化
- Pull/Push Query判定機能

### 2. Builders（クエリ構築層）- 高重要度
**責務**: LINQ式木からKSQL句内容の構築
**重要度**: ★★★★☆

#### 責務分離設計
各BuilderはKSQLキーワードを**除外**し、純粋な句内容のみ生成：

##### 主要Builder
- **SelectClauseBuilder**: `col1, col2 AS alias` (SELECT除外)
- **WhereClauseBuilder**: `condition1 AND condition2` (WHERE除外)
- **GroupByClauseBuilder**: `col1, col2` (GROUP BY除外)
- **JoinClauseBuilder**: 完全なJOIN文出力（例外的にキーワード含む）

##### 共通基盤
- **BuilderBase**: Builder共通制約・バリデーション
- **BuilderValidation**: 式木安全性チェック、深度制限
- **JoinLimitationEnforcer**: 2テーブル制限の厳格実装

-#### ストリーム処理制約
- 2テーブルJOIN制限
- ネストした集約関数禁止
- 式木深度制限（スタックオーバーフロー防止）

### 3. Functions（関数変換層）- 高重要度
**責務**: C#メソッドからKSQL関数への変換
**重要度**: ★★★★☆

#### 主要コンポーネント
- **KsqlFunctionRegistry**: 100+のC#→KSQL関数マッピング
- **KsqlFunctionTranslator**: メソッド呼び出し変換エンジン
- **KsqlFunctionMapping**: 変換規則定義（引数数、テンプレート等）

#### 対応関数カテゴリ
```
文字列関数: ToUpper, Contains, StartsWith等
数値関数: Abs, Round, Floor等  
日付関数: AddDays, Year, Month等
集約関数: Sum, Count, Max等
配列関数: ArrayLength, ArrayContains等
JSON関数: JsonExtractString等
型変換関数: ToString, Parse等
```

#### データ型と関数の対応表（主要関数）

| 関数 | INT | BIGINT | DOUBLE | DECIMAL(p,s) | STRING | BOOLEAN | DATE/TIME/TIMESTAMP | STRUCT/ARRAY/MAP |
|------|-----|--------|--------|--------------|--------|---------|--------------------|-----------------|
| SUM() | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| AVG() | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| MIN() | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| MAX() | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| COUNT() | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| TOPK() | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| COLLECT_LIST() | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| LCASE() | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| UCASE() | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| LEN() | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |


### 4. Pipeline（クエリ生成パイプライン）- 高重要度
**責務**: 完全なKSQLクエリの組み立て
**重要度**: ★★★★☆

#### Generator層（統一実装基盤）
- **GeneratorBase**: Generator共通制約、Builder依存注入必須
- **DMLQueryGenerator**: SELECT文生成（Pull/Push Query対応）
- **DDLQueryGenerator**: CREATE STREAM/TABLE文生成
- **JoinQueryGenerator**: JOIN専門生成器（2テーブル制限対応）

#### 構造化組み立て
- **QueryStructure**: クエリ構造統一管理
- **QueryClause**: 句定義と優先度管理
- **QueryAssemblyContext**: 実行モード（Pull/Push）管理

### 5. Linq（LINQ統合層）- 中重要度
**責務**: LINQメソッドチェーンとJOIN操作の統合
**重要度**: ★★★☆☆

#### JOIN操作サポート
- **IJoinableEntitySet\<T>**: JOIN可能なEntitySet
- **IJoinResult\<TOuter, TInner>**: 2テーブルJOIN結果
- **JoinableEntitySet\<T>**: 既存EntitySetのJOIN機能拡張

## アーキテクチャ特徴

### 責務分離の徹底
1. **Builder**: 句内容のみ生成（キーワード除外）
2. **Generator**: 完全なクエリ組み立て
3. **Translator**: LINQ式解析
4. **Abstractions**: インターフェース統一

### ストリーム処理対応
- Pull Query（一回限り）vs Push Query（ストリーミング）
- 2テーブルJOIN制限
- co-partitioningパフォーマンス考慮

#### Push Query と Pull Query の対応

| | STREAM（ストリーム） | TABLE（テーブル／KTable） |
|---|---|---|
| Push Query | ✅ サポート（リアルタイムで流れる） | ✅ サポート（更新イベントが流れる） |
| Pull Query | ❌ 非対応（そもそも状態がない） | ✅ 対応（現在の状態を取得できる） |

##### Pull Query で使えない主な表現

| 分類 | 内容（禁止される表現） | 例 | 備考 |
|---|---|---|---|
| 集約関数 | `SUM()`, `AVG()`, `COUNT()`, `MIN()`, `MAX()` 等 | `SELECT SUM(AMOUNT) FROM ORDERS;` | ❌ |
| 集約関数（BY_OFFSET） | `EARLIEST_BY_OFFSET()`, `LATEST_BY_OFFSET()` など | `SELECT EARLIEST_BY_OFFSET(NAME) FROM USERS;` | ❌ |
| GROUP BY | `GROUP BY` 句 (Push Query では `EMIT CHANGES` が自動付与) | `SELECT COUNT(*) FROM ORDERS GROUP BY ITEM EMIT CHANGES;` | ❌ |
| EMIT CHANGES | `EMIT CHANGES` はPull Queryでは使用不可 | `SELECT * FROM TABLE EMIT CHANGES;` | ❌（Push専用） |
| JOIN句 | テーブル・ストリームの JOIN | `SELECT * FROM A JOIN B ON A.ID = B.ID;` | ❌ |
| WINDOW句 | `WINDOW TUMBLING`, `HOPPING`, `SESSION` など | `SELECT COUNT(*) FROM STREAM WINDOW TUMBLING ...` | ❌ |
| 非KTable参照 | STREAM からの Pull Query | `SELECT * FROM STREAM;` | ❌（TABLEのみ可） |
| 非キー検索 | 主キー以外での `WHERE` 検索 | `SELECT * FROM TABLE WHERE COL2 = 'x';` | ❌ |

### エラーハンドリング統一
- 式木バリデーション（深度、複雑度制限）
- Builder例外の統一処理
- SQL安全性チェック（基本的なインジェクション防止）

## 他Namespaceとの関係

### Abstractionsを通じた疎結合
```
Core.Abstractions → Query.Abstractions ← Query.Builders
                                      ← Query.Pipeline  
                                      ← Query.Linq
```

### 依存方向
- 他namespace → Query.Abstractions（のみ）
- Query内部 → 相互依存なし（Builder → Pipeline → Linq）

## 重要な設計制約

1. **Builder依存注入必須**: Generatorは必ずBuilder注入
2. **キーワード分離**: Builder=句内容、Generator=完全文
3. **2テーブル制限**: ストリーム処理性能のための制限
4. **式木安全性**: 深度・複雑度制限によるスタックオーバーフロー防止
5. **NULL安全**: 全Builder・Generatorで統一されたNULL処理

## 論理Key/Valueメタ情報提供への転換

2025-07-13 の設計変更により、Query namespace は LINQ 解析結果から
"物理モデル" だけを抽出する方式を改め、論理 Key/Value 構造と
クラス名・namespace を含むメタ情報を `QuerySchema` として返すよう統一した。
このメタ情報は MappingManager とスキーマ生成ロジックに連携され、
Kafka/Avro/Schema Registry まで一貫した管理を実現する。
将来のスキーマバージョンや互換性フラグも `KeyInfo`/`ValueInfo` に拡張可能である。
