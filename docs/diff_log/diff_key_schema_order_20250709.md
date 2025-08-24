# 差分履歴: key_schema_order

🗕 2025年7月9日（JST）
🧐 作業者: 広夢（戦略広報AI）

## 差分タイトル
POCO定義順キー生成テストの追加

## 変更理由
POCO/DTOの定義順によるキー生成とマッピング順の検証を強化するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- UnifiedSchemaGeneratorTests に定義順キー生成テストを追加
- SelectExpressionVisitorDtoOrderTests に例外メッセージ検証を追加

## 参考文書
- `poco_design_policy.md` セクション 2
