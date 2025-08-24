# 差分履歴: decimal_scale_config

🗕 2025-08-10
🧐 作業者: 鳴瀬

## Avro decimal scale and precision default updated
- Default `DecimalPrecisionConfig` changed to precision 18 and scale 2.
- KeyValueTypeMapping now wraps decimals with `new AvroDecimal(decimal.Round(value, scale))`.

## 変更理由
- Previous scale 9 was too large for typical price usage and caused serialization mismatches.

## 追加・修正内容（反映先: oss_design_combined.md）
- Updated `DecimalPrecisionConfig` and `KsqlDslOptions` defaults.
- Applied scale-aware conversion in `KeyValueTypeMapping`.
- Adjusted tests and appsettings to reflect new defaults.

## 参考文書
- Avro decimal logical type documentation

