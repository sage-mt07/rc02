# 差分履歴: remove_statestore

🗕 2025年7月17日（JST）
🧐 作業者: codex

## 差分タイトル
StateStore namespace の完全削除

## 変更理由
テーブルキャッシュ機能を Cache namespace で提供するため、旧 StateStore 実装を削除した。

## 追加・修正内容（反映先: oss_design_combined.md）
- `src/StateStore` 以下のコードを全て削除
- テストコード `tests/StateStore/` を削除
- `WindowFinalConsumer` を `RocksDbTableCache` ベースに変更
- ドキュメントから StateStore 言及を削除し、cache 仕様に統一

## 参考文書
- `docs/namespaces/cache_namespace_doc.md`
