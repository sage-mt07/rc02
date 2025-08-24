# Advanced Rules（詳細設計と運用ルール）

## 1. 本ドキュメントの位置付け

本ドキュメントは「getting-started.md」に記載された設計原則および構成ルールを前提とし、Kafka.Ksql.Linq OSSの**実装詳細・高度な設計思想・内部処理の挙動**を明文化するものです。

DSLや属性の基本的な使い方、アーキテクチャの理解を終えた上級開発者・運用担当者が、さらに深く制御や拡張を行うための参照資料として機能します。

---

## 2. クラス設計と可視性ポリシー

### 2.1 internal/public の役割整理

- APIとして外部に公開すべき型・拡張ポイント：`public`
- DSL内部の処理ロジック・変換パイプライン・State管理クラスなど：`internal`
- テストは公開インターフェース経由で実施、具象クラス直アクセス禁止

### 2.2 拡張ポイント

- `.OnError()` `.WithRetry()` は `IQueryable` 拡張で構成
- Window関連のDSLは `.Window(x)` 拡張として `IQueryable<POCO>` に統合
- 実行時のウィンドウ選択は `Set<T>().Window(x)` を使い `WindowMinutes` 直接指定は不要
- Fluent API の基本方針は [api_reference.md の Fluent API ガイドライン](./api_reference.md#fluent-api-guide) を参照してください。
- 主なメソッド一覧は [api_reference.md の Fluent API 一覧](./api_reference.md#fluent-api-list) にまとめています。

---

## 3. 型変換とスキーマ登録戦略（Avro連携）

-### 3.1 POCO → Avro スキーマ変換

- POCOに付与された属性（[KsqlDecimal], [KsqlDatetimeFormat] など）を読み取り、Avroスキーマを動的生成する。
- キー情報は DTO/POCO のプロパティ定義順から自動的に生成され、`Key` 属性は利用しない（詳細は [architecture_overview.md](./architecture_overview.md#poco%E8%A8%AD%E8%A8%88%E3%83%BBpk%E9%81%8B%E7%94%A8%E3%83%BB%E3%82%B7%E3%83%AA%E3%82%A2%E3%83%A9%E3%82%A4%E3%82%BA%E6%96%B9%E9%87%9D) を参照）。
- `SchemaRegistry.AutoRegisterSchemas = true` の場合、Kafka初回送信時に自動登録

### 3.2 変換時のマッピング規則

| POCO型                          | Avro型                                 | 備考             |
| ------------------------------ | ------------------------------------- | -------------- |
| `decimal` + [KsqlDecimal] | `bytes` + logicalType=decimal         | 精度・スケール付きで定義   |
| `DateTime`, `DateTimeOffset`   | `long` + logicalType=timestamp-millis | UTCに変換         |
| `string`, `Guid`               | `string`                              | Guidは文字列化      |
| `byte[]`                       | `bytes`                               | Avroのbinaryに対応 |

**Key schema に利用できる型は `int` `long` `string` `Guid` のみ。その他の型で GroupBy
を行う場合は、必ずこれらの型へ変換してから指定すること。**

### 3.3 スキーマレジストリの運用

- CI/CDパイプラインに統合し、スキーマ互換チェックをビルド時に実行
- `FORWARD` / `BACKWARD` / `FULL` の互換モードは明示指定
- 登録失敗時のフィードバックは詳細ログ出力（--verbose）で確認可能

### 3.4 Avroスキーマ命名規則と Namespace 管理

- スキーマの `Name` は **エンティティのクラス名** に対応させます。
- `Namespace` にはエンティティの名前空間を反映し、スキーマの一意性を担保します。
- トピック名を `Name` に含める設計は推奨されません。同一クラス名を複数トピックで使用する場合、Schema Registry 上で名前衝突が発生する可能性が高まります。
- 名前空間管理が不十分な場合は、トピック名などの接頭辞を `Namespace` 側に取り込む運用も検討してください。
- ユーザーは同一クラス名を再利用する際の衝突リスクを理解し、スキーマ管理体制を整備する必要があります。

### 3.5 NUL区切り文字列キー

`KeyValueTypeMapping.FormatKeyForPrefix` は Avro キーの `KeyProperties` を NUL (`\u0000`) で連結した文字列に変換し、`CombineFromStringKeyAndAvroValue` がこのキーと Avro value から POCO を復元します。`DateTime` は UTC の `yyyyMMdd'T'HHmmssfff'Z'` 形式でソート性を保ちます。

---

## 4. Finalトピック生成とWindow処理のタイマー駆動
### 4.1 Window処理

  🚩【最重要パターン宣言】
  本OSSのウィンドウ集約設計は「1つのPOCO＋Window属性で多足集約を一元管理」が基本方針です。
  型設計・APIサンプル・高度な応用もまずこの方式を優先してください。

複数時間足を扱う際は専用のPOCOを分ける必要はありません。以下のように `Window()` 拡張と
`WindowMinutes` プロパティを組み合わせることで、1つのエンティティで任意の足を処理できます。

```csharp
modelBuilder.Entity<RateCandle>()
    .ToQuery(q => q
        .From<Rate>()
        .Window(new[] { 1, 5, 15, 60 })
        .GroupBy(r => r.Symbol)
        .Select(g => new RateCandle { /* 集約ロジック */ }));
```

`ToQuery` DSL は JOIN を最大 2 テーブルまでサポートし、結合条件は `Join` メソッド内で指定します。必要に応じて追加のフィルタリングを `Where` で行えます。

実行時に特定の足だけを処理したい場合は `Set<T>().Window(5)` のように分岐させます。


### 4.2 Finalトピックの命名と作成およびRocksDBとの関係

- `{EntityName}_{Window}min_final` を基本命名規則とする

- 各Windowごとに1つのStateStore（RocksDBインスタンス）が構築され、ウィンドウ確定タイミングでその内容がFinalトピックに出力される

- StateStoreはアプリ内の状態保持に使われ、集計済みの結果は `WindowFinalizationManager` によりローカル→Kafkaへ出力される

- Final用のRocksDBは `rocksdb/final/{Entity}_{Window}min_Store/` に作成される（通常のStateStoreとは別ディレクトリ）

- キャッシュ（EnableCache）がONの場合、最新状態をメモリ保持するためファイルサイズは減少傾向にあるが、OFFの場合は全状態を永続化するためファイルサイズが大きくなる傾向がある

- アプリ起動時に `EnsureWindowFinalTopicsExistAsync` により全トピック事前作成

- `{EntityName}_{Window}min_final` を基本命名規則とする

- 各Windowごとに1つのStateStore（RocksDBインスタンス）が構築され、ウィンドウ確定タイミングでその内容がFinalトピックに出力される

- StateStoreはアプリ内の状態保持に使われ、集計済みの結果は `WindowFinalizationManager` によりローカル→Kafkaへ出力される

- アプリ起動時に `EnsureWindowFinalTopicsExistAsync` により全トピック事前作成

### 4.3 Final出力の特徴とGap対応

- TickがなくてもWindow終了時刻に自動出力されることで、“Gap”（空白期間）を補完し、時系列の連続性を保つ
- Gapとは、トピックにイベントが流れない時間帯においてもウィンドウ処理が時間軸上で欠損しないようにするための、明示的な“空の足”データを指します
- Finalデータは `WindowedResult` POCOをAvro化し、別トピックに出力
- 例：`orders_5min_final` トピックに `OrderCandle` 出力

---

## 5. DLQ設計とエラーハンドリング

### 5.1 DLQの設計思想

- すべてのエラーは `ErrorAction.DLQ` により集約的にDLQトピックへ送信可能
- DLQトピックは1系統（例：`dead-letter-queue`）を共通で使用
- メッセージには `sourceTopic`, `errorCode`, `exception` などのメタ情報付与

### 5.2 DLQ構成例

```json
"DlqOptions": {
  "RetentionMs": 5000,
  "NumPartitions": 1,
  "ReplicationFactor": 1,
  "EnableAutoCreation": true
}
```

> ※ RetentionMs のデフォルト値は 5000（5秒）です。これでは短すぎるケースも多いため、必要に応じて明示的に設定を行ってください。

---
## 6. 可観測性・メトリック運用指針 / Observability & Metrics

### 6.1 メトリック設計方針

本OSSでは、Kafkaやストリーム処理に関連するメトリック収集は**Confluent公式クライアントパッケージ（Confluent.Kafka）**側の機能を利用する方針とします。  
OSS本体はアプリケーション側の運用情報・エラー通知等を**ILogger等の標準ロギング機構**で出力します。
ログメッセージの表記ルールは [logging_guidelines.md](logging_guidelines.md) を参照してください。

- Kafkaパフォーマンス・レイテンシ・メッセージ数などの詳細メトリックは、Confluent.Kafkaが標準で提供する監視API・メトリック機能を活用してください。
- OSS本体で追加するのは「運用ログ（状態・エラー・イベント）」のみです。
- 独自メトリック追加が必要な場合は、ILoggerのログ集約または外部監視ツールと連携する拡張で対応します。

**参考：Confluent.Kafkaの公式メトリック／監視ガイドを参照のこと**

## 7. ストリーム/テーブルの自動判定と明示オーバーライド

- `GroupBy`, `Aggregate`, `Window` を含むLINQ式はテーブルと判定
- `AsStream()`, `AsTable()` は判定ロジックを上書き
- `AsPush()`, `AsPull()` でクエリの実行モードを強制（未指定時は `Unspecified` として扱い、Pull クエリ制約違反検知時に自動で Push へ切り替え）
- 判定結果は `.Explain()` や `ILogger` に出力可能（開発支援）

---

## 8. CI/CDおよび検証モード

- `ValidationMode: Strict` によりDSL構文とPOCO定義を初期化時に厳格チェック
- `GroupBy`/`Join` のキー順と DTO/POCO の定義順を照合し、相違があれば
  `InvalidOperationException` を発生させる。メッセージは
  "GroupByキーの順序と出力DTOの定義順が一致していません。必ず同じ順序にしてください。"
- CI環境では構文検証モードを利用し、Kafka未接続状態でDSL整合性確認
- 初期化失敗はビルド失敗とみなす

---

## 9. デフォルト構成と運用ルール

### 9.1 RocksDBの配置と構成

- StateStoreはローカルファイルとして `rocksdb/{Entity}_{Window}min_Store/` に配置される
- アプリケーション実行ディレクトリ内に階層構造で保存（実体はleveldb/rocksdbによる）
- コンパクションポリシー：デフォルトで `compact` モードが有効

### 9.2 Kafka関連のデフォルト設定

- パーティション数：設定がなければ `1`
- ReplicationFactor：設定がなければ `1`
- GroupId：`KsqlDsl_{EntityName}` が自動割当（手動設定可能）
- AutoOffsetReset：`Latest` がデフォルト

---

## 10. 用語定義と今後の拡張予定

- `WindowFinalizationManager`: Window終了時刻に自動出力を行う内部クラス
- `EventSet<T>.Commit(T entity)`: 手動コミット用メソッド
- `WithRetry`, `OnError`: DSLの拡張ポイント

今後追加予定：

- RetryBackoff, DeadLetterRetry, Topic間リレーション設計
- クエリのExplain/Previewモード

