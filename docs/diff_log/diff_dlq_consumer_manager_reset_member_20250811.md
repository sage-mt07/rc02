# 差分履歴: dlq_consumer_manager_reset_member

🗕 2025-08-11 JST
🧐 作業者: assistant

## 差分タイトル
CreateBeginningOffsetsをメンバ関数ResetOffsetsToBeginning<T>に置換しConsumerBuilderを再利用

## 変更理由
DlqClientのオフセットリセット時にKafkaConsumerManagerのConsumerBuilderを活用し重複コードを排除するため

## 追加・修正内容（反映先: oss_design_combined.md）
- KafkaConsumerManagerにResetOffsetsToBeginning<TPOCO>を追加し、内部でConsumerBuilderを利用してオフセット0をコミット
- DlqClientから静的オフセット計算とConsumerBuilder生成を削除し、新メンバ関数を呼び出すよう変更

## 参考文書
- docs_advanced_rules.md
