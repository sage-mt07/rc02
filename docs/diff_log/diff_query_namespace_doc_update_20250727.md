# 差分履歴: query_namespace_doc_update

🗕 2025年7月27日（JST）
🧐 作業者: assistant

## 差分タイトル
Query namespaceドキュメントのJOIN制限説明を更新

## 変更理由
JOIN制限が2テーブルに統一されたため、旧3テーブルJOIN関連記述を削除する。

## 追加・修正内容（反映先: oss_design_combined.md）
- `docs/namespaces/query_namespace_doc.md` から `IJoinResult<TOuter, TInner, TThird>` の説明を削除
- 設計上の2テーブルJOIN制限を再確認

## 参考文書
- `docs/namespaces/query_namespace_doc.md`
