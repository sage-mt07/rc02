# 差分履歴: decimal_scale

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
Decimal スケールをグローバル設定で統一

## 変更理由
各エンティティの Bid や Ask など decimal プロパティのスケールを一括調整できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlDslOptions` に `DecimalScale` プロパティを追加
- クエリ生成ロジックで `DecimalScaleConfig.DecimalScale` を参照
- ドキュメントに設定方法を追記

## 参考文書
- `docs_configuration_reference.md` セクション 1.7
