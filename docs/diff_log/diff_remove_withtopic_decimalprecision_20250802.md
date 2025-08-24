# 差分履歴: remove_withtopic_decimalprecision

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
WithTopic and WithDecimalPrecision APIs removed

## 変更理由
Fluent API methods for topic assignment and decimal precision were deprecated in favor of attribute-based configuration.

## 追加・修正内容（反映先: oss_design_combined.md）
- Removed `WithTopic` and `WithDecimalPrecision` from `IEntityBuilder` and `EntityModelBuilder`.
- Adjusted `DdlSchemaBuilder` to accept topic information via constructor.
- Updated tests to use `[KsqlTopic]` and `[KsqlDecimal]` attributes.

## 参考文書
- `docs_advanced_rules.md` セクション  B.1.2
