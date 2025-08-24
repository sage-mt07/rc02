# 差分履歴: rdbcache_warning_revert

🗕 2025年8月2日（JST）
🧐 作業者: assistant

## 差分タイトル
RocksDbTableCache の null 安全化リファクタを取り消し

## 変更理由
レビューによりキャッシュ実装を変更せず、テスト側で初期化保証すべきと判断されたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- RocksDbTableCache のローカル変数キャプチャを削除し、フィールドを直接参照する元の実装へ戻した
- 進捗ログにテスト初期化の必要性を追記

## 参考文書
- `docs_advanced_rules.md` セクション 2
