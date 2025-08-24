# 差分履歴: KeyValueTypeMapping Decimal Order

🗕 2025-08-10 JST
🧐 作業者: naruse

## 差分タイトル
Decimal precision resolution priority

## 変更理由
KsqlDecimal属性を最優先し、未指定時はappsettings由来の全体設定、さらに無い場合はデフォルト値を利用するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- DecimalPrecisionConfigにResolvePrecision/ResolveScaleを追加
- SpecificRecordGeneratorとKeyValueTypeMappingで上記ヘルパーを使用

## 参考文書
- `docs_advanced_rules.md` セクション未特定
