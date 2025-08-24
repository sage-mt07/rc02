# 差分履歴: confluent_serializer_unify

🗕 2025年7月20日（JST）
🧐 作業者: assistant

## 差分タイトル
Chr.Avro.Confluent でのシリアライザ統一

## 変更理由
Confluent 標準の AvroSerializer は ISpecificRecord 実装を要求するため、POCO 利用時に制約が残っていた。Chr.Avro.Confluent の AsyncSerializer/Deserializer を採用し、ISpecificRecord 依存を排除した。

## 追加・修正内容（反映先: oss_design_combined.md）
- KafkaProducerManager/KafkaConsumerManager で AsyncSchemaRegistrySerializer/Deserializer を使用
- csproj の Confluent.Kafka 系パッケージを 2.10.1 へ更新
- Chr.Avro/Chr.Avro.Confluent を 10.8.1 へ更新
- 既存テストを新シリアライザへ対応

## 参考文書
- `docs/test_guidelines.md` セクション 1
