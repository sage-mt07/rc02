# 差分履歴: dlq_beginning_commit_fix

🗕 2025-08-11 JST
🧐 作業者: assistant

## 差分タイトル
KafkaConsumerManager.CreateBeginningOffsetsがOffset(0)を返すよう修正

## 変更理由
DLQリセット時にコミットが失敗しないようにするため

## 追加・修正内容（反映先: oss_design_combined.md）
- KafkaConsumerManager.CreateBeginningOffsetsでOffset.BeginningではなくOffset(0)を生成
- DlqClient.ResetOffsetsToBeginningが正しいオフセットをコミットできるよう改善

## 参考文書
- docs_advanced_rules.md
