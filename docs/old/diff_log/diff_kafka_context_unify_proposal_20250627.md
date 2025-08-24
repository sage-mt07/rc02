# 差分履歴: kafka_context_unify_proposal

🗕 2025年6月27日（JST）
🧐 作業者: 迅人（テスト自動化AI）

## 差分タイトル
KafkaContext と KsqlContext のクラス統合提案

## 変更理由
- [`diff_overall_20250626.md`](./diff_overall_20250626.md) にて命名揺れが指摘されたため
- `KafkaContext` と `KsqlContext` の二重管理を解消し、実装とドキュメントの一貫性を確保するため

## 追加・修正内容（反映先: oss_design_combined.md）
- `src/KsqlContext.cs` へ `KsqlContext` クラスを集約
- 旧 `src/Application/KsqlContext.cs` を削除
- 主要メソッドや `EventSetWithServices<T>` の機能を維持したまま統合

## 統一後のファイル構成案

- `src/KsqlContext.cs` に `KsqlContext` クラスを集約
- 旧 `src/Application/KsqlContext.cs` は削除
- 既存の `KsqlContextBuilder`・`KsqlContextOptions` などと同一 namespace に統一

## 残すべきクラス／メソッド

### KsqlContext
- フィールド
  - `KafkaProducerManager _producerManager`
  - `KafkaConsumerManager _consumerManager`
  - `Lazy<ISchemaRegistryClient> _schemaRegistryClient`
  - `IAvroSchemaRegistrationService _schemaRegistrationService`
  - `KafkaAdminService _adminService`
- コンストラクター（`KsqlContextOptions` 受け取り可）
- `CreateEntitySet<T>`
- `GetProducerManager`, `GetConsumerManager`
- `ConvertToAvroConfigurations`
- `Dispose` / `DisposeAsyncCore`
- `ToString`

### EventSetWithServices<T>
- `AddAsync`
- `ToListAsync`
- `ForEachAsync`
- `GetAsyncEnumerator`

### その他保持メソッド
- `CreateSchemaRegistryClient`
- `CreateSchemaRegistrationService`
- `RegisterSchemasSync`
- `EnsureKafkaReadyAsync`
- `ValidateKafkaConnectivity`
- `GetDlqTopicName`

## 吸収・統合時の注意点

1. **命名統一** : 全ての `KafkaContext` 参照を `KsqlContext` にリネームし、ドキュメント類（`oss_design_combined.md` 等）も更新する。
2. **共通API維持** : 既存の `KafkaContext` API を利用している箇所はそのまま動作させるため、公開メソッドのシグネチャ変更は行わない。
3. **SchemaRegistry と DLQ 初期化処理** : `KafkaContext.cs` のスキーマ登録および DLQ 準備ロジックを `KsqlContext` に移植する。簡素化版の Manager 初期化コードは統合後も利用可能。
4. **ネームスペース** : `Kafka.Ksql.Linq.Application` に統一し、他の層（Core/StateStore など）からの参照を一本化する。
5. **テストの影響** : クラス名変更に伴うリフレクション利用箇所（テストコードなど）を確認する。`ConvertToAvroConfigurations` はテストからリフレクション呼び出しされるためアクセス修飾子を維持する。
6. **互換性確保** : 旧 `KafkaContext` を直接参照しているコードは暫定的なエイリアス型または `using` 指定で移行を容易にする。

## 設計意図

- DSL 層の主役コンテキストを `KsqlContext` に統一することで、利用者が混乱なく API を扱えるようにする。
- スキーマ登録・DLQ 初期化などの運用に必須な処理をデフォルト実装として組み込みつつ、テスト容易性のため `SkipSchemaRegistration` フックを残す。
- `EventSetWithServices<T>` を内部クラスとして保持し、Producer/Consumer/Streaming の基本機能を一か所で管理する。

以上が統合方針の要約である。既存の命名揺れを解消し、`KsqlContext` を中心としたシンプルな構造に再編成することで、将来的な保守性とテスト自動化の効率向上を狙う。

## 参考文書
- `docs_advanced_rules.md` の「2. 命名規約の詳細」
- [`diff_kafka_context_rename_20250627.md`](./diff_kafka_context_rename_20250627.md)

