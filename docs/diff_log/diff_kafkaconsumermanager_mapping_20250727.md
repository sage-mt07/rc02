# 差分履歴: kafkaconsumermanager_mapping

🗕 2025年7月27日（JST）
🧐 作業者: assistant

## 差分タイトル
KafkaConsumerManager が MappingRegistry を利用して key/value 分割受信に対応

## 変更理由
Producer 側と同様に、Consumer でも動的に生成された key/value 型を経由して POCO に復元する必要があったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `GetConsumerAsync` で `KeyValueTypeMapping` を参照し、動的型で `SimpleKafkaConsumer` を生成
- `ConsumeAsync` など各取得メソッドで `CombineFromKeyValue` を使用して POCO を復元
- `CreateConsumerBuilder` も MappingRegistry の型情報を用いてキー無し時は `Null` 型でビルダーを生成

## 参考文書
- `docs/architecture/key_value_flow.md` セクション 8
