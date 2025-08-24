# 差分履歴: MappingRegistry

🗕 2025-08-02
🧐 作業者: assistant

## Allow empty value mappings
Dropped value-property validation so `MappingRegistry` can register POCO types with no properties, enabling zero-field Avro schemas.

## 変更理由
Review comment: keyless entities should serialize without requiring value properties.

## 追加・修正内容（反映先: oss_design_combined.md）
- Removed value-property checks from `Register`, `RegisterEntityModel`, and `RegisterQueryModel`.
- Added unit test ensuring zero-field mappings register successfully.

## 参考文書
- None
