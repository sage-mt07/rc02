# 差分履歴: table_cache_docs

🗕 2025-08-22 08:33 JST
🧐 作業者: 鳴瀬

## 差分タイトル
Table cache docs updated for string-key prefix filtering

## 変更理由
NUL区切りの文字列キーとフィルタ引数を公開API・設計ドキュメントに反映するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- api_reference.md に ITableCache と NULプレフィックスフィルタを追記
- docs_advanced_rules.md に キー連結/復元の詳細を追加
- getting-started.md に TableCache 利用例を追加

## 参考文書
- `docs/api_reference.md`
- `docs/docs_advanced_rules.md`
- `docs/getting-started.md`
