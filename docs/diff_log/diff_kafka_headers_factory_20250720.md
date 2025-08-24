# 差分履歴: kafka_headers_factory

🗕 2025年7月20日（JST）
🧐 作業者: assistant

## 差分タイトル
AddAsync/SendAsync ヘッダー指定と型安全Factory追加

## 変更理由
- Kafka 送信時に任意のヘッダーを付与できるようにするため
- Producer/Consumer の型情報を自動展開し、同一型のインスタンスを生成するAPIを提供するため

## 追加・修正内容（反映先: oss_design_combined.md）
- `AddAsync`/`SendAsync` に `Dictionary<string,string>` ヘッダー引数を追加
- `KafkaProducerManager.CreateProducerBuilder<T>` / `KafkaConsumerManager.CreateConsumerBuilder<T>` 実装
- `KsqlContext` から上記ファクトリを取得できるメソッドを公開
- README とドキュメントのサンプルコードをヘッダー付きに更新

## 参考文書
- `docs_advanced_rules.md` テスト方針セクション
