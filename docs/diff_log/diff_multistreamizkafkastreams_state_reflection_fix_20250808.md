# 差分履歴: multistreamizkafkastreams_state_reflection_fix

🗕 2025年8月8日（JST）
🧐 作業者: assistant

## 差分タイトル
MultiStreamizKafkaStreams.GetInitialState uses public and non-public reflection

## 変更理由
Initial state retrieval missed when property visibility changed; expanded binding flags ensure correct state detection.

## 追加・修正内容（反映先: oss_design_combined.md）
- Include both public and non-public flags when reflecting KafkaStream `StreamState`.

## 参考文書
- `src/Cache/Core/MultiStreamizKafkaStreams.cs`
