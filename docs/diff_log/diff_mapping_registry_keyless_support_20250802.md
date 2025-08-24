# 差分履歴: mapping_registry_keyless_support

🗕 2025年8月2日（JST）
🧐 作業者: assistant

## 差分タイトル
MappingRegistry handles keyless entities without requiring attributes

## 変更理由
Order entity lacked key properties causing MappingRegistry registration failure. Keyless entities must be supported without artificial keys.

## 追加・修正内容（反映先: oss_design_combined.md）
- Removed restriction on empty key property arrays in `MappingRegistry.Register`
- Allowed `RegisterEntityModel` to register keyless entities
- Reverted DLQ integration test to keyless order entity

## 参考文書
- `docs/architecture/key_value_flow.md`

## 追記: Zero-field Avro schema generation
- `SpecificRecordGenerator` now outputs `fields: []` when a mapping has no properties, allowing keyless entities to register without schema errors.
- Added unit test to verify empty record schemas contain no fields.
