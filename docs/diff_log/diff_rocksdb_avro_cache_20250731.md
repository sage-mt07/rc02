# 差分履歴: rocksdb_avro_cache

🗕 2025年7月31日（JST）
🧐 作業者: codex

## 差分タイトル
RocksDbTableCache を Streamiz + Avro 復元に対応

## 変更理由
RocksDB キャッシュのテーブルを Streamiz KafkaStreams から取得し、 Avro で値を復元する設計に統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `RocksDbTableCache` コンストラクタに `IKafkaStreams`, `storeName`, `ISchemaRegistryClient` を追加
- `InitializeAsync` で RUNNING 状態を待機し、QueryableStore からストアを取得
- `GetAll` で `AvroDeserializer` を用いて値を復元

## 参考文書
- `docs/namespaces/cache_namespace_doc.md`
