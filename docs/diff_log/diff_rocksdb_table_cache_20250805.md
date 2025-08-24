# 差分履歴: rocksdb_table_cache

🗕 2025-08-05 (JST)
🧐 作業者: naruse

## 差分タイトル
RocksDbTableCache InitializeAsyncループの状態チェック修正

## 変更理由
Streamiz KafkaのStateにはPENDING_ERRORが存在せず、コンパイルエラーと誤ったログ出力を招いていたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- InitializeAsync内のループでNOT_RUNNING状態を判定し、警告ログを出すよう修正。

## 参考文書
- なし
