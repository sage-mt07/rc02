# 差分履歴: rocksdb_mapping

🗕 2025年7月31日（JST）
🧐 作業者: codex

## 差分タイトル
RocksDbTableCache MappingRegistry 統合

## 変更理由
Streamizから取得したAvroデータをPOCOへ復元する際にMappingRegistryを利用するよう更新した。

## 追加・修正内容（反映先: oss_design_combined.md）
- RocksDbTableCacheコンストラクタにMappingRegistryを追加
- InitializeAsyncでAvroDeserializerを動的生成し、CombineFromAvroKeyValueでPOCO復元
- TableCacheRegistryがダミーKafkaStreamsでキャッシュを生成
- テストコードをMappingRegistry登録に対応

## 参考文書
- `docs/namespaces/cache_namespace_doc.md`
