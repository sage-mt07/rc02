# 差分履歴: decimal_precision_ksql_generation

🗕 2025-08-10 07:01 JST
🧐 作業者: naruse

## 差分タイトル
KSQL生成時のdecimal精度解決を属性優先へ統一

## 変更理由
OnModelCreatingで生成するスキーマでもKsqlDecimal属性を最優先とするため

## 追加・修正内容（反映先: oss_design_combined.md）
- PropertyMetaから精度・スケールを取得しKsqlTypeMappingに渡す
- KsqlSchemaBuilderとEntityModelDdlAdapterを更新

## 参考文書
- docs_advanced_rules.md
