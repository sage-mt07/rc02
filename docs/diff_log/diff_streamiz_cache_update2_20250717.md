# 差分履歴: streamiz_cache_update2

🗕 2025年7月17日（JST）
🧐 作業者: codex

## 差分タイトル
TableCache 設定項目と Cache ドキュメント修正

## 変更理由
レビューコメントに基づき、キャッシュディレクトリ構成と StoreName 設定を明確化し、Avro と Mapping 利用を明示するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `TableCache` セクションに `BaseDirectory` 項目を追加
- `StoreName` のデフォルト説明を "トピック名基準" へ変更
- Cache namespace ドキュメントへ "Avro形式のkey/value" と `Mapping` 連携を追記
- Query 責務資料に Mapping 登録処理を追記

## 参考文書
- `docs/docs_configuration_reference.md`
- `docs/namespaces/cache_namespace_doc.md`
- `docs/namespaces/ksql_query_responsibility.md`
