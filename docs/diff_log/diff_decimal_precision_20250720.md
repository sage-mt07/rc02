# 差分履歴: decimal_precision

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
Decimal Precision と Scale をグローバル設定で統一

## 変更理由
スキーマや計算で decimal 型の precision/scale を一括で調整できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlDslOptions` に `DecimalPrecision` プロパティを追加
- 既存 `DecimalScale` と合わせて `DecimalPrecisionConfig` に反映
- クエリ生成ロジックで `DecimalPrecisionConfig` を参照
- ドキュメントに Precision/Scale 設定方法を追記

## 参考文書
- `docs_configuration_reference.md` セクション 1.7
