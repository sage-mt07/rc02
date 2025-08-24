# 差分履歴: ModelBuilder

🗕 2025-08-02
🧐 作業者: assistant

## Topic attribute parsing in ModelBuilder
- `ModelBuilder` now reads `KsqlTopicAttribute` to set topic name, partitions, and replication factor.
- Enables `[KsqlTopic]` to configure entity topic metadata directly.

## 変更理由
- TopicAttribute tests expected attribute-driven configuration but model builder ignored attribute.

## 追加・修正内容（反映先: oss_design_combined.md）
- `ModelBuilder.CreateEntityModelFromType` checks `KsqlTopicAttribute` and applies values.

## 参考文書
- docs/api_reference.md
