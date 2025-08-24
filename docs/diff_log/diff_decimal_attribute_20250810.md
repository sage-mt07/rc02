# 差分履歴: decimal_attribute

🗕 2025-08-10
🧐 作業者: 鳴瀬

## decimal属性の精度スケール反映
- KsqlDecimal 属性から精度とスケールを読み取り Avro スキーマと変換に適用

## 変更理由
- グローバル設定のスケール固定で ProduceException が発生していたため

## 追加・修正内容（反映先: oss_design_combined.md）
- PropertyMeta.FromProperty で KsqlDecimal を解析
- SpecificRecordGenerator と KeyValueTypeMapping が属性のスケールを使用

## 参考文書
- docs/architecture/key_value_flow.md
