# 差分履歴: keyvaluetypemapping_decimal_round

🗕 2025-08-10
🧐 作業者: 鳴瀬

## AvroDecimal作成時にスケールを固定
- decimal を `F{scale}` で文字列化し再パースしてスケールを一致
- KeyValueTypeMapping で AvroDecimal 生成を統一

## 変更理由
- 整数値を DECIMAL(scale=2) にシリアライズするとスケール0になりエラーとなるため

## 追加・修正内容（反映先: oss_design_combined.md）
- ToAvroDecimal ユーティリティを追加し各メソッドで利用

## 参考文書
- docs_advanced_rules.md B.1.2
