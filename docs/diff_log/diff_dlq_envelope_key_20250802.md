# 差分履歴: dlq_envelope_key_registration

🗕 2025年8月2日（JST）
🧐 作業者: sion

## 差分タイトル
DlqEnvelope の MappingRegistry 登録用キー指定

## 変更理由
DlqEnvelope にキー情報がなく最新APIでコンテキスト初期化時に MappingRegistry が失敗するため。

## 追加・修正内容（反映先: src/KsqlContext.cs）
- DLQ 用エンティティモデルに MessageId をキーとして設定し、MappingRegistry 登録が成功するよう修正。

## 参考文書
- docs/change_summary_codex_20250801.md
