# 差分履歴: schema registry key subject handling

🗕 2025-08-10 JST
🧐 作業者: assistant

## 差分タイトル
Missing key subject no longer triggers schema retrieval error

## 変更理由
SchemaRegistryMetaProvider threw an exception when a topic's key schema was absent, preventing contexts from initializing for value-only topics or early schema registration tests.

## 追加・修正内容（反映先: oss_design_combined.md）
- Treat 40401 (subject not found) for key schema as empty metadata

## 参考文書
- N/A
