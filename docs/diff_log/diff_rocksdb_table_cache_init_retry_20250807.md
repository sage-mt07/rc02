# 差分履歴: rocksdb_table_cache_init_retry

🗕 2025-08-07 (JST)
🧐 作業者: assistant

## 差分タイトル
RocksDbTableCache InitializeAsync adds stream and store readiness checks with retries

## 変更理由
InvalidStateStoreException が発生し、ストア復元完了前のアクセスが原因となっていたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KafkaStream の RUNNING への遷移を待機する WaitForStreamRunning を追加。
- メタデータ確認でストア復元を判定する WaitForStoreRestoration を追加。
- リトライとストアアクセス検証を行う InitializeStoreWithRetry を導入。

## 参考文書
- なし
