# 差分履歴: cache_implementation

🗕 2025年7月17日（JST）
🧐 作業者: codex

## 差分タイトル
Cache namespace 初期実装

## 変更理由
Streamiz を利用したテーブルキャッシュ機構をコードに組み込み、StateStore 依存を解消するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `Cache` サブ名前空間を新設し `RocksDbTableCache`, `TableCacheRegistry` を追加
- `KsqlContext` をキャッシュ利用に対応させ `StateStore` 連携コードを削除
- 既存テストをキャッシュ仕様に合わせ更新、一部テストをスキップ

## 参考文書
- `docs/namespaces/cache_namespace_doc.md`
