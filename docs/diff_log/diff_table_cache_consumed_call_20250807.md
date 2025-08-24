# 差分履歴: table_cache_consumed_call

🗕 2025-08-07 (JST)
🧐 作業者: assistant

## 差分タイトル
StreamBuilder.Table reflection now uses Consumed.With to supply SerDes

## 変更理由
Kafka Streams failed to initialize when key/value SerDes were not provided to the Table source.

## 追加・修正内容（反映先: oss_design_combined.md）
- Invoke StreamBuilder.Table with Consumed.With to set source SerDes.
- Output log of configured key and value SerDes for each table.

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "UseTableCache"
