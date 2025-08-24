# 差分履歴: messaging_cleanup

🗕 2025年7月14日（JST）
🧐 作業者: codex

## 差分タイトル
Raw Kafka classes removed from Messaging

## 変更理由
使用されていない RawKafkaProducer/Consumer と関連インターフェースを削除し、Messaging を簡素化するため

## 追加・修正内容（反映先: oss_design_combined.md）
- IRawKafkaProducer, IRawKafkaConsumer インターフェース削除
- RawKafkaProducer, RawKafkaConsumer クラス削除

## 参考文書
- `docs/api_reference.md`
