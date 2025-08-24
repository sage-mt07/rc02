# 差分履歴: join_limit

🗕 2025年7月27日（JST）
🧐 作業者: assistant

## 差分タイトル
JOIN制限を2テーブルに変更

## 変更理由
ストリーム処理で扱うJOIN数を最小限に抑えるため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `JoinLimitationEnforcer.MaxJoinTables` を 3 から 2 に変更
- 関連コメント、テスト、ドキュメントを更新

## 参考文書
- `docs/namespaces/query_namespace_doc.md`
