# 差分履歴: typed_store

🗕 2025-08-06 (JST)
🧐 作業者: assistant

## 差分タイトル
Kafka Streams state store and cache switched to typed keys/values.

## 変更理由
byte[] usage lacked type safety for Kafka Streams queries and message handling.

## 追加・修正内容（反映先: oss_design_combined.md）
- IKafkaStreams.Store generalized for key/value types.
- StreamizKafkaStreams and RocksDbTableCache use Avro SerDes for typed access.
- Updated test utilities and examples to use concrete types.

## 参考文書
- なし

