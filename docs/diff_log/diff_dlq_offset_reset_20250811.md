# 差分履歴: dlq_offset_reset

🗕 2025-08-11 JST
🧐 作業者: 鳴瀬

## 差分タイトル
KafkaConsumerManagerにTopicPartitionOffsetをリセットするIFを追加

## 変更理由
DLQクライアントがトピックを先頭から読むため

## 追加・修正内容（反映先: oss_design_combined.md）
- KafkaConsumerManager.CreateBeginningOffsetsを追加
- DlqClientが当メソッドを利用

## 参考文書
- docs_advanced_rules.md
