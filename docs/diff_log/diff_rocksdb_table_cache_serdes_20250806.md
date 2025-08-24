# 差分履歴: rocksdb_table_cache

🗕 2025-08-06 (JST)
🧐 作業者: assistant

## 差分タイトル
RocksDbTableCache initializes store with Avro serdes

## 変更理由
State store enumeration failed when key/value serdes were not supplied, causing StreamsException.

## 追加・修正内容（反映先: oss_design_combined.md）
- Inject AvroSerDes for key and value in RocksDbTableCache.InitializeAsync.
- Attach Schema Registry client to the serdes before retrieving the store.

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "主要コンポーネント責務"
