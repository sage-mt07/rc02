# 差分履歴: decimal precision registry validation
🗕 2025-08-10 JST
🧐 作業者: assistant

## 差分タイトル
appsettings overrides and schema registry validation for decimal precision

## 変更理由
Schema Registry を「正」として decimal precision/scale を統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KsqlDslOptions に `Decimals` 辞書を追加しプロパティ単位で精度/スケールを設定可能にした
- DecimalPrecisionConfig で appsettings と SR を読み取り最終精度を解決
- SchemaRegistry 登録時に SR との不一致を Strict/Relaxed で検証するバリデータを追加
- README に優先順位と ValidationMode 動作例を追記

## 参考文書
- `docs/docs_configuration_reference.md`
