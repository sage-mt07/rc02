# Kafka.Ksql.Linq.Messaging 責務ドキュメント

## 概要
Kafka メッセージング機能の型安全な抽象化層を提供する namespace。Producer/Consumer の統一管理、設定管理、エラーハンドリング（DLQ）を担当。`MappingRegistry` から取得した key/value 型を利用して Avro シリアライザを生成し、KsqlContext との送受信を効率化する。

## 主要な責務

### 1. Abstractions - インターフェース定義
- **`IKafkaProducer<T>`**: 型安全な Producer インターフェース
- **`IKafkaConsumer<TValue, TKey>`**: 型安全な Consumer インターフェース

**設計意図**: 型安全性確保、テスタビリティ向上、既存 Avro 実装との統合

### 2. Configuration - 設定管理
- **`CommonSection`**: Kafka ブローカー共通設定（接続、セキュリティ）
- **`ProducerSection`**: Producer 固有設定（確認応答、圧縮、冪等性）
- **`ConsumerSection`**: Consumer 固有設定（グループ、オフセット、フェッチ）
- **`SchemaRegistrySection`** (Core namespace): Schema Registry 接続設定
- **`TopicSection`**: トピック別設定（Producer/Consumer 両方を含む）

**設計意図**: 設定の階層化、運用時の柔軟性確保

### 3. Producers - メッセージ送信
#### Core クラス
- **`KafkaProducer<T>`**: 統合型安全 Producer（Pool 削除、Confluent.Kafka 完全委譲）
- **`KafkaProducerManager`**: Producer の型安全管理（事前確定・キャッシュ）

#### DLQ（Dead Letter Queue）
- **`DlqProducer`**: デシリアライズ失敗データの DLQ 送信
- **`DlqEnvelope`**: DLQ メッセージ形式

##### 設計方針（メタ情報のみ格納）

本ライブラリの DLQ は元メッセージ本文を保存せず、以下の参照情報のみを保持します。

- 元トピック名
- パーティション番号
- オフセット
- スキーマ ID
- 発生時刻（タイムスタンプ）
- エラー種別・例外情報

メッセージデータ自体は元トピックに残るため、DLQ のストレージ使用量を抑えつつ大量トラフィック下でも安定したエラー追跡が可能です。

**復旧方法**  
DLQ レコードのメタ情報を参照し、元トピック上の該当オフセットのメッセージを取得して再処理または手動修正を行います。

**利用者への注意**  
DLQ 単体では元メッセージ本文を取得できません。復旧には元トピックへのアクセス権が必要です。メタ情報のスキーマは `DlqEnvelope` クラスとして提供されます。大規模障害時は DLQ と元トピック双方の保持期間が十分に設定されていることを確認してください。


**設計意図**: EF風API、型安全性確保、エラー耐性

### 4. Consumers - メッセージ消費
#### Core クラス
- **`KafkaConsumer<TValue, TKey>`**: 統合型安全 Consumer
- **`KafkaConsumerManager`**: Consumer の型安全管理

プール機構は廃止され、単一インスタンスでの購読管理に統一されている。

### 5. Contracts - エラーハンドリング契約
- **`IErrorSink`**: エラーレコード処理インターフェース（DLQ送信等）

### 6. Models - データ構造
- **`DlqEnvelope`**: DLQ メッセージのエンベロープ形式
  - 元メッセージ情報（Topic、Partition、Offset）
  - エラー情報（例外タイプ、メッセージ、スタックトレース）
  - デバッグ用ヘッダー復元

### 7. Internal - 内部実装
- **`ErrorHandlingContext`**: エラーハンドリング実行コンテキスト
  - リトライ制御
  - カスタムハンドラー実行
  - DLQ 送信判定

### 8. Exceptions - 例外定義

## アーキテクチャ特徴

### 型安全性の確保
- 全ての Producer/Consumer が型パラメータ `<T>` を持つ
- EntityModel を通じたメタデータ管理
- コンパイル時の型チェック

### Pool 削除による簡素化
- 従来の Pool 管理を廃止
- Confluent.Kafka への完全委譲
- リソース管理の簡素化

### 統一されたエラーハンドリング
- DLQ による失敗メッセージの保存
- デシリアライゼーション失敗の自動検出
- カスタムエラーハンドラーのサポート

### EF Core 風 API
- Manager クラスによる事前確定管理
- キャッシュによる性能向上
- 設定の階層化

## 主要な設計判断

1. **Pool 削除**: 複雑性削減のため Producer/Consumer プールを廃止
2. **型安全性優先**: 実行時エラーを防ぐため型パラメータを全面採用
3. **Confluent.Kafka 委譲**: 低レベル実装を Confluent.Kafka に完全委譲
4. **DLQ 標準装備**: 運用時のデータロスト防止のため DLQ を標準実装
