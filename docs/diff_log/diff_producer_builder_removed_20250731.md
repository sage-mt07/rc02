# 差分履歴: producer_builder_removed

🗕 2025年7月31日（JST）
🧐 作業者: assistant

## 差分タイトル
KafkaProducerManager.CreateProducerBuilder<T> の削除

## 変更理由
- SendAsync API 統一に伴い、ProducerBuilder を直接生成するファクトリが不要となったため

## 追加・修正内容（反映先: oss_design_combined.md）
- `KafkaProducerManager.CreateProducerBuilder<T>` メソッドを削除
- `KsqlContext` の関連コメントを削除

## 参考文書
- `diff_kafka_headers_factory_20250720.md`
