# 差分履歴: rocksdb_table_cache

🗕 2025年6月28日（JST）
🧐 作業者: 迅人（テスト自動化AI）

## 差分タイトル
RocksDBテーブルキャッシュ機能の不足部分調査と設計方針

## 変更理由
- 新機能「RocksDBキャッシュ」を使いKTable状態を保持する要望を受けたため

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlContext` 生成時に `StateStoreBindingManager` を用いて RocksDB と Consumer を接続する処理を追加する
- `KsqlDslOptions.Entities` 設定に基づいて RocksDB ストアとバインディングを初期化
- `TopicStateStoreBinding.ReadyStateChanged` を `KsqlContext` 経由でアプリに通知するイベントハンドラを設計
- Ready監視は既存 `ReadyStateMonitor` を利用し、`IsReady` が `false` の場合にアプリ層へ警告イベントを送出

## 参考文書
- `docs/namespaces/statestore_namespace_doc.md` セクション Monitoring
- `docs/architecture_overview.md` セクション StateStore Layer
