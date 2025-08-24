# 差分履歴: rocksdb_table_cache

🗕 2025-08-05 (JST)
🧐 作業者: naruse

## 差分タイトル
RocksDbTableCache InitializeAsyncでNOT_RUNNING状態を即時エラーとして処理

## 変更理由
KafkaStreamが起動していない状態で初期化を待機するとループが終了しないため。

## 追加・修正内容（反映先: oss_design_combined.md）
- InitializeAsyncの待機ループでNOT_RUNNING状態時にInvalidOperationExceptionをスローし、エラーログを出力するよう修正。

## 参考文書
- なし
