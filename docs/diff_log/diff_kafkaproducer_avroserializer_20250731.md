# 差分履歴: kafkaproducer_avroserializer_switch

🗕 2025年7月31日（JST）
🧐 作業者: assistant

## 差分タイトル
KafkaProducerManager を Confluent.AvroSerializer 利用へ変更

## 変更理由
プロデューサーで Chr.Avro.Confluent に依存していたが、SpecificRecord 生成済み型を利用し Confluent 公式 `AvroSerializer` へ統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KafkaProducerManager` が `AvroSerializer` を使用し、`MappingRegistry` の `AvroKeyType` `AvroValueType` をもとにシリアライザを生成
- キーレスエンティティでも空の `AvroKeyType` を用いてスキーマ登録を行う
- `KeyValueTypeMapping` に Avro 型プロパティ情報とコピー用メソッドを追加

## 参考文書
- `docs/architecture/key_value_flow.md` セクション 8
