# 差分履歴: consumer_builder_removed

🗕 2025年7月31日（JST）
🧐 作業者: assistant

## 差分タイトル
KafkaConsumerManager.CreateConsumerBuilder<T> の削除

## 変更理由
- SendAsync API 統一に伴う簡素化の一環として、ConsumerBuilder 生成用メソッドを廃止

## 追加・修正内容（反映先: oss_design_combined.md）
- `KafkaConsumerManager.CreateConsumerBuilder<T>` メソッドを削除
- README のConsumerBuilder使用例を削除

## 参考文書
- `diff_kafkaconsumermanager_mapping_20250727.md`
