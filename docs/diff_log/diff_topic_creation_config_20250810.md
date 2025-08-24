# 差分履歴: topic_creation_config

🗕 2025-08-10 JST
🧐 作業者: assistant

## 差分タイトル
Apply configured partitions and replicas during DDL generation

## 変更理由
- EnsureSimpleEntityDdlAsync ignored DSL topic creation settings and always used defaults

## 追加・修正内容（反映先: oss_design_combined.md）
- set EntityModel.Partitions and ReplicationFactor only when configuration is present
- pass model values to CreateDbTopicAsync

## 参考文書
- `docs/docs_configuration_reference.md`
