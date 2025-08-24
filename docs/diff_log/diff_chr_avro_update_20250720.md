# 差分履歴: chr_avro_update

🗕 2025年7月20日（JST）
🧐 作業者: assistant

## 差分タイトル
Chr.Avro を用いた動的スキーマ生成に更新

## 変更理由
Apache.Avro の SchemaBuilder API が利用できなかったため、Chr.Avro 系ライブラリを使って C# 型からスキーマを生成するよう変更した。

## 追加・修正内容（反映先: oss_design_combined.md）
- DynamicSchemaGenerator を Chr.Avro.Json + Apache.Avro パースで再実装
- KafkaProducerManager / KafkaConsumerManager から生成スキーマをログ出力
- csproj に Chr.Avro と Chr.Avro.Json を追加
- README に Chr.Avro 利用を明記

## 参考文書
- `docs_advanced_rules.md` セクション 2
