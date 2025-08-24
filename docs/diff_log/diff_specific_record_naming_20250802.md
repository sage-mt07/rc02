# 差分履歴: specific_record_naming

🗕 2025-08-02 JST
🧐 作業者: assistant

## 差分タイトル
SpecificRecordGenerator uses full type names to avoid duplicates

## 変更理由
Repeated context initialisation generated identical dynamic Avro classes, leading to runtime `Duplicate type name within an assembly` errors.

## 追加・修正内容（反映先: oss_design_combined.md）
- Cache generated Avro types by full type name.
- Use the full name (including enclosing types) when defining Avro record types.

## 参考文書
- `docs_advanced_rules.md`
