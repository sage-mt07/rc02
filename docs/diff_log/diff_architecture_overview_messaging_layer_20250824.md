# 差分履歴: Architecture Overview (Messaging)

🗕 2025-08-24 (JST)
🧐 作業者: assistant

## 差分タイトル
Messaging層追加とレイヤー再編のための Architecture Overview 更新

## 変更理由
レイヤー一覧から Messaging が抜けており、責務が不明確だったため。

## 追加・修正内容（反映先: docs/architecture_overview.md）
- クエリ構築層とストリーム構成層を統合
- Messaging層を追加し、Kafka I/O との橋渡しを明示

## 参考文書
- docs/architecture/key_value_flow.md
- docs/architecture/entityset_to_messaging_story.md
