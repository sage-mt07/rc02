# 差分履歴: DLQ MessageId Guid conversion

🗕 2025-08-10 JST
🧐 作業者: assistant

## 差分タイトル
Convert DlqEnvelope.MessageId Guid to string for Avro compatibility

## 変更理由
Avro serializer expected string for MessageId but received Guid, causing "System.String required … but found System.Guid".

## 追加・修正内容（反映先: oss_design_combined.md）
- SpecificRecordGenerator emits string property for Guid
- KeyValueTypeMapping converts Guid to string on serialization and parses string on deserialization
- Added unit test verifying Guid round-trip

## 参考文書
- N/A
