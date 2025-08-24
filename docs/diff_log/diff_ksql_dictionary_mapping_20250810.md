# 差分履歴: ksql_dictionary_mapping

🗕 2025-08-10
🧐 作業者: assistant

## 差分タイトル
Map Dictionary<string,string> to ksqlDB MAP<STRING, STRING>

## 変更理由
Dictionary headers caused schema generation failures because KsqlTypeMapping did not support dictionaries.

## 追加・修正内容（反映先: oss_design_combined.md）
- Added Dictionary<string,string>/IDictionary<string,string> handling to KsqlTypeMapping
- Removed KsqlIgnore from DlqEnvelope.Headers so headers participate in schema registration

## 参考文書
- None
