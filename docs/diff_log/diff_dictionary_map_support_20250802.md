# 差分履歴: dictionary_map_support

🗕 2025-08-02
🧐 作業者: assistant

## 差分タイトル
Dictionary<string,string> Avro map support

## 変更理由
POCO to Avro conversion lacked support for mapping `Dictionary<string,string>` properties, preventing Kafka headers from being serialized as map types.

## 追加・修正内容（反映先: oss_design_combined.md）
- Map `Dictionary<string,string>` properties to Avro `map` with string values and empty default.
- Reject dictionaries with non-string keys or values.
- Documented supported and unsupported dictionary usage.

## 参考文書
- None
