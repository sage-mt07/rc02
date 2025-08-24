# 差分履歴: decimal_precision_mapping

🗕 2025-08-02
🧐 作業者: assistant

## 差分タイトル
Include precision/scale when generating decimal Avro schemas

## 変更理由
Runtime generation of Avro records for POCO `decimal` properties failed with `AvroTypeException` because `precision` and `scale` were omitted.

## 追加・修正内容（反映先: oss_design_combined.md）
- `SpecificRecordGenerator.MapToAvroType` now emits `precision` and `scale` using `DecimalPrecisionConfig`.
- Added unit test verifying configured precision/scale appear in the schema.
- Documented decimal mapping behavior in the API reference.

## 参考文書
- docs/api_reference.md
