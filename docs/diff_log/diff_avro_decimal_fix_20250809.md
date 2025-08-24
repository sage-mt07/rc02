# 差分履歴: avro_decimal_fix

🗕 2025-08-09
🧐 作業者: 鳴瀬

## AvroDecimal対応でdecimalシリアライズを修正
- SpecificRecordGeneratorのdecimalプロパティをAvro.AvroDecimalへ変更
- KeyValueTypeMappingにAvroDecimal⇔decimal変換を追加
- Avroオブジェクト生成時にdecimalをAvroDecimalで包む処理を実装
- 小数の往復テストとサンプルスキーマを追加

## 変更理由
- decimalを直接渡すとAvro.AvroDecimalへのキャスト例外が発生するため

## 追加・修正内容（反映先: oss_design_combined.md）
- SpecificRecordGeneratorでAvroDecimal型を生成
- KeyValueTypeMappingのConvertIfNeededとAvro抽出処理を拡張
- テストとYourValue.avscを追加

## 参考文書
- Avro decimal logical type ドキュメント
