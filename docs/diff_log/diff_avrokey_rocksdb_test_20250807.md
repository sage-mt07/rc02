# 差分履歴: avrokey_rocksdb_test

🗕 2025-08-07 JST
🧐 作業者: assistant

## 差分タイトル
AvroKey_To_RocksDb supports distinct Avro key and value types

## 変更理由
AvroKey_To_RocksDb previously handled identical key/value types; ensure support for differing Avro schemas.

## 追加・修正内容（反映先: oss_design_combined.md）
- Added Address Avro model.
- Added AvroKeyValueDifferentTypes_To_RocksDb test verifying different Avro key/value types.

## 参考文書
- N/A

